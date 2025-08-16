// Assets/CoasterSpline/Scripts/myScripts/StartRegionBinder.cs
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace CoasterSpline
{
    /// <summary>
    /// 시작 구간 동기 이동/정렬 도우미.
    /// - (선택) 스테이션 근처 앵커 자동 탐지
    /// - 슬라이더(절대높이 or 오프셋)로 앵커 높이 이동 → 트랙 재생성
    /// - 스테이션/기둥/엑셀/센서/기차도 같은 Δy로 이동
    /// - (옵션) 플레이 시작 시 씬의 높이를 '기본값'으로 슬라이더만 맞춤
    /// </summary>
    [DisallowMultipleComponent]
    public class StartRegionBinder : MonoBehaviour
    {
        #region Track - 대상 앵커 선택
        [Header("Track (CoasterGenerator)")]
        public CoasterGenerator generator;

        [Tooltip("스테이션 근처 앵커를 자동으로 찾아 시작 구간으로 사용")]
        public bool autoDetectAnchors = true;
        public Transform stationRoot;                 // 자동탐지 기준점
        [Min(1)] public int autoAnchorCount = 8;      // 시작구간으로 이동시킬 앵커 개수

        [Tooltip("수동 지정 시 사용")]
        public int chainIndex = 0;
        public int startAnchor = 0;
        public int anchorCount = 20;

        [Header("범위 옵션")]
        [Tooltip("체인의 전 앵커를 모두 이동(간단 모드)")]
        public bool affectAllChains = false;

        [Tooltip("기차 뒤쪽(X-방향)은 항상 포함(빠른해결 1)")]
        public bool alwaysIncludeBehindTrain = true;
        public Transform trainRoot;                   // 뒤쪽 판정 기준(없으면 stationRoot 사용)
        #endregion

        #region 절대높이 / 오프셋 모드
        [Header("Absolute Height (권장)")]
        public bool useAbsoluteHeight = true;
        public Terrain terrain;
        [Tooltip("지면 위 최소 높이(m)")] public float minClearance = 0f;
        [Tooltip("지면 위 최대 높이(m)")] public float maxAboveGround = 15f;
        public Slider heightSlider;                   // UI 슬라이더(선택)

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
        public Rigidbody trainRb;                    // 있으면 위치 갱신 후 속도 0
        public bool moveTrainOnlyWhenNotRunning = true;

        [Header("Auto Collect (optional)")]
        public bool autoFindAccelerators = true;
        public bool autoFindSensors = true;
        public bool autoFindSupportsByTag = false;
        public string supportTag = "Support";
        #endregion

        #region Defaults (요청 기능)
        [Header("Defaults")]
        [Tooltip("플레이 시작 시, 씬에 놓여 있던 높이를 기준으로 '슬라이더만' 조용히 맞춤")]
        public bool initSliderAtPlay = true;

        [Tooltip("기본 참조 높이: 기차(있으면) → 없으면 스테이션")]
        public bool useTrainYAsDefault = true;
        #endregion

        // ───────── 내부 캐시 ─────────
        struct AnchorRef
        {
            public int chain;
            public int index;
            public Vector3 basePos;
        }

        readonly List<AnchorRef> _targets = new();            // 이동 대상 앵커 + 원본 좌표
        readonly Dictionary<Transform, float> _baseY = new(); // 이동 대상들의 원본 Y
        float _baseStationY;                                   // 기준 y(Station or Train)
        bool _cached;
        float _lastOffset;

        void Awake()
        {
            if (!generator) generator = FindObjectOfType<CoasterGenerator>(true);
            if (!terrain) terrain = Terrain.activeTerrain;
            if (!trainRoot && trainRb) trainRoot = trainRb.transform;

            CacheBases();

            // 플레이 시작 시 UI만 현재 씬 높이에 맞춤
            if (initSliderAtPlay) InitSliderToSceneHeight();

            // 슬라이더에 직접 바인딩해도 됨(중복 바인딩은 피하세요)
            if (heightSlider)
                heightSlider.onValueChanged.AddListener(SetHeight01);
        }

        void OnValidate()
        {
            if (!generator) generator = FindObjectOfType<CoasterGenerator>(true);
            if (!terrain) terrain = Terrain.activeTerrain;
        }

        // ─────────────────────────────────────────────────────
        #region Public API

        /// <summary>슬라이더에서 0..1 값이 들어오면 호출</summary>
        public void SetHeight01(float t01)
        {
            if (!_cached) CacheBases();
            if (!_cached) return;

            t01 = Mathf.Clamp01(t01);

            float offset;
            if (useAbsoluteHeight)
            {
                // 지면 기준 절대 높이 계산
                Vector3 probe = (stationRoot ? stationRoot.position
                                  : (trainRoot ? trainRoot.position : transform.position));
                float groundY = GetGroundYAt(probe);

                float targetY = Mathf.Lerp(groundY + minClearance, groundY + maxAboveGround, t01);
                offset = targetY - _baseStationY;
            }
            else
            {
                offset = Mathf.Lerp(minOffset, maxOffset, t01);
            }

            ApplyOffset(offset);
        }

        /// <summary>
        /// AppController 등에서 직접 '지면 위 높이(m)'로 설정하고 싶을 때 호출.
        /// useAbsoluteHeight 설정과 무관하게 작동합니다.
        /// </summary>
        public void SetAbsoluteHeight(float metersAboveGround)
        {
            if (!_cached) CacheBases();
            if (!_cached) return;

            Vector3 probe = (stationRoot ? stationRoot.position
                              : (trainRoot ? trainRoot.position : transform.position));
            float groundY = GetGroundYAt(probe);

            float clamped = Mathf.Clamp(metersAboveGround, minClearance, maxAboveGround);
            float targetY = groundY + clamped;
            float offset = targetY - _baseStationY;

            ApplyOffset(offset);

            // UI 동기화(있으면)
            if (heightSlider && useAbsoluteHeight)
            {
                float t01 = Mathf.InverseLerp(groundY + minClearance, groundY + maxAboveGround, targetY);
                heightSlider.SetValueWithoutNotify(Mathf.Clamp01(t01));
            }
        }

        /// <summary>캐시 재작성(씬 편집 후 수동 호출용)</summary>
        public void CacheBases()
        {
            _cached = false;
            _targets.Clear();
            _baseY.Clear();

            if (!generator || generator.Chains == null || generator.Chains.Count == 0)
                return;

            // 기준 Y(씬 상태)
            float refY = (useTrainYAsDefault && trainRoot) ? trainRoot.position.y
                       : (stationRoot ? stationRoot.position.y : transform.position.y);
            _baseStationY = refY;

            // 이동할 앵커 집합 구성
            BuildAnchorTargets();

            // 동기 이동 대상 원본 Y 캐시
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

            _lastOffset = 0f;
            _cached = true;
        }

        #endregion
        // ─────────────────────────────────────────────────────

        #region Internals

        void BuildAnchorTargets()
        {
            _targets.Clear();

            // 1) affectAllChains면 모든 체인의 모든 앵커 수집
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

            // 2) 자동탐지 or 수동 구간
            int useChain = chainIndex, useStart = startAnchor, useCount = anchorCount;

            if (autoDetectAnchors && stationRoot)
            {
                float best = float.MaxValue;
                int bestChain = 0, bestAnchor = 0;

                for (int ci = 0; ci < generator.Chains.Count; ci++)
                {
                    var ch = generator.Chains[ci];
                    for (int ai = 0; ai < ch.Anchors.Count; ai++)
                    {
                        Vector3 wp = ch.Anchors[ai].Position + generator.transform.position;
                        float d = (wp - stationRoot.position).sqrMagnitude;
                        if (d < best) { best = d; bestChain = ci; bestAnchor = ai; }
                    }
                }

                useChain = bestChain;
                useStart = Mathf.Max(0, bestAnchor - (autoAnchorCount / 2));
                useCount = Mathf.Clamp(autoAnchorCount, 1, generator.Chains[useChain].Anchors.Count - useStart);
            }

            // 대상 구간 수집
            var chain = generator.Chains[Mathf.Clamp(useChain, 0, generator.Chains.Count - 1)];
            int n = Mathf.Clamp(useCount, 1, chain.Anchors.Count - useStart);

            for (int i = 0; i < n; i++)
            {
                int ai = Mathf.Clamp(useStart + i, 0, chain.Anchors.Count - 1);
                _targets.Add(new AnchorRef { chain = useChain, index = ai, basePos = chain.Anchors[ai].Position });
            }

            // 3) 빠른해결 1: 기차 '뒤쪽'은 항상 포함(X가 기준)
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

        void ApplyOffset(float offset)
        {
            _lastOffset = offset;

            // A) 앵커 이동
            foreach (var t in _targets)
            {
                var ch = generator.Chains[t.chain];
                if (t.index < 0 || t.index >= ch.Anchors.Count) continue;

                var a = ch.Anchors[t.index];
                var p = t.basePos;
                a.Position = new Vector3(p.x, p.y + offset, p.z);
                ch.SetDirty();
            }
            RebuildGenerator();

            // B) 기타 오브젝트 이동
            foreach (var kv in _baseY)
            {
                var tr = kv.Key; if (!tr) continue;
                tr.position = new Vector3(tr.position.x, kv.Value + offset, tr.position.z);
            }

            // C) 기차 리지드바디 정지
            if (trainRb)
            {
                trainRb.velocity = Vector3.zero;
                trainRb.angularVelocity = Vector3.zero;
                if (!trainRb.isKinematic && trainRoot)
                    trainRb.position = new Vector3(trainRb.position.x, _baseY[trainRoot] + offset, trainRb.position.z);
            }
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

            return 0f; // fallback
        }

        /// <summary>
        /// 플레이 시작 시, 씬의 높이를 '기본값'으로 슬라이더만 맞춤(트랙은 그대로)
        /// </summary>
        void InitSliderToSceneHeight()
        {
            if (!heightSlider) return;

            if (useAbsoluteHeight)
            {
                float yRef =
                    (useTrainYAsDefault && trainRoot) ? trainRoot.position.y :
                    (stationRoot ? stationRoot.position.y : _baseStationY);

                Vector3 probe = stationRoot ? stationRoot.position
                                  : (trainRoot ? trainRoot.position : transform.position);
                float ground = GetGroundYAt(probe);

                float t01 = Mathf.InverseLerp(ground + minClearance, ground + maxAboveGround, yRef);
                heightSlider.SetValueWithoutNotify(Mathf.Clamp01(t01));
            }
            else
            {
                float t01 = Mathf.InverseLerp(minOffset, maxOffset, 0f);
                heightSlider.SetValueWithoutNotify(Mathf.Clamp01(t01));
            }
        }

        #endregion
    }
}
