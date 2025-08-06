using UnityEngine;

public class MagneticZone : MonoBehaviour
{
    public float magneticForce = 5f;        // 자석 세기
    public float magneticRange = 1.5f;      // 적용 범위
    public Transform attractTarget;         // 붙을 위치 (선택사항)

    private void OnTriggerStay(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if (rb != null && other.CompareTag("MagneticObject"))
        {
            float distance = Vector3.Distance(transform.position, other.transform.position);
            if (distance <= magneticRange)
            {
                Vector3 direction = (transform.position - other.transform.position).normalized;
                rb.AddForce(direction * magneticForce, ForceMode.Acceleration);
            }
        }
    }
}
