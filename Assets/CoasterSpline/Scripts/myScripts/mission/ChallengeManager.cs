// Assets/CoasterSpline/Scripts/myScripts/mission/ChallengeManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CoasterSpline
{
    /// <summary>
    /// 챌린지 로직(겸용 버전)
    /// - 기본: MissionData.checkpoint(CoasterSensor)를 직접 사용
    /// - 선택: MissionData.checkpointId(string) + 이 매니저의 Checkpoints 배열(id↔sensor/focus) 매핑 사용
    /// - 런 시작 시 규칙 적용 → 체크포인트에서 판정 → 슬로모션 → 결과 UI
    /// </summary>
    [DisallowMultipleComponent]
    public class ChallengeManager : MonoBehaviour
    {
        [Header("Mission")]
        public MissionData mission;

        [Header("Refs")]
        public Rigidbody          trainRb;
        public Terrain            terrain;
        public Transform          baseline;
        public Transform          trainRoot;
        public FrictionController friction;
        public StartRegionBinder  startBinder;

        [Header("UI")]
        public ChallengeUI ui;

        // ---- Checkpoint 매핑용 타입 선언(필드 아님) ----
        [System.Serializable]
        public struct CheckpointBinding
        {
            [Tooltip("미션 SO의 checkpointId와 일치(예: ground, hill1top)")]
            public string id;
            public CoasterSensor sensor;   // 판정 센서
            public Transform     focus;    // 연출용 포커스(없으면 sensor.transform)
        }

        // <<< 여기 '배열 필드'에만 Header를 붙입니다 >>>
        [Header("Checkpoints (optional)")]
        [Tooltip("문자열 ID ↔ 센서/포커스를 연결(원할 때만 사용). SO가 CoasterSensor를 직접 들고 있으면 비워도 됩니다.")]
        public CheckpointBinding[] checkpoints;

        [Header("Debug")]
        public bool debugLogs = true;

        // 내부
        readonly Dictionary<string, CheckpointBinding> _cpMap = new();
        CoasterSensor _activeSensor;
        Transform     _activeFocus;
        bool  runActive;
        float peakHeight;
        float groundAtStart;

        // ───────────────── lifecycle
        void Awake()
        {
            if (!terrain) terrain = Terrain.activeTerrain;
            if (!trainRoot && trainRb) trainRoot = trainRb.transform;
            RebuildCheckpointMap();
        }

        void OnValidate()
        {
            if (!Application.isPlaying) RebuildCheckpointMap();
        }

        void OnEnable()
        {
            if (GameModeManager.I)
            {
                GameModeManager.I.OnRunStart.AddListener(OnRunStart);
                GameModeManager.I.OnRunEnd  .AddListener(OnRunEnd);
                GameModeManager.I.OnRunReset.AddListener(OnRunReset);
            }

            // 센서 구독
            if (ResolveActiveCheckpoint() && _activeSensor)
                _activeSensor.OnTrainEnter.AddListener(OnCheckpointEnter);
            else if (mission && mission.checkpoint)
                mission.checkpoint.OnTrainEnter.AddListener(OnCheckpointEnter);
        }

        void OnDisable()
        {
            if (GameModeManager.I)
            {
                GameModeManager.I.OnRunStart.RemoveListener(OnRunStart);
                GameModeManager.I.OnRunEnd  .RemoveListener(OnRunEnd);
                GameModeManager.I.OnRunReset.RemoveListener(OnRunReset);
            }

            if (_activeSensor) _activeSensor.OnTrainEnter.RemoveListener(OnCheckpointEnter);
            if (mission && mission.checkpoint) mission.checkpoint.OnTrainEnter.RemoveListener(OnCheckpointEnter);
        }

        void Start()
        {
            if (ui && mission)
            {
                ui.SetupIntro(mission);
                ui.ShowIntro(true);
                ui.ShowResult(false);
            }
        }

        void Update()
        {
            if (!runActive || !trainRoot) return;

            float gy = GetGroundYAt(trainRoot.position);
            float h  = Mathf.Max(0f, trainRoot.position.y - gy);
            if (h > peakHeight) peakHeight = h;
        }

        // ───────────────── map & resolve
        void RebuildCheckpointMap()
        {
            _cpMap.Clear();
            if (checkpoints == null) return;
            foreach (var b in checkpoints)
            {
                if (string.IsNullOrEmpty(b.id)) continue;
                if (_cpMap.ContainsKey(b.id))
                {
                    if (debugLogs) Debug.LogWarning($"[Challenge] Duplicate checkpoint id: {b.id}");
                    continue;
                }
                _cpMap.Add(b.id, b);
            }
        }

        bool ResolveActiveCheckpoint()
        {
            _activeSensor = null;
            _activeFocus  = null;
            if (mission == null) return false;

            // 1) SO의 직접 참조 우선
            if (mission.checkpoint)
            {
                _activeSensor = mission.checkpoint;
                _activeFocus  = mission.focusOverride ? mission.focusOverride : mission.checkpoint.transform;
                return true;
            }

            // 2) ID 매핑(선택)
            if (!string.IsNullOrEmpty(mission.checkpointId) && _cpMap.TryGetValue(mission.checkpointId, out var b))
            {
                _activeSensor = b.sensor;
                _activeFocus  = b.focus ? b.focus : (b.sensor ? b.sensor.transform : null);
                return _activeSensor != null;
            }

            return false;
        }

        // ───────────────── run flow
        void OnRunStart()
        {
            if (debugLogs) Debug.Log("[Challenge] RunStart");
            if (ui) { ui.ShowIntro(false); ui.ShowResult(false); }

            if (mission != null)
            {
                if (friction) friction.SetEnabled(mission.forceFrictionOn);

                if (startBinder)
                {
                    startBinder.enabled = !mission.lockEditing;
                    if (mission.lockStartHeight && startBinder.heightSlider)
                        startBinder.heightSlider.interactable = false;
                }

                if (trainRb)
                {
                    trainRb.isKinematic = false;

                    float v0 = Mathf.Max(0f, mission.startSpeed);
                    Vector3 dir = (trainRoot ? trainRoot.forward : Vector3.forward).normalized;
                    trainRb.velocity        = dir * v0;
                    trainRb.angularVelocity = Vector3.zero;
                }

                if (mission.massOverride >= 0f)
                {
                    var hud = FindObjectOfType<HUD_All_TMP>(true);
                    if (hud) hud.mass = mission.massOverride;
                }
            }

            Vector3 probe = trainRoot ? trainRoot.position :
                            (baseline ? baseline.position : transform.position);
            groundAtStart = GetGroundYAt(probe);

            peakHeight = 0f;
            runActive  = true;
        }

        void OnRunEnd()
        {
            if (debugLogs) Debug.Log("[Challenge] RunEnd");
            runActive = false;
        }

        void OnRunReset()
        {
            if (debugLogs) Debug.Log("[Challenge] RunReset");
            Time.timeScale = 1f;
            runActive = false;

            if (startBinder)
            {
                startBinder.enabled = true;
                if (startBinder.heightSlider)
                    startBinder.heightSlider.interactable = true;
            }

            if (ui)
            {
                ui.ShowIntro(true);
                ui.ShowResult(false);
            }
        }

        // ───────────────── judge
        void OnCheckpointEnter()
        {
            if (!runActive || mission == null) return;

            bool   success  = false;
            string headline = "";
            string subline  = "";

            switch (mission.goal)
            {
                case GoalType.SpeedAtCheckpoint:
                {
                    float speed = (trainRb ? trainRb.velocity.magnitude : 0f);
                    success  = Compare(speed, mission.targetValue, mission.tolerance, mission.compare);
                    headline = success ? mission.successText : mission.failText;
                    subline  = $"속도 {speed:0.0} m/s  (목표 {mission.targetValue:0.0})";
                    break;
                }

                case GoalType.PeakHeightBeforeCheckpoint:
                {
                    success  = Compare(peakHeight, mission.targetValue, mission.tolerance, mission.compare);
                    headline = success ? mission.successText : mission.failText;
                    subline  = $"최고높이 {peakHeight:0.0} m  (목표 {mission.targetValue:0.0})";
                    break;
                }
            }

            StartCoroutine(ShowResultRoutine(success, headline, subline));
        }

        IEnumerator ShowResultRoutine(bool success, string headline, string subline)
        {
            float old = Time.timeScale;
            Time.timeScale = (mission ? Mathf.Clamp(mission.slowmoScale, 0.05f, 1f) : 0.25f);
            float hold = (mission ? Mathf.Max(0.2f, mission.slowmoHoldRealtime) : 1.2f);

            if (ui)
            {
                ui.SetupResult(success, headline, subline);
                ui.ShowResult(true);
            }

            yield return new WaitForSecondsRealtime(hold);
            Time.timeScale = old;
            // GameModeManager.I?.ResetRun(); // 자동리셋 원하면 주석 해제
        }

        // ───────────────── utils
        bool Compare(float value, float target, float tol, CompareMode mode)
        {
            switch (mode)
            {
                case CompareMode.AtLeast: return value >= target - tol;
                case CompareMode.AtMost:  return value <= target + tol;
                default:                  return Mathf.Abs(value - target) <= Mathf.Abs(tol);
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
    }
}
