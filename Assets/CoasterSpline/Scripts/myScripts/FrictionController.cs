using UnityEngine;

[RequireComponent(typeof(Rigidbody))]   // ← 여기 꼭 Rigidbody!
public class FrictionController : MonoBehaviour
{
    public bool enabledAtStart = true;
    public float kLinear = 5f;
    public float cQuadratic = 0.5f;
    public float rollingMu = 0.001f;
    public float g = 9.81f;

    Rigidbody rb; bool useFriction;

    void Awake(){ rb = GetComponent<Rigidbody>(); SetEnabled(enabledAtStart); }
    public void SetEnabled(bool on)=> useFriction = on;

    void FixedUpdate()
    {
        if (!useFriction) return;
        var v = rb.velocity; float s = v.magnitude;
        if (s > 0.001f)
        {
            var drag = -(kLinear * v + cQuadratic * s * v);
            rb.AddForce(drag, ForceMode.Force);
            var roll = -rollingMu * rb.mass * g * v.normalized;
            rb.AddForce(roll, ForceMode.Force);
        }
    }
}
