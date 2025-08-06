using UnityEngine;
using UnityEngine.UI;

public class StretchToScreen : MonoBehaviour
{
    void Awake()
    {
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            // 화면 전체로 Stretch
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
