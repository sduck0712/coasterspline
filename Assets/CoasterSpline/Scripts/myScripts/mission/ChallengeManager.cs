// Assets/CoasterSpline/Scripts/myScripts/ChallengeManager.cs
using System.Collections;
using UnityEngine;

namespace CoasterSpline
{
    /// <summary>
    /// 챌린지 로직: 미션 로드 → 소개 화면 → 런 시작 시 규칙 적용 → 체크포인트에서 판정 → 슬로모션 → 결과 UI
    /// 표시 포맷: 속도/높이 소수 1자리, 나머지 정수(ChallengeUI에서 포맷 제어)
    /// </summary>
    [DisallowMultipleComponent]
    public class ChallengeManager : MonoBehaviour
    {
        [Header("Mission")]
        public MissionData mission;          // 인스펙터에서 선택

        [Header("Refs")]
        public Rigidbody trainRb;            // 열차 RB(속도/초기속도)
        public Terrain terrain;              // 높이 기준 Terrain(없으면 Raycast)
        public Transform baseline;           // 기준 포인트(없으면 trainRoot)
        public Transform trainRoot;          // 열차 루트(기준/높이)
        public FrictionController friction;  // 규칙 강제 On
        public StartRegionBinder startBinder;// 편집/슬라이더 잠금

        [Header("UI")]
        public ChallengeUI ui;

        [Header("Debug")]
        public bool debugLogs = true;

        // 내부 상태
        bool runActive;
        float peakHeight; // 시작~체크포인트 사이 최고 높이(m)
        float groundAtStart;

        void OnEnable()
        {
            if (!terrain) terrain = Terrain.activeTerrain;

            // 런 흐름 구독
            if (GameModeManager.I)
            {
                GameModeManager.I.OnRunStart.AddListener(OnRunStart);
                GameModeManager.I.OnRunEnd  .AddListener(OnRunEnd);
                GameModeManager.I.OnRunReset.AddListener(OnRunReset);
            }

            // 센서 구독
            if (mission && mission.checkpoint)
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
            if (mission && mission.checkpoint)
                mission.checkpoint.OnTrainEnter.RemoveListener(OnCheckpointEnter);
        }

        void Start()
        {
            // 소개 화면 표시
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

            // 현재 높이(지면 기준)
            float gy = GetGroundYAt(trainRoot.position);
            float h  = Mathf.Max(0f, trainRoot.position.y - gy);
            if (h > peakHeight) peakHeight = h;
        }

        // ───────────────────────── 런 플로우 ─────────────────────────
        void OnRunStart()
        {
            if (debugLogs) Debug.Log("[Challenge] RunStart");

            // UI 숨김
            if (ui) { ui.ShowIntro(false); ui.ShowResult(false); }

            // 규칙 적용
            if (mission != null)
            {
                if (friction) friction.SetEnabled(mission.forceFrictionOn);

                if (startBinder)
                {
                    // 편집 잠금
                    startBinder.enabled = !mission.lockEditing;
                    if (mission.lockStartHeight && startBinder.heightSlider)
                        startBinder.heightSlider.interactable = false;
                }

                // 초기 속도/질량
                if (trainRb)
                {
                    trainRb.isKinematic = false;
                    // 초기 속도
                    float v0 = Mathf.Max(0f, mission.startSpeed);
                    Vector3 dir = (trainRoot ? trainRoot.forward : Vector3.forward).normalized;
                    trainRb.velocity = dir * v0;
                    trainRb.angularVelocity = Vector3.zero;
                }
                if (mission.massOverride >= 0f)
                {
                    var hud = FindObjectOfType<HUD_All_TMP>(true);
                    if (hud) hud.mass = mission.massOverride;
                }
            }

            // 기준 지면 높이 기록
            Vector3 probe = trainRoot ? trainRoot.position :
                            (baseline ? baseline.position : transform.position);
            groundAtStart = GetGroundYAt(probe);

            // 피크 리셋
            peakHeight = 0f;
            runActive = true;
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

            // 편집 복구
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

        // ───────────────────── 체크포인트 판정 ─────────────────────
        void OnCheckpointEnter()
        {
            if (!runActive || mission == null) return;

            bool success = false;
            string headline = "", subline = "";

            switch (mission.goal)
            {
                case GoalType.SpeedAtCheckpoint:
                {
                    float speed = (trainRb ? trainRb.velocity.magnitude : 0f);
                    success = Compare(speed, mission.targetValue, mission.tolerance, mission.compare);
                    headline = success ? mission.successText : mission.failText;
                    subline  = $"속도 {speed:0.0} m/s  (목표 {mission.targetValue:0.0})";
                    break;
                }
                case GoalType.PeakHeightBeforeCheckpoint:
                {
                    success = Compare(peakHeight, mission.targetValue, mission.tolerance, mission.compare);
                    headline = success ? mission.successText : mission.failText;
                    subline  = $"최고높이 {peakHeight:0.0} m  (목표 {mission.targetValue:0.0})";
                    break;
                }
            }

            // 연출 + 결과
            StartCoroutine(ShowResultRoutine(success, headline, subline));
        }

        IEnumerator ShowResultRoutine(bool success, string headline, string subline)
        {
            // 슬로모션
            float old = Time.timeScale;
            Time.timeScale = (mission ? Mathf.Clamp(mission.slowmoScale, 0.05f, 1f) : 0.25f);
            float hold = (mission ? Mathf.Max(0.2f, mission.slowmoHoldRealtime) : 1.2f);

            // 결과 UI 표시
            if (ui)
            {
                ui.SetupResult(success, headline, subline);
                ui.ShowResult(true);
            }

            yield return new WaitForSecondsRealtime(hold);
            Time.timeScale = 1f;

            // 여기서 바로 정지/리셋은 하지 않고, UI의 Reset 버튼에 맡기길 권장
            // 필요 시 자동 리셋하려면 아래 한 줄 활성화:
            // GameModeManager.I?.ResetRun();
        }

        // ───────────────────────── Utils ─────────────────────────
        bool Compare(float value, float target, float tol, CompareMode mode)
        {
            switch (mode)
            {
                case CompareMode.AtLeast: return value >= target - tol;
                case CompareMode.AtMost:  return value <= target + tol;
                default: // Near
                    return Mathf.Abs(value - target) <= Mathf.Abs(tol);
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
