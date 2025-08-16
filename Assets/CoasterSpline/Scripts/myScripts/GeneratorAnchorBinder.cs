// GeneratorAnchorBinder.cs
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class GeneratorAnchorBinder : MonoBehaviour
{
    [Header("Target")]
    public Component generator;      // Boomerang의 CoasterGenerator 컴포넌트 드래그

    [Header("Which chain / anchors?")]
    public int chainIndex = 0;       // Chains 배열에서 사용할 체인 인덱스
    public int startAnchor = 0;      // 이 앵커부터
    public int anchorCount = 4;      // 몇 개를 움직일지

    [Header("Height Range (world offset on Y)")]
    public float minOffset = -3f;
    public float maxOffset =  6f;

    [Header("Rebuild")]
    public bool callRebuild = true;  // 적용 후 트랙 재생성 호출
    public bool logDebug = false;

    // 캐시
    object chainsList;       // generator.Chains
    Type chainType;          // 요소 타입
    PropertyInfo anchorsProp;// chain.Anchors 프로퍼티
    FieldInfo anchorsField;  // 또는 필드
    string[] posNames = { "Position", "position", "Pos", "pos" };
    string[] ancNames = { "Anchors", "anchors", "Points", "points", "Nodes", "nodes" };

    void Awake()
    {
        if (!generator) { Debug.LogWarning("[Binder] generator 미지정"); return; }
        TryBindReflection();
    }

    // 슬라이더 OnValueChanged에 연결
    public void SetHeight01(float t01)
    {
        if (generator == null) return;
        if (!TryBindReflection()) return;

        t01 = Mathf.Clamp01(t01);
        float offset = Mathf.Lerp(minOffset, maxOffset, t01);

        // 체인 꺼내기
        var chains = chainsList as IList;
        if (chains == null || chains.Count == 0) { Warn("Chains 비어있음"); return; }
        int ci = Mathf.Clamp(chainIndex, 0, chains.Count - 1);
        var chain = chains[ci];

        // Anchors 꺼내기 (리스트)
        IList anchors = GetAnchorsList(chain);
        if (anchors == null || anchors.Count == 0) { Warn("Anchors 비어있음"); return; }

        int start = Mathf.Clamp(startAnchor, 0, anchors.Count - 1);
        int end   = Mathf.Clamp(start + anchorCount, 0, anchors.Count);

        for (int i = start; i < end; i++)
        {
            var ancor = anchors[i];
            // 앵커 객체 안의 위치(Vector3)를 찾아 Y만 변경
            if (!TryGetSetVector3(ancor, out Vector3 p, true)) { Warn($"pos 접근 실패 i={i}"); continue; }
            p.y += offset;
            TryGetSetVector3(ancor, out _, false, p);
            if (logDebug) Debug.Log($"[Binder] chain{ci} anchor{i} -> y={p.y:0.###}");
        }

        if (callRebuild) TryRebuild();
    }

    // ───────────────────────── Reflection helpers

    bool TryBindReflection()
    {
        var genType = generator.GetType();

        // Chains (List<Chain>) 찾기
        var chainsMember = (MemberInfo)genType.GetField("Chains", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                          ?? genType.GetProperty("Chains", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (chainsMember == null) { Warn("generator에 Chains 멤버 없음"); return false; }

        chainsList = GetMemberValue(chainsMember, generator);
        if (chainsList == null) { Warn("Chains 가져오기 실패"); return false; }

        // Chain 요소 타입 추정
        var ilist = chainsList as IList;
        object firstChain = (ilist != null && ilist.Count > 0) ? ilist[0] : null;
        if (firstChain == null) return true; // 빈 상태지만 OK
        chainType = firstChain.GetType();

        // Anchors 멤버(프로퍼티/필드) 찾기 캐시
        if (anchorsProp == null && anchorsField == null)
        {
            foreach (var n in ancNames)
            {
                anchorsProp = chainType.GetProperty(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (anchorsProp != null) break;
                anchorsField = chainType.GetField(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (anchorsField != null) break;
            }
        }

        return true;
    }

    IList GetAnchorsList(object chain)
    {
        if (anchorsProp != null) return anchorsProp.GetValue(chain) as IList;
        if (anchorsField != null) return anchorsField.GetValue(chain) as IList;
        return null;
    }

    bool TryGetSetVector3(object obj, out Vector3 value, bool get, Vector3 set = default)
    {
        var t = obj.GetType();
        // 후보 필드/프로퍼티 검색
        foreach (var name in posNames)
        {
            var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(Vector3))
            {
                if (get) { value = (Vector3)f.GetValue(obj); return true; }
                else { f.SetValue(obj, set); value = default; return true; }
            }

            var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.PropertyType == typeof(Vector3) && p.CanRead && p.CanWrite)
            {
                if (get) { value = (Vector3)p.GetValue(obj); return true; }
                else { p.SetValue(obj, set); value = default; return true; }
            }
        }
        value = default; return false;
    }

    object GetMemberValue(MemberInfo m, object obj)
    {
        if (m is FieldInfo fi)    return fi.GetValue(obj);
        if (m is PropertyInfo pi) return pi.GetValue(obj);
        return null;
    }

    void TryRebuild()
    {
        var type = generator.GetType();
        string[] methodNames = { "Rebuild", "Generate", "Regenerate", "Build", "Recreate", "UpdateMesh" };
        foreach (var n in methodNames)
        {
            var mi = type.GetMethod(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (mi != null) { mi.Invoke(generator, null); return; }
        }
    }

    void Warn(string msg) { if (logDebug) Debug.LogWarning("[Binder] " + msg); }
}
