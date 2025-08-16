using UnityEngine;
using System;

public class EnergyTrackerKP : MonoBehaviour
{
    [Header("Target (실제로 움직이는 Cart Transform 권장)")]
    public Transform trainTf;      // Cart(0) 같은 실제 이동 Transform
    public Rigidbody trainRb;      // 있으면 참고(없어도 됨)

    [Header("Physics")]
    public float mass = 500f;
    public float g = 9.81f;

    // OnEnergyKP(Ek, Ep, E0)
    public event Action<float,float,float> OnEnergyKP;

    [Header("속도 평활화")]
    public float sampleHz = 30f;
    public float smoothTau = 0.2f; // 크게 할수록 부드러움

    Vector3 prevPos;
    float y0, E0, vFiltered, accT;
    bool inited;

    void Start()
    {
        if (!trainTf && trainRb) trainTf = trainRb.transform;
        if (!trainTf) { enabled = false; return; }

        prevPos = trainTf.position;
        y0 = trainTf.position.y;

        float v0 = (trainRb && !trainRb.isKinematic) ? trainRb.velocity.magnitude : 0f;
        E0 = 0.5f*mass*v0*v0 + mass*g*Mathf.Max(0f, trainTf.position.y - y0);

        inited = true;
    }

    void Update()
    {
        if (!inited) return;

        float dt = Mathf.Max(Time.deltaTime, 1e-6f);
        accT += dt;
        float step = 1f / Mathf.Max(1f, sampleHz);
        if (accT < step) return;

        float v = (trainRb && !trainRb.isKinematic)
            ? trainRb.velocity.magnitude
            : (trainTf.position - prevPos).magnitude / accT;

        prevPos = trainTf.position;
        accT = 0f;

        // 지수평활
        float a = 1f - Mathf.Exp(-dt / Mathf.Max(0.01f, smoothTau));
        vFiltered = Mathf.Lerp(vFiltered, v, a);

        float h  = Mathf.Max(0f, trainTf.position.y - y0);
        float Ek = 0.5f * mass * vFiltered * vFiltered;
        float Ep = mass * g * h;

        OnEnergyKP?.Invoke(Ek, Ep, E0);
    }

    // 실험 시작 전에 한 번 호출해서 기준 재설정
    public void ResetBaseline()
    {
        if (!trainTf) return;
        y0 = trainTf.position.y;
        prevPos = trainTf.position;

        float v0 = (trainRb && !trainRb.isKinematic) ? trainRb.velocity.magnitude : 0f;
        E0 = 0.5f*mass*v0*v0 + mass*g*Mathf.Max(0f, trainTf.position.y - y0);
        vFiltered = 0f;
    }
}
