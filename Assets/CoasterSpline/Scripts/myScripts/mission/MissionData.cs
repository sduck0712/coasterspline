// Assets/CoasterSpline/Scripts/myScripts/mission/MissionData.cs
using UnityEngine;

namespace CoasterSpline
{
    public enum GoalType
    {
        SpeedAtCheckpoint,           // 체크포인트 통과 시 속도
        PeakHeightBeforeCheckpoint   // 체크포인트 도달 전 최고 높이
    }

    public enum CompareMode
    {
        AtLeast,    // >= target - tol
        AtMost,     // <= target + tol
        Near        // |value - target| <= |tol|
    }

    /// <summary>
    /// 챌린지 미션 데이터 (ScriptableObject)
    /// - checkpoint(직접참조) 또는 checkpointId(문자열, 선택)를 통해 체크포인트 지정
    /// - 표시문구/룰/연출값 포함
    /// </summary>
    [CreateAssetMenu(menuName = "Coaster/Mission Data", fileName = "New Mission Data")]
    public class MissionData : ScriptableObject
    {
        [Header("Meta")]
        public string missionId = "";      // 내부 식별자(선택)
        public string grade    = "J1";     // 학년/난도 표시(예: J1)

        [Header("Texts")]
        public string title       = "미션 제목";
        [TextArea] public string description = "미션 설명";
        public string successText = "성공!";
        public string failText    = "실패";

        [Header("Goal")]
        public GoalType   goal     = GoalType.SpeedAtCheckpoint;
        public CompareMode compare = CompareMode.AtLeast;
        public float      targetValue = 0f;   // 목표값(속도 m/s, 높이 m 등)
        public float      tolerance   = 0.1f; // 허용 오차

        [Header("Checkpoint")]
        [Tooltip("체크포인트 센서를 직접 지정하는 기본 방식")]
        public CoasterSensor checkpoint;      // 직접 참조 방식

        [Tooltip("문자열 ID로 지정하고 싶을 때 사용(선택). " +
                 "ChallengeManager의 Checkpoints 배열에서 같은 id와 센서를 매핑하면 이 값이 우선됩니다.")]
        public string checkpointId = "";      // 매핑 방식(선택)

        [Tooltip("연출용 포커스(선택). 비우면 checkpoint의 트랜스폼을 사용")]
        public Transform focusOverride;

        [Header("Run Rules")]
        [Tooltip("마찰 강제 On")]
        public bool forceFrictionOn = true;

        [Tooltip("탐색/편집 잠금")]
        public bool lockEditing = true;

        [Tooltip("시작 높이 슬라이더 비활성화(잠금)")]
        public bool lockStartHeight = true;

        [Tooltip("초기 속도(m/s)")]
        public float startSpeed = 0f;

        [Tooltip("질량(kg) 오버라이드. 미사용은 음수로")]
        public float massOverride = -1f;

        [Header("Presentation")]
        [Range(0.05f, 1f)]
        public float slowmoScale = 0.25f;       // 슬로모 배속
        public float slowmoHoldRealtime = 1.2f; // 결과 유지 실시간(s)
    }
}
