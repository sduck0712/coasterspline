// Assets/CoasterSpline/Scripts/myScripts/ChallengeUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CoasterSpline
{
    [DisallowMultipleComponent]
    public class ChallengeUI : MonoBehaviour
    {
        [Header("Intro")]
        public GameObject introGroup;
        public TMP_Text introTitle;
        public TMP_Text introDesc;
        public Button introNextButton; // 다음(규칙 적용 후 대기 화면 전환)

        [Header("Result")]
        public GameObject resultGroup;
        public TMP_Text resultText;
        public TMP_Text resultSub;
        public Button resetButton;     // 초기화(ResetRun)

        [Header("Format")]
        public string speedFmt = "0.0";   // 속도 소수 1자리
        public string heightFmt = "0.0";  // 높이 소수 1자리
        public string energyFmt = "0";    // 에너지 정수

        void Awake()
        {
            ShowIntro(false);
            ShowResult(false);
        }

        public void ShowIntro(bool on)
        {
            if (introGroup) introGroup.SetActive(on);
        }

        public void SetupIntro(MissionData mission)
        {
            if (!mission) return;
            if (introTitle) introTitle.text = mission.title;
            if (introDesc)  introDesc.text  = mission.description;
        }

        public void ShowResult(bool on)
        {
            if (resultGroup) resultGroup.SetActive(on);
        }

        public void SetupResult(bool success, string headline, string subline)
        {
            if (resultText) resultText.text = headline;
            if (resultSub)  resultSub.text  = subline;
            if (resultText) resultText.color = success ? Color.cyan : Color.red;
        }
    }
}
