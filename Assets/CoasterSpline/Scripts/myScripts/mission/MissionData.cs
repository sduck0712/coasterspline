// Assets/CoasterSpline/Scripts/myScripts/MissionData.cs
using UnityEngine;

namespace CoasterSpline
{
    public enum GradeLevel { J1, J2, J3 }
    public enum GoalType { SpeedAtCheckpoint, PeakHeightBeforeCheckpoint }
    public enum CompareMode { AtLeast, AtMost, Near } // Near는 |측정-목표|<=허용오차

    [CreateAssetMenu(menuName = "Coaster/MissionData")]
    public class MissionData : ScriptableObject
    {
        [Header("Meta")]
        public string missionId = "J1-01";
        public GradeLevel grade = GradeLevel.J1;
        public string title = "바닥에서 속도 19.8m/s 도달";
        [TextArea] public string description = "바닥 지점에서 속도 목표를 달성하세요.";

        [Header("Goal")]
        public GoalType goal = GoalType.SpeedAtCheckpoint;
        public CompareMode compare = CompareMode.AtLeast;
        public float targetValue = 19.8f;
        public float tolerance = 0.2f; // Near/AtLeast/AtMost에서 여유값

        [Header("Checkpoint")]
        public CoasterSensor checkpoint; // 해당 지점 센서(필수)

        [Header("Rules")]
        public bool forceFrictionOn = true; // 챌린지 동안 마찰 강제 On
        public bool lockEditing     = true; // 높이/질량/슬라이더 잠금
        public bool lockStartHeight = true;
        public bool lockMass        = false;

        [Header("Initial Conditions")]
        public float startSpeed = 0f;  // m/s
        public float massOverride = -1f; // <0이면 무시

        [Header("Cameras & FX")]
        public Transform introFocus;            // 미션 소개 카메라 포커스(옵션)
        public Vector3 introOffset = new Vector3(-6,3,-6);
        public float slowmoScale = 0.25f;
        public float slowmoHoldRealtime = 1.2f;

        [Header("UI Text")]
        public string successText = "성공!";
        public string failText    = "실패";
    }
}

