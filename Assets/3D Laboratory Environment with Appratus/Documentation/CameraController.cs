using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target;
    public float moveSpeed = 3f;
    public float rotationSpeed = 2f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private bool returningToInitial = false;

    private void Start()
    {
        // 시작 위치 기억
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    private void Update()
    {
        if (target != null)
        {
            // 지정된 타겟으로 부드럽게 이동
            transform.position = Vector3.Lerp(transform.position, target.position, Time.deltaTime * moveSpeed);

            Quaternion targetRot = Quaternion.LookRotation(target.forward);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
        }
        else if (returningToInitial)
        {
            // 초기 위치로 부드럽게 복귀
            transform.position = Vector3.Lerp(transform.position, initialPosition, Time.deltaTime * moveSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, initialRotation, Time.deltaTime * rotationSpeed);

            // 충분히 도달했으면 멈춤
            if (Vector3.Distance(transform.position, initialPosition) < 0.01f)
            {
                returningToInitial = false;
            }
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        returningToInitial = false;
    }

    public void ReturnToInitialPosition()
    {
        target = null; // 자동 이동 중단
        returningToInitial = true;
    }
}
