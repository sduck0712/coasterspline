using UnityEngine;

public class ShowcaseManager : MonoBehaviour
{
    [Header("플레이트 프리팹")]
    public GameObject platePrefab;

    [Header("플레이트 등장 위치")]
    public Transform plateSpawnPoint;

    // ① 메인테이블 양 끝 앵커를 드래그
    [Header("메인테이블 앵커")]
    public Transform leftAnchor;
    public Transform rightAnchor;

    private GameObject currentPlate;
    private PlateDetector plateDetector;

    /// <summary>
    /// 문 열 때(Showcase 시작) 호출
    /// </summary>
    public void OpenShowcase()
    {
        if (currentPlate != null) return;

        if (platePrefab == null || plateSpawnPoint == null)
        {
            Debug.LogError("[ShowcaseManager] Prefab 또는 SpawnPoint 미할당!");
            return;
        }

        // 1) 플레이트 생성
        currentPlate = Instantiate(
            platePrefab,
            plateSpawnPoint.position,
            plateSpawnPoint.rotation
        );

        // 2) PlateDetector 찾기
        plateDetector = currentPlate.GetComponentInChildren<PlateDetector>();
        if (plateDetector == null)
        {
            Debug.LogError("[ShowcaseManager] PlateDetector를 찾을 수 없습니다!");
            return;
        }

        // 3) 앵커 정보 넘기기
        plateDetector.leftAnchor  = leftAnchor;
        plateDetector.rightAnchor = rightAnchor;

        Debug.Log("[ShowcaseManager] OpenShowcase: Plate 생성 & Detector 세팅 완료");
    }

    /// <summary>
    /// 뒤로가기(문 닫기) 시 호출
    /// </summary>
    public void CloseShowcase()
    {
        if (plateDetector != null)
        {
            // 1) 원본 리셋
            plateDetector.ResetOriginals();
            // 2) plate 내부 데이터만 초기화 (클론 유지)
            plateDetector.ClearPlateData();
        }

        // 3) Plate 오브젝트 파괴
        if (currentPlate != null)
        {
            Destroy(currentPlate);
            currentPlate   = null;
            plateDetector  = null;
        }

        Debug.Log("[ShowcaseManager] CloseShowcase: Plate 삭제 & 원본 리셋 완료");
    }
}
