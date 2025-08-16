// HUD_All_TMP.cs — 속도/높이 + 운동/위치에너지(LOSS 없음, Ep는 지면 기준 고정)
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HUD_All_TMP : MonoBehaviour
{
    [Header("Target (실제로 움직이는 Cart Transform 권장)")]
    public Transform trainTf;      // Cart(0) 등 실제 이동 Transform
    public Rigidbody trainRb;      // 선택(없어도 됨)

    [Header("Height baseline (화면/에너지 공통 기준)")]
    public bool useTerrainAsBaseline = true;   // On이면 Terrain 기준, Off면 fixedZero 기준
    public Terrain terrain;                    // 비우면 Terrain.activeTerrain 사용
    public Transform fixedZero;                // Terrain을 안 쓸 때 기준 y0

    [Header("UI (TMP)")]
    public TMP_Text speedText;
    public TMP_Text heightText;
    public TMP_Text kText;   // 운동에너지
    public TMP_Text pText;   // 위치에너지

    [Header("Bars (선택, Filled Image)")]
    public Image kBar;       // 운동에너지 막대
    public Image pBar;       // 위치에너지 막대
    public bool useE0AsMax = true;     // 시작 에너지(E0)로 정규화
    public float maxJoules = 100000f;  // useE0AsMax가 꺼져 있으면 이 값 사용(또는 자동 스케일)

    [Header("Speed filtering")]
    public float sampleHz = 25f;   // 속도 샘플링 빈도
    public float displayHz = 8f;   // 화면 갱신 빈도
    public float snapUp = 0.20f;   // 정지→이동 임계값
    public float snapDown = 0.08f; // 이동→정지 임계값
    public float smoothTau = 0.25f;// 속도 저주파 필터

    [Header("Energy filtering")]
    public float energySmoothTau = 0.35f; // 높이(따라서 Ep) 저주파 필터

    [Header("Physics")]
    public float mass = 500f;
    public float g = 9.81f;

    // 내부 상태
    Vector3 prevPos;
    float y0Fixed;               // Terrain 미사용 시 기준 y0
    float vFiltered;             // 필터링된 속도
    float heightFiltered;        // 필터링된 높이(지면 기준)
    float sampleTimer, displayTimer;
    bool moving;

    float startEnergy = 1f;      // E0 (막대 정규화용)
    float autoMax = 1f;          // useE0AsMax=false일 때 자동 스케일 상한

    void Start()
    {
        if (!trainTf && trainRb) trainTf = trainRb.transform;
        if (!terrain) terrain = Terrain.activeTerrain;
        y0Fixed = fixedZero ? fixedZero.position.y : 0f;
        prevPos = trainTf ? trainTf.position : Vector3.zero;

        ResetBaseline();         // 시작 기준 세팅
    }

    // 실험 시작 전에 호출(버튼 OnClick으로 연결 권장)
    public void ResetBaseline()
    {
        if (!trainTf) return;

        // 속도 초기화
        vFiltered = 0f;
        prevPos = trainTf.position;

        // 높이(지면 기준) 초기화 + 에너지 시작값 계산도 같은 기준으로!
        float h0 = GetHeightForDisplay(trainTf.position);
        heightFiltered = h0;

        float v0 = (trainRb && !trainRb.isKinematic) ? trainRb.velocity.magnitude : 0f;
        startEnergy = 0.5f * mass * v0 * v0 + mass * g * h0;
        if (startEnergy < 1f) startEnergy = 1f;

        autoMax = 1f;
    }

    void Update()
    {
        if (!trainTf) return;
        float dt = Mathf.Max(Time.deltaTime, 1e-6f);

        sampleTimer += dt;
        displayTimer += dt;

        // ── 1) 속도 샘플링/필터 ─────────────────────────────────────────────
        float samplePeriod = 1f / Mathf.Max(1f, sampleHz);
        if (sampleTimer >= samplePeriod)
        {
            float inst = (trainRb && !trainRb.isKinematic)
                ? trainRb.velocity.magnitude
                : (trainTf.position - prevPos).magnitude / sampleTimer;

            prevPos = trainTf.position;
            sampleTimer = 0f;

            // 히스테리시스(정지 스냅)
            if (!moving && inst >= snapUp) moving = true;
            else if (moving && inst <= snapDown) moving = false;
            if (!moving) inst = 0f;

            // 속도 저주파 필터
            float aV = 1f - Mathf.Exp(-dt / Mathf.Max(0.01f, smoothTau));
            vFiltered = Mathf.Lerp(vFiltered, inst, aV);
        }

        // ── 2) 높이(지면 기준) 샘플 + 필터 ─────────────────────────────────
        float hRaw = GetHeightForDisplay(trainTf.position);
        float aH = 1f - Mathf.Exp(-dt / Mathf.Max(0.01f, energySmoothTau));
        heightFiltered = Mathf.Lerp(heightFiltered, hRaw, aH);

        // ── 3) 에너지 계산 (Ep는 항상 '지면 기준 높이'로) ──────────────────
        float Ek = 0.5f * mass * vFiltered * vFiltered;
        float Ep = mass * g * heightFiltered;

        // 막대 스케일
        autoMax = Mathf.Max(autoMax, Mathf.Max(Ek, Ep));
        float maxRef = useE0AsMax ? startEnergy
                                  : Mathf.Max(1f, maxJoules > 0 ? maxJoules : autoMax);

        // ── 4) UI 갱신(낮은 빈도로) ──────────────────────────────────────
        float displayPeriod = 1f / Mathf.Max(1f, displayHz);
        if (displayTimer >= displayPeriod)
        {
            displayTimer = 0f;
            if (speedText)  speedText.text  = $"속도 {vFiltered:0.0} m/s";
            if (heightText) heightText.text = $"높이 {heightFiltered:0.0} m";
            if (kText)      kText.text      = $"운동에너지 {Ek:0} J";
            if (pText)      pText.text      = $"위치에너지 {Ep:0} J";

            if (kBar) kBar.fillAmount = Mathf.Clamp01(Ek / maxRef);
            if (pBar) pBar.fillAmount = Mathf.Clamp01(Ep / maxRef);
        }
    }

    // 현재 위치에서의 '지면 대비 높이' 계산
    float GetHeightForDisplay(Vector3 pos)
    {
        float groundY = useTerrainAsBaseline && terrain
            ? terrain.SampleHeight(pos) + terrain.transform.position.y
            : y0Fixed;
        return Mathf.Max(0f, pos.y - groundY);
    }
}
