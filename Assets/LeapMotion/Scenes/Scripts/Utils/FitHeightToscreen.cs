using UnityEngine;
using UnityEngine.UI; // UI 네임스페이스 추가

public class FitHeightToScreen : MonoBehaviour
{
    private RawImage rawImage;

    void Awake()
    {
        rawImage = GetComponent<RawImage>();
        if (rawImage == null || rawImage.texture == null)
        {
            Debug.LogError("RawImage 또는 Texture가 없습니다!");
            return;
        }

        float width_height_ratio = (float)rawImage.texture.width / rawImage.texture.height;
        float width = width_height_ratio * Screen.height;
        float x_offset = (Screen.width - width) / 2.0f;

        // RectTransform으로 크기와 위치 조정
        RectTransform rt = rawImage.rectTransform;
        rt.anchorMin = new Vector2(0, 0); // 좌측 하단
        rt.anchorMax = new Vector2(0, 1); // 좌측 상단
        rt.pivot = new Vector2(0, 0.5f);  // 좌측 중앙
        rt.sizeDelta = new Vector2(width, Screen.height);
        rt.anchoredPosition = new Vector2(x_offset, 0);
    }
}
