using UnityEngine;
using TMPro; // TextMeshPro를 쓴다면 필요

public class ShowNameOnHover : MonoBehaviour
{
    [Header("Inspector에서 연결")]
    public GameObject nameLabelPrefab; // NameLabelPrefab 프리팹 드래그해서 연결
    public string labelText = "이름";    // Inspector에서 입력

    private GameObject currentLabel;     // 생성된 라벨 오브젝트
    private Canvas mainCanvas;           // 씬 내 UI Canvas

    void Start()
    {
        // Canvas 자동 탐색 (여러 Canvas 중 첫 번째)
        mainCanvas = FindObjectOfType<Canvas>();
    }

    void OnMouseEnter()
    {
        if (nameLabelPrefab != null && currentLabel == null && mainCanvas != null)
        {
            // 라벨 프리팹 Canvas 하위로 인스턴스화
            currentLabel = Instantiate(nameLabelPrefab, mainCanvas.transform);

            // TMP_Text 또는 Text 컴포넌트에 텍스트 입력
            TMP_Text text = currentLabel.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = labelText;

            // 최초 위치 지정 (오브젝트 위)
            UpdateLabelPosition();
        }
    }

    void OnMouseExit()
    {
        if (currentLabel != null)
        {
            Destroy(currentLabel);
            currentLabel = null;
        }
    }

    void Update()
    {
        // 라벨이 표시 중일 때, 계속 위치 업데이트
        if (currentLabel != null)
        {
            UpdateLabelPosition();
        }
    }

    // 오브젝트 위에 딱 맞는 위치 계산
    void UpdateLabelPosition()
    {
        // 오브젝트의 월드 좌표 + 높이의 일부만큼 Y축으로 올림
        float objHeight = 1.0f;
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
            objHeight = rend.bounds.size.y;

        // 간격 조절: 0.2~0.4f 사이로 조절하면 적당히 붙음
        Vector3 worldAbove = transform.position + Vector3.up * (objHeight * 0.35f);
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldAbove);
        currentLabel.transform.position = screenPos;
    }
}
