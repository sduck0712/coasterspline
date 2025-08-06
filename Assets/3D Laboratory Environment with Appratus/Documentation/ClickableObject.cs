using UnityEngine;

public class ClickableObject : MonoBehaviour
{
    [Header("Camera Targets")]
    public Transform cameraFocusPoint;

    [Header("Animation Settings")]
    public Animator doorAnimator;
    public string openTrigger = "Open";
    public string closeTrigger = "Close";

    [Header("UI")]
    public GameObject backButton;

    [Header("Door Collider Control")]
    public Collider doorCollider;

    [Header("Showcase 연동")]
    public ShowcaseManager showcaseManager; // 반드시 Inspector에서 연결

    private bool isOpen = false;

    private void Start()
    {
        if (backButton != null)
            backButton.SetActive(false);
    }

    private void OnMouseDown()
    {
        if (isOpen) return;

        Debug.Log("문 클릭됨 → 열기");

        Camera.main.GetComponent<CameraController>().SetTarget(cameraFocusPoint);

        if (doorAnimator != null)
            doorAnimator.SetTrigger(openTrigger);

        if (doorCollider != null)
            doorCollider.enabled = false;

        if (backButton != null)
            backButton.SetActive(true);

        // 🔴 Plate 생성
        if (showcaseManager != null)
            showcaseManager.OpenShowcase();

        isOpen = true;
        var mgr = GameObject.Find("ShowcaseManager")
                          .GetComponent<ShowcaseManager>();
        mgr.OpenShowcase();
    }

    public void CloseAndReturn()
    {
        Debug.Log("뒤로가기 버튼 클릭됨 → 닫기 & 카메라 복귀");

        if (doorAnimator != null)
            doorAnimator.SetTrigger(closeTrigger);

        if (doorCollider != null)
            doorCollider.enabled = true;

        Camera.main.GetComponent<CameraController>().ReturnToInitialPosition();

        if (backButton != null)
            backButton.SetActive(false);

        // 🔴 Plate 제거
        if (showcaseManager != null)
            showcaseManager.CloseShowcase();

        isOpen = false;
    }
}
