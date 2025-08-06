using UnityEngine;

public class PlateFollowCamera : MonoBehaviour
{
    [Header("카메라로부터의 오프셋 (로컬 X,Y,Z)")]
    public Vector3 offsetFromCamera = new Vector3(0f, -0.8f, 1.6f);

    // 스폰 시점의 회전을 기억
    private Quaternion spawnRotation;

    void Start()
    {
        // Instantiate 직후의 회전을 저장
        spawnRotation = transform.rotation;
    }

    void LateUpdate()
    {
        // 1) 위치만 카메라 따라가기
        Transform cam = Camera.main.transform;
        transform.position = cam.TransformPoint(offsetFromCamera);

        // 2) 회전은 스폰 시점에 고정
        transform.rotation = spawnRotation;
    }
}
