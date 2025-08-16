using UnityEngine;

namespace CoasterSpline
{
    // 카메라는 Seat 위치를 따라가고, Target을 보되
    // 우클릭 드래그로 시야(yaw/pitch) 오프셋을 줄 수 있습니다.
    public class CoasterCam : MonoBehaviour
    {
        [Header("Follow")]
        [SerializeField] Transform _seat;    // 카메라 위치 기준
        [SerializeField] Transform _target;  // 바라볼 대상(카메라 타겟)
        [Range(0f,1f)] [SerializeField] float _lerpPos = 0.8f;  // 위치 보간
        [Range(0f,1f)] [SerializeField] float _lerpRot = 0.8f;  // 회전 보간

        [Header("Free Look (Mouse Drag)")]
        [SerializeField] bool  _enableFreeLook = true;
        [SerializeField] bool  _requireRightMouse = true; // 우클릭일 때만 회전
        [SerializeField] float _xSpeed = 120f;            // yaw 속도(도/초)
        [SerializeField] float _ySpeed = 80f;             // pitch 속도(도/초)
        [SerializeField] float _yMin = -20f, _yMax = 80f; // pitch 제한
        [SerializeField] float _recentreTau = 0f;         // 드래그 아닐 때 0으로 복귀(초). 0이면 끔
        [SerializeField] bool  _lockCursorWhileDragging = false;

        float _yawOff, _pitchOff;

        void Update()
        {
            // 커서 잠금/해제(선택)
            if (!_enableFreeLook || !_requireRightMouse) return;

            if (Input.GetMouseButtonDown(1) && _lockCursorWhileDragging)
            { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }

            if (Input.GetMouseButtonUp(1) && _lockCursorWhileDragging)
            { Cursor.lockState = CursorLockMode.None;  Cursor.visible = true;  }
        }

        void LateUpdate()
        {
            if (!_seat || !_target) return;

            // 1) 위치 따라가기
            transform.position = Vector3.Lerp(transform.position, _seat.position, _lerpPos);

            // 2) 드래그 입력 → 시야 오프셋 업데이트
            bool dragging = !_enableFreeLook ? false : (!_requireRightMouse || Input.GetMouseButton(1));
            if (dragging)
            {
                _yawOff   += Input.GetAxis("Mouse X") * _xSpeed * Time.deltaTime;
                _pitchOff -= Input.GetAxis("Mouse Y") * _ySpeed * Time.deltaTime;
                _pitchOff  = Mathf.Clamp(_pitchOff, _yMin, _yMax);
            }
            else if (_recentreTau > 0f)
            {
                // 드래그 중이 아니면 서서히 중앙(0,0)으로 복귀
                float a = 1f - Mathf.Exp(-Time.deltaTime / _recentreTau);
                _yawOff   = Mathf.Lerp(_yawOff,   0f, a);
                _pitchOff = Mathf.Lerp(_pitchOff, 0f, a);
            }

            // 3) 기본 바라보기 회전 + 드래그 오프셋 적용
            Quaternion baseRot   = Quaternion.LookRotation(_target.position - transform.position, _seat.up);
            Quaternion offsetRot = Quaternion.Euler(_pitchOff, _yawOff, 0f); // pitch→yaw 오프셋
            Quaternion desired   = baseRot * offsetRot;

            // 4) 회전 보간
            transform.rotation = Quaternion.Slerp(transform.rotation, desired, _lerpRot);
        }
    }
}

