// Assets/CoasterSpline/Scripts/myScripts/AppController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

namespace CoasterSpline
{
    /// <summary>
    /// 탐색(Explore) UI와 시스템 제어.
    /// - 높이 슬라이더(0..1) → StartRegionBinder.SetHeight01
    /// - 마찰 토글 → FrictionController
    /// - 질량/초기속도 입력 → HUD_All_TMP / Rigidbody
    /// - 카메라 모드 전환(팔로우/자유시야)
    /// - Run 제어: AppController는 물리/UI만 처리. (이벤트/플로우는 GameModeManager가 담당)
    /// </summary>
    [DisallowMultipleComponent]
    public class AppController : MonoBehaviour
    {
        [Header("Refs")]
        [Tooltip("레일/스테이션/기둥/가속기/기차를 함께 올려주는 바인더")]
        public StartRegionBinder startBinder;

        [Tooltip("Train의 Rigidbody (초기 속도 세팅 등)")]
        public Rigidbody trainRb;

        [Tooltip("초기 속도 방향 기준(없으면 trainRb.transform.forward)")]
        public Transform trainForward;

        [Tooltip("마찰 컨트롤러(있으면 토글로 on/off)")]
        public FrictionController friction;

        [Tooltip("팔로우 카메라 스크립트(CoasterCam 등)")]
        public Behaviour coasterCam;

        [Tooltip("자유 시야 카메라 스크립트(MouseOrbitDrag 등)")]
        public Behaviour mouseOrbit;

        [Header("Controllers")]
        [Tooltip("가감속/센서 상태 머신 (이벤트는 GameModeManager에서 쏨)")]
        public BoomerangController boomerang;

        [Header("Explore UI")]
        [Tooltip("시작 높이 슬라이더 (0..1 범위 권장)")]
        public Slider heightSlider;

        [Tooltip("시작 높이 텍스트 입력(선택). 값은 0..1로 해석")]
        public TMP_InputField heightInput;

        [Tooltip("질량(kg) 입력 → HUD_All_TMP.mass")]
        public TMP_InputField massInput;

        [Tooltip("초기 속도(m/s) 입력 → StartRun때 적용")]
        public TMP_InputField startSpeedInput;

        [Tooltip("마찰 on/off")]
        public Toggle frictionToggle;

        [Header("HUD (optional)")]
        public HUD_All_TMP hud;

        void Start()
        {
            // UI 바인딩
            if (heightSlider) heightSlider.onValueChanged.AddListener(OnHeightSliderChanged);
            if (heightInput)  heightInput.onEndEdit.AddListener(OnHeightInputCommitted);
            if (massInput)    massInput.onEndEdit.AddListener(OnMassInputCommitted);
            if (frictionToggle) frictionToggle.onValueChanged.AddListener(on => friction?.SetEnabled(on));

            // 기본 카메라: 팔로우
            SetFreeCam(false);
        }

        // ── Run 제어: 버튼은 GameModeManager를 호출. GameModeManager가 아래 메서드를 호출함 ──

        /// <summary>주행 시작: 물리/입력만 처리 (이벤트는 GameModeManager가 브로드캐스트)</summary>
        public void StartRun()
        {
            // 입력 잠금
            SetExploreInteractable(false);

            // 초기 속도 적용(선택)
            if (trainRb)
            {
                trainRb.isKinematic = false;

                float v0 = 0f;
                if (startSpeedInput && float.TryParse(startSpeedInput.text, out var vParsed))
                    v0 = Mathf.Max(0f, vParsed);

                Vector3 dir = trainForward ? trainForward.forward :
                               (trainRb ? trainRb.transform.forward : Vector3.forward);

                trainRb.velocity = dir.normalized * v0;
                trainRb.angularVelocity = Vector3.zero;
            }

            // HUD 기준 초기화
            hud?.ResetBaseline();

            // ❌ 여기서 GameModeManager.StartRun() 호출 금지 (순환 방지)
            // ❌ boomerang.StartRunFromUI() 직접 호출 금지 (이벤트 중복 방지)
        }

        /// <summary>주행 종료: UI 잠금 해제 등</summary>
        public void EndRun()
        {
            // 물리 정지는 EndRun 대신 ResetRun에서 확실히 처리
            SetExploreInteractable(true);

            // ❌ 여기서 GameModeManager.EndRun() 호출 금지
            // ❌ boomerang.ResetFlow() 직접 호출 금지 (OnRunEnd 이벤트에서 처리)
        }

        /// <summary>빠른 리셋(씬 리로드 없이)</summary>
        public void ResetRun()
        {
            if (trainRb)
            {
                trainRb.velocity = Vector3.zero;
                trainRb.angularVelocity = Vector3.zero;
            }

            hud?.ResetBaseline();
            SetExploreInteractable(true);

            // ❌ 여기서 GameModeManager.ResetRun() 호출 금지
            // boomerang의 흐름 초기화는 GameModeManager가 OnRunEnd/Reset 시점에 이벤트로 처리
        }

        // ── 카메라 모드 전환 ──
        public void SetFreeCam(bool free)
        {
            if (coasterCam) coasterCam.enabled = !free;
            if (mouseOrbit) mouseOrbit.enabled = free;
        }

        // ── UI 콜백 ──
        void OnHeightSliderChanged(float t01)
        {
            if (!startBinder) return;
            t01 = Mathf.Clamp01(t01);
            startBinder.SetHeight01(t01);

            if (heightInput) heightInput.text = t01.ToString("0.##");
        }

        void OnHeightInputCommitted(string text)
        {
            if (!startBinder) return;
            if (!float.TryParse(text, out var t01)) return;
            t01 = Mathf.Clamp01(t01);

            if (heightSlider) heightSlider.SetValueWithoutNotify(t01);
            startBinder.SetHeight01(t01);
        }

        void OnMassInputCommitted(string text)
        {
            if (!hud) return;
            if (float.TryParse(text, out var m)) hud.mass = Mathf.Max(0f, m);
        }

        void SetExploreInteractable(bool on)
        {
            if (heightSlider)     heightSlider.interactable = on;
            if (heightInput)      heightInput.interactable  = on;
            if (massInput)        massInput.interactable    = on;
            if (startSpeedInput)  startSpeedInput.interactable = on;
            if (frictionToggle)   frictionToggle.interactable = on;
        }

        // ── 모드 전환용 단축(매니저가 호출) ──
        public void GoExplore()    { EndRun(); SetFreeCam(false); }
        public void GoExperiment() { EndRun(); }
        public void GoChallenge()  { EndRun(); }

        // (선택) 씬 리로드형 리셋
        public void RestartScene()
            => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
