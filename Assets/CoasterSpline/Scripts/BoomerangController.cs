// Assets/CoasterSpline/Scripts/myScripts/BoomerangController.cs
using UnityEngine;

namespace CoasterSpline
{
    public enum PushDirection { Forward = 1, Reverse = -1 }

    [System.Serializable]
    public class AccelGroup
    {
        public CoasterAccelerator[] accelerators;

        [Header("힘 설정")]
        public float force = 50f;
        public float maxSpeed = 2f;
        public float brakeForce = 0f;
        public PushDirection direction = PushDirection.Forward;
    }

    [DisallowMultipleComponent]
    public class BoomerangController : MonoBehaviour
    {
        [Header("Sensors")]
        public CoasterSensor lifthill1TopSensor;
        public CoasterSensor lifthill2TopSensor;
        public CoasterSensor stationSensor;

        [Header("Groups")]
        public AccelGroup station = new AccelGroup();
        public AccelGroup lifthill1 = new AccelGroup();
        public AccelGroup lifthill2 = new AccelGroup();

        [Header("Start / Flow")]
        public bool  autoStartOnPlay = false;
        public float startDelaySec    = 0f;
        public bool  allowSensorStart = false;

        [Header("Brakes")]
        public float returnBrakeForce = 2f;
        public float finalBrakeForce  = 30f;

        int  state = 0;     // 0 idle, 1 st->h1, 2 h1->h2, 3 back, 4 almost stop
        bool armed = false; // UI start 눌러 무장?

        bool _isHooked = false; // GM 이벤트 구독 성공 여부 캐시

        // ───────────────────────────── Unity Hooks

        void Awake()
        {
            Debug.Log("[Boomerang] Awake");
        }

        void OnEnable()
        {
            Debug.Log("[Boomerang] OnEnable");
            // 센서
            if (stationSensor)
            {
                stationSensor.OnTrainEnter.AddListener(OnStationEnter);
                stationSensor.OnTrainExit.AddListener(OnStationExit);
            }
            if (lifthill1TopSensor) lifthill1TopSensor.OnTrainEnter.AddListener(OnLifthill1Top);
            if (lifthill2TopSensor) lifthill2TopSensor.OnTrainEnter.AddListener(OnLifthill2Top);

            EnsureHookToGameMode();
        }

        void Start()
        {
            Debug.Log("[Boomerang] Start");
            StopAllGroups();
            EnsureHookToGameMode(); // Awake 순서 문제 대비

            if (autoStartOnPlay)
            {
                armed = true;
                if (startDelaySec > 0f) Invoke(nameof(StartFromStation), startDelaySec);
                else StartFromStation();
            }
        }

        void OnDisable()
        {
            Debug.Log("[Boomerang] OnDisable");
            // 센서
            if (stationSensor)
            {
                stationSensor.OnTrainEnter.RemoveListener(OnStationEnter);
                stationSensor.OnTrainExit.RemoveListener(OnStationExit);
            }
            if (lifthill1TopSensor) lifthill1TopSensor.OnTrainEnter.RemoveListener(OnLifthill1Top);
            if (lifthill2TopSensor) lifthill2TopSensor.OnTrainEnter.RemoveListener(OnLifthill2Top);

            UnhookFromGameMode();
        }

        // ───────────────────────────── GM 연동

        void EnsureHookToGameMode()
        {
            if (_isHooked) return;
            var gm = GameModeManager.I;
            if (gm != null)
            {
                gm.OnRunStart.AddListener(StartRunFromUI);
                gm.OnRunEnd.AddListener(ResetFlow);
                gm.OnRunReset.AddListener(ResetFlow);
                _isHooked = true;
                Debug.Log("[Boomerang] Subscribed to GameModeManager events");
            }
            else
            {
                Debug.LogWarning("[Boomerang] GameModeManager.I is null (will retry at Start).");
            }
        }

        void UnhookFromGameMode()
        {
            if (!_isHooked) return;
            var gm = GameModeManager.I;
            if (gm != null)
            {
                gm.OnRunStart.RemoveListener(StartRunFromUI);
                gm.OnRunEnd.RemoveListener(ResetFlow);
                gm.OnRunReset.RemoveListener(ResetFlow);
            }
            _isHooked = false;
        }

        // ───────────────────────────── 외부 제어

        public void StartRunFromUI()
        {
            Debug.Log("[Boomerang] StartRunFromUI");
            armed = true;
            StartFromStation();
        }

        public void ResetFlow()
        {
            Debug.Log("[Boomerang] ResetFlow");
            CancelInvoke();
            StopAllGroups();
            state = 0;
            armed = false;
        }

        // ───────────────────────────── Flow

        void OnStationEnter()
        {
            Debug.Log($"[Boomerang] StationEnter (state={state}, allowSensorStart={allowSensorStart}, armed={armed})");

            if (!allowSensorStart)
            {
                if (state == 4)
                {
                    SetBrakeOnly(station, finalBrakeForce);
                    state = 0;
                    armed = false;
                }
                return;
            }

            if (!autoStartOnPlay && !armed) return;

            if (state == 0)
            {
                if (autoStartOnPlay && startDelaySec > 0f) Invoke(nameof(StartFromStation), startDelaySec);
                else StartFromStation();
            }
            else if (state == 4)
            {
                SetBrakeOnly(station, finalBrakeForce);
                state = 0;
                armed = false;
            }
        }

        void OnStationExit()
        {
            Debug.Log($"[Boomerang] StationExit (state={state})");
            if (state == 3)
            {
                state = 4;
                SetBrakeOnly(station, returnBrakeForce);
            }
        }

        void StartFromStation()
        {
            Debug.Log("[Boomerang] StartFromStation -> Station+Lift1 ON");
            ApplyGroup(station,  true);
            ApplyGroup(lifthill1,true);
            ApplyGroup(lifthill2,false);
            state = 1;
        }

        void OnLifthill1Top()
        {
            Debug.Log($"[Boomerang] Lifthill1Top (state={state})");
            if (state != 1) return;

            ApplyGroup(station,  false);
            ApplyGroup(lifthill1,false);
            ApplyGroup(lifthill2,true);
            state = 2;
        }

        void OnLifthill2Top()
        {
            Debug.Log($"[Boomerang] Lifthill2Top (state={state})");
            if (state != 2) return;

            ApplyGroup(lifthill2,false);
            state = 3;
        }

        // ───────────────────────────── Group helpers

        void ApplyGroup(AccelGroup g, bool active)
        {
            if (g == null || g.accelerators == null)
            {
                Debug.LogWarning("[Boomerang] ApplyGroup: group is null");
                return;
            }

            foreach (var acc in g.accelerators)
            {
                if (!acc) continue;

                // MaxSpeed은 항상 세팅
                acc.MaxSpeed = g.maxSpeed;

                if (active)
                {
                    float signed = (int)g.direction * Mathf.Abs(g.force);
                    acc.Force      = signed;
                    acc.BreakForce = g.brakeForce;
                    acc.enabled    = true; // 안전
                    Debug.Log($"[Boomerang] ON  {acc.name}  F={acc.Force}  Max={acc.MaxSpeed}  Brake={acc.BreakForce}");
                }
                else
                {
                    acc.Force      = 0f;
                    acc.BreakForce = 0f;
                    acc.enabled    = true; // 비활성화 안 함(시각화 유지)
                    Debug.Log($"[Boomerang] OFF {acc.name}  F={acc.Force}");
                }
            }
        }

        void SetBrakeOnly(AccelGroup g, float brake)
        {
            if (g == null || g.accelerators == null) return;
            foreach (var acc in g.accelerators)
            {
                if (!acc) continue;
                acc.Force      = 0f;
                acc.BreakForce = brake;
                acc.enabled    = true;
                Debug.Log($"[Boomerang] BRAKE {acc.name}  Brake={acc.BreakForce}");
            }
        }

        void StopAllGroups()
        {
            ApplyGroup(station,  false);
            ApplyGroup(lifthill1,false);
            ApplyGroup(lifthill2,false);
        }
    }
}
