// GameModeManager.cs
using UnityEngine;
using UnityEngine.Events;

namespace CoasterSpline
{
    public enum GameMode { Explore, Experiment, Challenge }

    /// <summary>
    /// 전역 게임 모드/런 흐름 관리.
    /// - 모드 전환(탐색/실험/챌린지)
    /// - 러닝 시작/종료/리셋 이벤트 브로드캐스트
    /// - AppController와 느슨하게 연동(있으면 호출, 없으면 이벤트만 발생)
    /// </summary>
    [DisallowMultipleComponent]
    public class GameModeManager : MonoBehaviour
    {
        public static GameModeManager I { get; private set; }

        [SerializeField] GameMode _mode = GameMode.Explore;
        public GameMode Mode => _mode;

        [System.Serializable] public class ModeChangedEvent : UnityEvent<GameMode> {}

        [Header("Events")]
        public ModeChangedEvent OnModeChanged; // 현재 모드 전달
        public UnityEvent OnRunStart;          // 주행 시작
        public UnityEvent OnRunEnd;            // 주행 종료(멈춤)
        public UnityEvent OnRunReset;          // ★ 리셋(초기화)

        [Header("Optional Wiring")]
        [Tooltip("있으면 패널/슬라이더 상태 변경을 맡깁니다.")]
        public AppController app;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;

            if (!app) app = FindObjectOfType<AppController>(true);
        }

        // ── 모드 제어 ─────────────────────────────────────────────
        public void SetMode(GameMode m)
        {
            if (_mode == m) { OnModeChanged?.Invoke(_mode); return; }
            _mode = m;

            // AppController가 있으면 패널 전환도 위임
            if (app)
            {
                switch (_mode)
                {
                    case GameMode.Explore:    app.GoExplore();    break;
                    case GameMode.Experiment: app.GoExperiment(); break;
                    case GameMode.Challenge:  app.GoChallenge();  break;
                }
            }

            OnModeChanged?.Invoke(_mode);
            Debug.Log($"[GameModeManager] Mode => {_mode}");
        }

        public void SetModeExplore()    => SetMode(GameMode.Explore);
        public void SetModeExperiment() => SetMode(GameMode.Experiment);
        public void SetModeChallenge()  => SetMode(GameMode.Challenge);

        // ── 런 흐름 ───────────────────────────────────────────────
        /// <summary>주행/실험 시작</summary>
        public void StartRun()
        {
            app?.StartRun();       // UI/슬라이더 등 내부 처리(있으면)
            OnRunStart?.Invoke();  // 물리/컨트롤러들은 이 이벤트에 반응
            Debug.Log("[GameModeManager] Run Start");
        }

        /// <summary>주행/실험 종료</summary>
        public void EndRun()
        {
            app?.EndRun();
            OnRunEnd?.Invoke();    // 브레이크/정지 등은 이 이벤트 구독자에서 처리
            Debug.Log("[GameModeManager] Run End");
        }

        /// <summary>빠른 리셋(씬 리로드 없이)</summary>
        public void ResetRun()
        {
            app?.ResetRun();
            OnRunReset?.Invoke();  // ★ 리셋 이벤트 브로드캐스트
            Debug.Log("[GameModeManager] Run Reset");
        }

#if UNITY_EDITOR
        [ContextMenu("Set Explore")]     void _ctxExplore()   => SetModeExplore();
        [ContextMenu("Set Experiment")]  void _ctxExperiment()=> SetModeExperiment();
        [ContextMenu("Set Challenge")]   void _ctxChallenge() => SetModeChallenge();
        [ContextMenu("Start Run")]       void _ctxStart()     => StartRun();
        [ContextMenu("End Run")]         void _ctxEnd()       => EndRun();
        [ContextMenu("Reset Run")]       void _ctxReset()     => ResetRun();
#endif
    }
}
