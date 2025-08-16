// Assets/CoasterSpline/Scripts/myScripts/DataLogger.cs
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEngine;

public class DataLogger : MonoBehaviour
{
    [Header("Target")]
    public Transform trainTf;           // Cart(0) Transform
    public Rigidbody trainRb;           // 있으면 연결(가속도 정확)
    public Terrain terrain;             // 비우면 activeTerrain
    public Transform fixedZero;         // Terrain 미사용 시 기준
    public bool useTerrainAsBaseline = true;

    [Header("Physics (HUD와 동일하게)")]
    public float mass = 500f;
    public float g = 9.81f;

    [Header("Sampling")]
    public float sampleHz = 20f;        // 로그 간격(Hz) — 10~50 권장
    public bool logWhilePaused = false; // 일시정지 중에도 기록할지

    [Header("Friction (optional)")]
    public FrictionController friction; // 있으면 마찰 ON/OFF를 함께 기록

    [Header("Save")]
    public string filePrefix = "coaster_log";   // 파일명 접두사

    // 내부 상태
    float _tick;
    bool _running;
    Vector3 _prevPos;

    struct Row { public float t, h, v, Ek, Ep; public bool fr; }
    List<Row> _rows = new();

    void Start()
    {
        if (!terrain) terrain = Terrain.activeTerrain;
        if (!trainTf && trainRb) trainTf = trainRb.transform;
        _prevPos = trainTf ? trainTf.position : Vector3.zero;
    }

    // 실험 시작 버튼에 연결
    public void StartLogging()
    {
        if (!trainTf)
        {
            Debug.LogWarning("[DataLogger] trainTf가 비어 있습니다.");
            return;
        }
        _rows.Clear();
        _tick = 0f;
        _prevPos = trainTf.position;
        _running = true;
        Debug.Log("[DataLogger] Logging START");
    }

    // 실험 종료/저장 버튼에 연결
    public void StopAndSave()
    {
        _running = false;

        if (_rows.Count == 0)
        {
            Debug.Log("[DataLogger] 기록된 데이터가 없습니다.");
            return;
        }

        var dir = Application.persistentDataPath;
        var name = $"{filePrefix}_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv";
        var path = Path.Combine(dir, name);

        using (var sw = new StreamWriter(path))
        {
            var inv = CultureInfo.InvariantCulture;
            sw.WriteLine("time_s,height_m,speed_mps,Ek_J,Ep_J,friction_on");
            foreach (var r in _rows)
            {
                sw.WriteLine(string.Format(inv,
                    "{0:F3},{1:F3},{2:F3},{3:F1},{4:F1},{5}",
                    r.t, r.h, r.v, r.Ek, r.Ep, r.fr ? 1 : 0));
            }
        }

        Debug.Log($"[DataLogger] Saved CSV: {path}");
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.RevealInFinder(path);
        #endif
    }

    void Update()
    {
        if (!_running) return;
        if (!logWhilePaused && Time.timeScale <= 0f) return;

        float dt = Mathf.Max(Time.deltaTime, 1e-6f);
        _tick += dt;

        float step = 1f / Mathf.Max(1f, sampleHz);
        if (_tick < step) return;
        _tick = 0f;

        // 속도 계산: Rigidbody 우선, 없으면 위치 차분
        float v = (trainRb && !trainRb.isKinematic)
            ? trainRb.velocity.magnitude
            : (trainTf.position - _prevPos).magnitude / step;
        _prevPos = trainTf.position;

        // 지면 기준 높이
        float groundY = useTerrainAsBaseline && terrain
            ? terrain.SampleHeight(trainTf.position) + terrain.transform.position.y
            : (fixedZero ? fixedZero.position.y : 0f);
        float h = Mathf.Max(0f, trainTf.position.y - groundY);

        // 에너지( HUD 기준과 동일: Ep = m g h )
        float Ek = 0.5f * mass * v * v;
        float Ep = mass * g * h;

        // 마찰 상태 읽기
        bool frOn = GetFrictionOn();

        _rows.Add(new Row
        {
            t = Time.time,
            h = h,
            v = v,
            Ek = Ek,
            Ep = Ep,
            fr = frOn
        });
    }

    bool GetFrictionOn()
    {
        if (!friction) return false;

        // 1) private bool useFriction 리플렉션으로 시도
        var t = friction.GetType();
        var fUse = t.GetField("useFriction", BindingFlags.Instance | BindingFlags.NonPublic);
        if (fUse != null)
        {
            try { return (bool)fUse.GetValue(friction); }
            catch { /* ignore */ }
        }

        // 2) public bool enabledAtStart 있으면 참고(초기값)
        var fStart = t.GetField("enabledAtStart",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (fStart != null)
        {
            try { return (bool)fStart.GetValue(friction) && friction.enabled; }
            catch { /* ignore */ }
        }

        // 3) 폴백: 컴포넌트 활성화 상태
        return friction.enabled;
    }
}
