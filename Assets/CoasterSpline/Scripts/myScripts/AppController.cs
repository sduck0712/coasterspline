// Assets/CoasterSpline/Scripts/myScripts/AppController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

namespace CoasterSpline
{
    /// <summary>
    /// 탐색(Explore) UI와 시스템 제어.
    /// - 시작 높이(절대/상대) 조절 -> StartRegionBinder 호출
    /// - 마찰 토글 -> FrictionController
    /// - 질량/초기속도 입력 -> HUD_All_TMP / Rigidbody
    /// - 카메라 모드 전환(팔로우/자유시야)
    /// - Run 제어(Start/End/Reset) 및 GameModeManager와 연동
    /// </summary>
    [DisallowMultipleComponent]
    public class AppController : MonoBehaviour
    {
        [Header("Refs")]
        [Tooltip("레일/스테이션/기둥/가속기/기차를 함께 올려주는 바인더")]
        public StartRegionBinder startBinder;     // 필수 권장

        [Tooltip("Train의 Rigidbody (초기 속도 세팅 등)")]
        public Rigidbody trainRb;                // 선택(있으면 StartRun에서 초기속도 적용)

        [Tooltip("초기 속도 방향 기준(없으면 trainRb.transform.forward)")]
        public Transform trainForward;           // 선택

        [Tooltip("마찰 컨트롤러(있으면 토글로 on/off)")]
        public FrictionController friction;      // 선택

        [Tooltip("팔로우 카메라 스크립트(CoasterCam 등)")]
        public Behaviour coasterCam;             // 선택

        [Tooltip("자유 시야 카메라 스크립트(MouseOrbitDrag 등)")]
        public Behaviour mouseOrbit;             // 선택

        [Header("Explore UI")]
        [Tooltip("시작 높이 슬라이더(절대 높이로 쓰면 min/max를 미터로 세팅)")]
        public Slider heightSlider;              // 선택

        [Tooltip("시작 높이(미터) 입력창")]
        public TMP_InputField heightInput;       // 선택

        [Tooltip("질량(kg) 입력 -> HUD_All_TMP.mass 에 반영")]
        public TMP_InputField massInput;         // 선택

        [Tooltip("초기 속도(m/s) 입력 -> StartRun때 적용")]
        public TMP_InputField startSpeedInput;   // 선택

        [Tooltip("마찰 on/off")]
        public Toggle frictionToggle;            // 선택

        [Header("HUD (optional)")]
        public HUD_All_TMP hud;                  // 선택

        // ─────────────────────────────────────────────────────

        void Start()
        {
            // UI 바인딩
            if (heightSlider) heightSlider.onValueChanged.AddListener(OnHeightSliderChanged);
            if (heightInput)  heightInput.onEndEdit.AddListener(OnHeightInputCommitted);
            if (massInput)    massInput.onEndEdit.AddListener(OnMassInputCommitted);
            if (startSpeedInput) startSpeedInput.onEndEdit.AddListener(_ => { /* 값은 StartRun에서 읽음 */ });
            if (frictionToggle)  frictionToggle.onValueChanged.AddListener(on => friction?.SetEnabled(on));

            // 기본 카메라 모드: 팔로우 (원하면 SetFreeCam(true))
            SetFreeCam(false);
        }

        // ── Run 제어 (GameModeManager에서 호출 가능) ──────────────

        /// <summary>주행 시작(가속/센서 플로우는 BoomerangController가 이벤트로 시동)</summary>
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

            hud?.ResetBaseline();
        }

        /// <summary>주행 종료(편집/입력 다시 활성화)</summary>
        public void EndRun()
        {
            SetExploreInteractable(true);
        }

        /// <summary>빠른 리셋(씬 리로드 없이)</summary>
        public void ResetRun()
        {
            // 물리 정지
            if (trainRb)
            {
                trainRb.velocity = Vector3.zero;
                trainRb.angularVelocity = Vector3.zero;
                // 필요 시 위치 되돌리기는 StartRegionBinder가 관리(현재 offset을 기준으로 유지)
            }

            hud?.ResetBaseline();
            SetExploreInteractable(true);
        }

        // ── 카메라 모드 전환 ────────────────────────────────────
        public void SetFreeCam(bool free)
        {
            if (coasterCam)  coasterCam.enabled  = !free;
            if (mouseOrbit)  mouseOrbit.enabled  = free;
        }

        // ── UI 콜백 ────────────────────────────────────────────

        void OnHeightSliderChanged(float value)
        {
            if (!startBinder) return;

            // StartRegionBinder에 "Use Absolute Height" 옵션이 있는 경우:
            // - 슬라이더 min/max를 미터 단위로 맞춰 두고, 값을 그대로 넘김
            if (startBinder.useAbsoluteHeight)
            {
                startBinder.SetAbsoluteHeight(value);
                if (heightInput) heightInput.text = value.ToString("0.##");
            }
            else
            {
                // 0~1 → min~max offset
                startBinder.SetHeight01(value);
                // 절대값 표시를 원하면 binder 쪽에서 현재 절대높이를 반환하는 함수를 추가해 사용
            }
        }

        void OnHeightInputCommitted(string text)
        {
            if (!startBinder) return;
            if (!float.TryParse(text, out var meters)) return;

            startBinder.SetAbsoluteHeight(meters);
            if (heightSlider) heightSlider.value = meters;  // 슬라이더를 절대높이로 쓰는 구성이면 동기화
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
        }

        // ── GameModeManager 호환 어댑터(컴파일 오류 해결 포인트) ──
        public void GoExplore()
        {
            // 탐색 모드에선 편집/입력 가능, 자유시야는 기본 off(원하면 true)
            EndRun();
            SetFreeCam(false);
        }

        public void GoExperiment()
        {
            // 필요 시 탐색과 다르게 잠금/해제 정책을 넣을 수 있음
            EndRun();
        }

        public void GoChallenge()
        {
            EndRun();
        }

        // (선택) 씬 리로드형 리셋이 필요할 때 호출할 수도 있음
        public void RestartScene()
            => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
