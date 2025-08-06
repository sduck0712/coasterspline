using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class EnergyBarGraphManager : MonoBehaviour
{
    [Header("Tracked Rigidbodies")]
    public Rigidbody[] trackedObjects;

    [Header("Bar Parents (Contain Cube)")]
    public Transform peBar;
    public Transform keBar;

    [Header("Text Labels")]
    public TextMeshPro peLabel;
    public TextMeshPro keLabel;

    [Header("Text Values")]
    public TextMeshPro peValueText;
    public TextMeshPro keValueText;

    [Header("Graph Settings")]
    public float yScale = 0.01f;
    public float barWidth = 0.2f;

    [Header("Ground Plane")]
    public Transform groundPlane;
    private float baseY;

    void Start()
    {
        // 바닥 기준 Y값
        baseY = groundPlane != null ? groundPlane.position.y : 0f;

        // ✅ 기본 질량 100g (0.1kg)
        foreach (var obj in trackedObjects)
        {
            obj.mass = 0.1f;
        }
    }

    void Update()
    {
        float totalPE = 0f;
        float totalKE = 0f;

        foreach (var obj in trackedObjects)
        {
            float bottomY = obj.transform.position.y - obj.transform.localScale.y / 2f;
            float height = Mathf.Max(bottomY - baseY, 0f);

            float pe = obj.mass * 9.81f * height;
            float ke = 0.5f * obj.mass * obj.velocity.sqrMagnitude;

            totalPE += pe;
            totalKE += ke;
        }

        UpdateBar(peBar, totalPE);
        UpdateBar(keBar, totalKE);

        UpdateText(peValueText, totalPE);
        UpdateText(keValueText, totalKE);
    }

    void UpdateBar(Transform barParent, float value)
    {
        Transform cube = barParent.GetChild(0);
        float targetHeight = value * yScale;
        float currentHeight = cube.localScale.y;
        float newHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * 10f);

        cube.localScale = new Vector3(barWidth, newHeight, barWidth);
        cube.localPosition = new Vector3(0f, newHeight / 2f, 0f);
    }

    void UpdateText(TextMeshPro textMesh, float value)
    {
        textMesh.text = value.ToString("F1");
    }

    // ✅ 버튼에서 호출될 질량 설정 함수
    public void SetMass(float mass)
    {
        foreach (var obj in trackedObjects)
        {
            obj.mass = mass;
        }
    }
}
