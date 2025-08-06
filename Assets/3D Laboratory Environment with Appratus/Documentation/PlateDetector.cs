using UnityEngine;
using System.Collections.Generic;

public class PlateDetector : MonoBehaviour
{
    [Header("메인테이블 Anchor (Inspector에서 연결)")]
    public Transform leftAnchor;
    public Transform rightAnchor;
    public int maxPerRow = 6;
    public string excludeTag = "Door";

    // Plate 위에 올라간 원본 오브젝트들
    private List<GameObject> plateOriginals = new List<GameObject>();
    // 원본의 초기 위치·회전 저장
    private Dictionary<GameObject, Vector3> originalPositions = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, Quaternion> originalRotations = new Dictionary<GameObject, Quaternion>();

    // 복제 클론 관리
    private List<GameObject> clones = new List<GameObject>();
    private HashSet<GameObject> alreadyCloned = new HashSet<GameObject>();
    private int cloneCount = 0;

    private void OnTriggerEnter(Collider other)
    {
        // 1) 제외 태그
        if (!string.IsNullOrEmpty(excludeTag) && other.CompareTag(excludeTag))
            return;

        // 2) 첫 방문이면 “원본” 리스트에 추가 + 위치 저장
        if (!plateOriginals.Contains(other.gameObject))
        {
            plateOriginals.Add(other.gameObject);
            originalPositions[other.gameObject] = other.transform.position;
            originalRotations[other.gameObject] = other.transform.rotation;
        }

        // 3) 복제 논리
        if (alreadyCloned.Contains(other.gameObject) || cloneCount >= maxPerRow)
            return;

        float t = (float)cloneCount / Mathf.Max(1, maxPerRow - 1);
        Vector3 spawnPos = Vector3.Lerp(leftAnchor.position, rightAnchor.position, t);
        Quaternion spawnRot = Quaternion.Lerp(leftAnchor.rotation, rightAnchor.rotation, t);

        GameObject clone = Instantiate(other.gameObject, spawnPos, spawnRot);
        clone.name = other.name + "_Clone";

        var rb = clone.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        clones.Add(clone);
        alreadyCloned.Add(other.gameObject);
        cloneCount++;

        Debug.Log($"[PlateDetector] 복제: {other.name} → {clone.name} @ {spawnPos}");
    }

    /// <summary>
    /// 뒤로가기 시 원본을 초기 위치로 리셋
    /// </summary>
    public void ResetOriginals()
    {
        foreach (var original in plateOriginals)
        {
            if (original != null && originalPositions.ContainsKey(original))
            {
                original.transform.position = originalPositions[original];
                original.transform.rotation = originalRotations[original];
            }
        }
    }

    /// <summary>
    /// 복제된 클론은 남기고 Plate 데이터만 초기화
    /// </summary>
    public void ClearPlateData()
    {
        plateOriginals.Clear();
        originalPositions.Clear();
        originalRotations.Clear();

        // _clone_ 삭제는 ShowcaseManager가 담당하므로 여기서는 무시
        alreadyCloned.Clear();
        cloneCount = 0;
    }
}
