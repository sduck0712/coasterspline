using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace CoasterSpline
{
    [DisallowMultipleComponent]
    public class StartRegionBinder : MonoBehaviour
    {
        #region Track 선택
        [Header("Track (CoasterGenerator)")]
        public CoasterGenerator generator;

        public bool autoDetectAnchors = true;
        public Transform stationRoot;
        [Min(1)] public int autoAnchorCount = 8;

        public int chainIndex = 0;
        public int startAnchor = 0;
        public int anchorCount = 20;

        [Header("범위 옵션")]
        public bool affectAllChains = false;

        [Tooltip("기차 뒤쪽(X<=train.x)은 항상 포함(빠른해결1)")]
        public bool alwaysIncludeBehindTrain = true;
        public Transform trainRoot;
        #endregion

        #region 절대/오프셋 모드
        [Header("Absolute Height (권장)")]
        public bool useAbsoluteHeight = true;
        public Terrain terrain;
        public float minClearance = 0f;
        public float maxAboveGround = 15f;
        public Slider heightSlider;

        [Header("Offset Mode (0~1 -> min~max)")]
        public float minOffset = 0f;
        public float maxOffset = 20f;
        #endregion

        #region 동기 이동 대상
        [Header("Sync Objects (move by Δy)")]
        public List<Transform> supportRoots = new();
        public List<CoasterAccelerator> accelerators = new();
        public List<CoasterSensor> sensors = new();
        public List<Transform> extraTransforms = new();

        [Header("Sync Train")]
        public Rigidbody trainRb;
        public bool moveTrainOnlyWhenNotRunning = true;

        [Header("Auto Collect (optional)")]
        public bool autoFindAccelerators = true;
        public bool autoFindSensors = true;
        public bool autoFindSupportsByTag = false;
        public string supportTag = "Support";
        #endregion

        #region 내부 상태
        struct AnchorRef { public int chain; public int index; public Vector3 basePos; }

        readonly List<AnchorRef> _targets = new();
        readonly Dictionary<Transform, float> _baseY = new();

        float _baseStationY;              // 씬 초기 기준 Y (기차/스테이션)
        bool _cached;
        float _lastAppliedOffset = 0f;    // 현재 씬에 적용된 오프셋(렌더 상태)
        float _lastExploreOffset  = 0f;   // 탐색 모드에서 마지막으로 설정한 오프셋
        #endregion

        void Awake()
        {
            if (!generator) generator = FindObjectOfType<CoasterGenerator>(true);
            if (!terrain) terrain = Terrain.activeTerrain;
            if (!trainRoot && trainRb) trainRoot = trainRb.transform;

            CacheBases();                  // 베이스라인(기본값) 고정 저장

            // 슬라이더 이벤트 연결(선택)
            if (heightSlider) heightSlider.onValueChanged.AddListener(SetHeight01);
        }

        void OnValidate()
        {
            if (!generator) generator = FindObjectOfType<CoasterGenerator>(true);
            if (!terrain) terrain = Terrain.activeTerrain;
        }

        // ─────────────────────────────────────────────────────
        #region 외부 API

        /// <summary>슬라이더 0..1 -> 절대/오프셋 반영(탐색 전용: Explore 값 저장)</summary>
        public void SetHeight01(float t01)
        {
            if (!_cached) CacheBases();
            if (!_cached) return;

            t01 = Mathf.Clamp01(t01);
            float offset;

            if (useAbsoluteHeight)
            {
                // 현재 기준 위치의 지면 높이
                Vector3 probe = stationRoot ? stationRoot.position
                               : (trainRoot ? trainRoot.position : transform.position);
                float groundY = GetGroundYAt(probe);
                float targetY = Mathf.Lerp(groundY + minClearance, groundY + maxAboveGround, t01);
                offset = targetY - _baseStationY;
            }
            else
            {
                offset = Mathf.Lerp(minOffset, maxOffset, t01);
            }

            ApplyOffset(offset, rememberExplore:true);
        }

        /// <summary>절대 높이(미터) 직접 지정(탐색 전용)</summary>
        public void SetAbsoluteHeight(float meters)
        {
            if (!_cached) CacheBases();
            if (!_cached) return;

            Vector3 probe = stationRoot ? stationRoot.position
                           : (trainRoot ? trainRoot.position : transform.position);
            float groundY = GetGroundYAt(probe);
            meters = Mathf.Clamp(meters, groundY + minClearance, groundY + maxAboveGround);

            float offset = meters - _baseStationY;
            ApplyOffset(offset, rememberExplore:true);
        }

        /// <summary>탐색 모드로 들어갈 때 호출: 마지막 탐색 오프셋 재적용</summary>
        public void EnterExploreMode()
        {
            // 베이스라인에서 탐색 오프셋만 적용
            ApplyOffset(_lastExploreOffset, rememberExplore:false);
            SyncSliderToCurrent();
        }

        /// <summary>챌린지/실험 등: 항상 기본값(베이스라인)으로 복귀</summary>
        public void EnterChallengeMode()
        {
            ApplyOffset(0f, rememberExplore:false); // 베이스라인
        }

        /// <summary>현재 씬 상태 기준으로 슬라이더만 조용히 동기화</summary>
        public void SyncSliderToCurrent()
        {
            if (!heightSlider) return;

            if (useAbsoluteHeight)
            {
                // 지금 적용된 오프셋을 절대 높이 값으로 환산 후 UI만 동기화
                float currentY = _baseStationY + _lastAppliedOffset;

                Vector3 probe = stationRoot ? stationRoot.position
                               : (trainRoot ? trainRoot.position : transform.position);
                float g = GetGroundYAt(probe);

                float t01 = Mathf.InverseLerp(g + minClearance, g + maxAboveGround, currentY);
                heightSlider.SetValueWithoutNotify(Mathf.Clamp01(t01));
            }
            else
            {
                float t01 = Mathf.InverseLerp(minOffset, maxOffset, _lastAppliedOffset);
                heightSlider.SetValueWithoutNotify(Mathf.Clamp01(t01));
            }
        }

        /// <summary>씬 로드 직후 1회: 베이스라인 캐싱</summary>
        public void CacheBases()
        {
            _cached = false;
            _targets.Clear();
            _baseY.Clear();

            if (!generator || generator.Chains == null || generator.Chains.Count == 0) return;

            // 기준 Y(처음 씬 상태를 베이스라인으로 고정)
            _baseStationY = (trainRoot ? trainRoot.position.y :
                            (stationRoot ? stationRoot.position.y : transform.position.y));

            BuildAnchorTargets();

            // 동기 이동 대상의 초기 Y
            if (stationRoot && !_baseY.ContainsKey(stationRoot)) _baseY[stationRoot] = stationRoot.position.y;

            if (autoFindSupportsByTag)
            {
                foreach (var go in GameObject.FindGameObjectsWithTag(supportTag))
                {
                    var tr = go.transform;
                    if (!_baseY.ContainsKey(tr)) _baseY[tr] = tr.position.y;
                    if (!supportRoots.Contains(tr)) supportRoots.Add(tr);
                }
            }
            foreach (var tr in supportRoots) if (tr && !_baseY.ContainsKey(tr)) _baseY[tr] = tr.position.y;

            if (autoFindAccelerators)
            {
                accelerators.Clear();
                accelerators.AddRange(FindObjectsOfType<CoasterAccelerator>(true));
            }
            foreach (var a in accelerators) if (a && a.transform && !_baseY.ContainsKey(a.transform)) _baseY[a.transform] = a.transform.position.y;

            if (autoFindSensors)
            {
                sensors.Clear();
                sensors.AddRange(FindObjectsOfType<CoasterSensor>(true));
            }
            foreach (var s in sensors) if (s && s.transform && !_baseY.ContainsKey(s.transform)) _baseY[s.transform] = s.transform.position.y;

            foreach (var tr in extraTransforms) if (tr && !_baseY.ContainsKey(tr)) _baseY[tr] = tr.position.y;
            if (trainRoot && !_baseY.ContainsKey(trainRoot)) _baseY[trainRoot] = trainRoot.position.y;

            _lastAppliedOffset = 0f;
            // 탐색값도 초기 0 (탐색에서만 바뀜)
            _lastExploreOffset  = 0f;

            _cached = true;
        }
        #endregion

        // ─────────────────────────────────────────────────────
        #region 내부 동작

        void BuildAnchorTargets()
        {
            _targets.Clear();

            if (affectAllChains)
            {
                for (int ci = 0; ci < generator.Chains.Count; ci++)
                {
                    var ch = generator.Chains[ci];
                    for (int ai = 0; ai < ch.Anchors.Count; ai++)
                        _targets.Add(new AnchorRef { chain = ci, index = ai, basePos = ch.Anchors[ai].Position });
                }
                return;
            }

            int useChain = chainIndex, useStart = startAnchor, useCount = anchorCount;

            if (autoDetectAnchors && stationRoot)
            {
                float best = float.MaxValue; int bestC = 0, bestA = 0;

                for (int ci = 0; ci < generator.Chains.Count; ci++)
                {
                    var ch = generator.Chains[ci];
                    for (int ai = 0; ai < ch.Anchors.Count; ai++)
                    {
                        Vector3 wp = ch.Anchors[ai].Position + generator.transform.position;
                        float d = (wp - stationRoot.position).sqrMagnitude;
                        if (d < best) { best = d; bestC = ci; bestA = ai; }
                    }
                }

                useChain = bestC;
                useStart = Mathf.Max(0, bestA - (autoAnchorCount / 2));
                useCount = Mathf.Clamp(autoAnchorCount, 1, generator.Chains[useChain].Anchors.Count - useStart);
            }

            var chain = generator.Chains[Mathf.Clamp(useChain, 0, generator.Chains.Count - 1)];
            int n = Mathf.Clamp(useCount, 1, chain.Anchors.Count - useStart);

            for (int i = 0; i < n; i++)
            {
                int ai = Mathf.Clamp(useStart + i, 0, chain.Anchors.Count - 1);
                _targets.Add(new AnchorRef { chain = useChain, index = ai, basePos = chain.Anchors[ai].Position });
            }

            if (alwaysIncludeBehindTrain && (trainRoot || stationRoot))
            {
                Transform refTf = trainRoot ? trainRoot : stationRoot;
                float boundaryX = refTf.position.x - generator.transform.position.x;

                for (int ci = 0; ci < generator.Chains.Count; ci++)
                {
                    var ch = generator.Chains[ci];
                    for (int ai = 0; ai < ch.Anchors.Count; ai++)
                    {
                        var a = ch.Anchors[ai];
                        if (a.Position.x <= boundaryX)
                        {
                            bool exists = _targets.Exists(t => t.chain == ci && t.index == ai);
                            if (!exists) _targets.Add(new AnchorRef { chain = ci, index = ai, basePos = a.Position });
                        }
                    }
                }
            }
        }

        void ApplyOffset(float offset, bool rememberExplore)
        {
            // A) 앵커 이동 (베이스라인 + offset)
            foreach (var t in _targets)
            {
                var ch = generator.Chains[t.chain];
                if (t.index < 0 || t.index >= ch.Anchors.Count) continue;

                var a = ch.Anchors[t.index];
                a.Position = new Vector3(t.basePos.x, t.basePos.y + offset, t.basePos.z);
                ch.SetDirty();
            }
            RebuildGenerator();

            // B) 기타 객체 이동
            foreach (var kv in _baseY)
            {
                var tr = kv.Key; if (!tr) continue;
                tr.position = new Vector3(tr.position.x, kv.Value + offset, tr.position.z);
            }

            // C) 기차 RB 정지(옵션)
            if (trainRb)
            {
                trainRb.velocity = Vector3.zero;
                trainRb.angularVelocity = Vector3.zero;
                if (!trainRb.isKinematic && trainRoot)
                    trainRb.position = new Vector3(trainRb.position.x, _baseY[trainRoot] + offset, trainRb.position.z);
            }

            _lastAppliedOffset = offset;
            if (rememberExplore) _lastExploreOffset = offset;
        }

        void RebuildGenerator()
        {
            if (!generator) return;
            var t = generator.GetType();
            foreach (var name in new[] { "Rebuild", "Generate", "Regenerate", "Build", "UpdateMesh" })
            {
                var mi = t.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (mi != null) { mi.Invoke(generator, null); break; }
            }
        }

        float GetGroundYAt(Vector3 worldPos)
        {
            if (terrain)
                return terrain.SampleHeight(worldPos) + terrain.transform.position.y;

            if (Physics.Raycast(worldPos + Vector3.up * 200f, Vector3.down, out var hit, 1000f))
                return hit.point.y;

            return 0f;
        }
        #endregion
    }
}
