// Assets/CoasterSpline/Scripts/myScripts/BoomerangController.cs
using UnityEngine;

namespace CoasterSpline
{
    // 정/역 방향 선택
    public enum PushDirection { Forward = 1, Reverse = -1 }

    // 가속 휠 그룹(스테이션 / 힐1 / 힐2) 공통 설정
    [System.Serializable]
    public class AccelGroup
    {
        [Tooltip("이 그룹에 속한 CoasterAccelerator 목록")]
        public CoasterAccelerator[] accelerators;

        [Header("힘 설정")]
        [Tooltip("밀어줄 힘의 크기(부호는 Direction으로 결정)")]
        public float force = 50f;

        [Tooltip("속도 제한 (이 값 초과 시 GetForce가 0 또는 Brake 동작)")]
        public float maxSpeed = 2f;

        [Tooltip("감속(브레이크) 힘")]
        public float brakeForce = 0f;

        [Tooltip("Forward(정방향)=+1, Reverse(역방향)=-1  (휠 Transform.forward 기준)")]
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
        [Tooltip("플레이 직후 자동 출발 여부 (교육 시 OFF 권장)")]
        public bool autoStartOnPlay = false;
        [Tooltip("자동 출발을 사용할 때 지연(초)")]
        public float startDelaySec = 0f;

        [Tooltip("센서(Station Enter)로 출발을 허용할지? 기본값: 꺼짐(UI로만 출발)")]
        public bool allowSensorStart = false;

        [Header("Brakes")]
        [Tooltip("스테이션으로 복귀하며 지나갈 때 약한 감속")]
        public float returnBrakeForce = 2f;
        [Tooltip("스테이션에 정지시키는 강한 감속")]
        public float finalBrakeForce = 30f;

        // 상태: 0=대기, 1=스테이션→힐1, 2=힐1정상→힐2(역방향), 3=힐2정상→스테이션 복귀, 4=정지 직전
        int state = 0;
        bool armed = false; // Start 버튼으로 시동 무장했는지

        // ─────────────────────────────────────────────────────
        #region Unity Hooks

        void OnEnable()
        {
            // 센서 이벤트 연결
            if (stationSensor)
            {
                stationSensor.OnTrainEnter.AddListener(OnStationEnter);
                stationSensor.OnTrainExit.AddListener(OnStationExit);
            }
            if (lifthill1TopSensor) lifthill1TopSensor.OnTrainEnter.AddListener(OnLifthill1Top);
            if (lifthill2TopSensor) lifthill2TopSensor.OnTrainEnter.AddListener(OnLifthill2Top);

            // GameModeManager 연동(있을 때만)
            if (GameModeManager.I != null)
                GameModeManager.I.OnRunStart.AddListener(StartRunFromUI);
        }

        void OnDisable()
        {
            if (stationSensor)
            {
                stationSensor.OnTrainEnter.RemoveListener(OnStationEnter);
                stationSensor.OnTrainExit.RemoveListener(OnStationExit);
            }
            if (lifthill1TopSensor) lifthill1TopSensor.OnTrainEnter.RemoveListener(OnLifthill1Top);
            if (lifthill2TopSensor) lifthill2TopSensor.OnTrainEnter.RemoveListener(OnLifthill2Top);

            if (GameModeManager.I != null)
                GameModeManager.I.OnRunStart.RemoveListener(StartRunFromUI);
        }

        void Start()
        {
            // 모든 휠 OFF로 초기화
            StopAllGroups();

            // 자동 출발 모드면 플레이 직후 자동 시동
            if (autoStartOnPlay)
            {
                armed = true;
                if (startDelaySec > 0f) Invoke(nameof(StartFromStation), startDelaySec);
                else StartFromStation();
            }
        }

        #endregion
        // ─────────────────────────────────────────────────────

        #region External Controls (UI에서 직접 호출해도 됨)

        /// <summary>UI Start 버튼 → GameModeManager.StartRun() → 여기로 콜백</summary>
        public void StartRunFromUI()
        {
            armed = true;               // 시동 무장
            StartFromStation();         // 즉시 출발
        }

        /// <summary>수동 정지/리셋할 때 호출용</summary>
        public void ResetFlow()
        {
            CancelInvoke();
            StopAllGroups();
            state = 0;
            armed = false;
        }

        #endregion

        #region State Flow

        void OnStationEnter()
        {
            // 기본 정책: 센서로는 출발하지 않음(allowSensorStart=false)
            if (!allowSensorStart)
            {
                // 다만 정지 절차는 유지 (state==4에서 강브레이크)
                if (state == 4)
                {
                    SetBrakeOnly(station, finalBrakeForce);
                    state = 0;
                    armed = false;
                }
                return;
            }

            // 센서로 출발을 허용하는 경우에만 아래 로직 실행
            if (!autoStartOnPlay && !armed) return; // UI로 무장하지 않았으면 무시

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
            // 복귀 중 스테이션을 지나갈 때 약한 브레이크
            if (state == 3)
            {
                state = 4;
                SetBrakeOnly(station, returnBrakeForce);
            }
        }

        void StartFromStation()
        {
            // 스테이션 + 힐1 휠 활성화 (각 그룹의 Direction/Force/MaxSpeed 사용)
            ApplyGroup(station,  true);
            ApplyGroup(lifthill1,true);
            ApplyGroup(lifthill2,false);
            state = 1;
            // 이후 힐1 정상 센서 신호 대기 → OnLifthill1Top()
        }

        void OnLifthill1Top()
        {
            if (state != 1) return;

            // 힐1 정상 도달 → 스테이션/힐1 OFF, 힐2 ON(보통 역방향 설정)
            ApplyGroup(station,  false);
            ApplyGroup(lifthill1,false);
            ApplyGroup(lifthill2,true);
            state = 2;
        }

        void OnLifthill2Top()
        {
            if (state != 2) return;

            // 힐2 정상 도달 → 힐2 OFF, 관성으로 스테이션 복귀
            ApplyGroup(lifthill2,false);
            state = 3;
        }

        #endregion

        #region Group Helpers

        void ApplyGroup(AccelGroup g, bool active)
        {
            if (g == null || g.accelerators == null) return;

            foreach (var acc in g.accelerators)
            {
                if (!acc) continue;

                acc.MaxSpeed  = g.maxSpeed;

                if (active)
                {
                    float signedForce = (int)g.direction * Mathf.Abs(g.force);
                    acc.Force      = signedForce;
                    acc.BreakForce = g.brakeForce;
                }
                else
                {
                    acc.Force      = 0f;
                    acc.BreakForce = 0f;
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
            }
        }

        void StopAllGroups()
        {
            ApplyGroup(station,  false);
            ApplyGroup(lifthill1,false);
            ApplyGroup(lifthill2,false);
        }

        #endregion
    }
}
