// ModeUIBinder.cs
using UnityEngine;
using UnityEngine.UI;

namespace CoasterSpline
{
    [DisallowMultipleComponent]
    public class ModeUIBinder : MonoBehaviour
    {
        [Header("Buttons")]
        public Button exploreBtn;
        public Button experimentBtn;
        public Button challengeBtn;
        public Button startBtn;
        public Button endBtn;
        public Button resetBtn;

        GameModeManager gm;
        AppController app;

        void Awake()
        {
            // 같은 네임스페이스의 관리자/컨트롤러 자동 배선
            gm  = FindObjectOfType<GameModeManager>(true);
            app = FindObjectOfType<AppController>(true);

            // 안전 가드
            if (!gm) Debug.LogWarning("[ModeUIBinder] GameModeManager를 씬에서 찾지 못했습니다.");
            if (!app) Debug.Log("[ModeUIBinder] AppController가 없어도 동작은 계속됩니다.");

            // 중복 방지 위해 리스너 초기화 후 바인딩
            if (exploreBtn)
            {
                exploreBtn.onClick.RemoveAllListeners();
                exploreBtn.onClick.AddListener(() => gm?.SetModeExplore());
            }
            if (experimentBtn)
            {
                experimentBtn.onClick.RemoveAllListeners();
                experimentBtn.onClick.AddListener(() => gm?.SetModeExperiment());
            }
            if (challengeBtn)
            {
                challengeBtn.onClick.RemoveAllListeners();
                challengeBtn.onClick.AddListener(() => gm?.SetModeChallenge());
            }
            if (startBtn)
            {
                startBtn.onClick.RemoveAllListeners();
                startBtn.onClick.AddListener(() => gm?.StartRun());
            }
            if (endBtn)
            {
                endBtn.onClick.RemoveAllListeners();
                endBtn.onClick.AddListener(() => gm?.EndRun());
            }
            if (resetBtn)
            {
                resetBtn.onClick.RemoveAllListeners();
                resetBtn.onClick.AddListener(() =>
                {
                    // 빠른 리셋: AppController 있으면 호출, 없으면 GameModeManager로만 처리
                    if (app) app.ResetRun(); else gm?.ResetRun();
                });
            }
        }

        // 필요 시 외부에서 수동 재바인딩할 수 있게 공개 API 제공
        public void Bind(GameModeManager manager, AppController controller)
        {
            gm = manager; app = controller; Awake(); // 간단히 다시 묶기
        }
    }
}
