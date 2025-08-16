// AnchorHeightBinder.cs
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

public class AnchorHeightBinder : MonoBehaviour
{
    [Header("Anchors to move (drag Spline Anchor Transforms here)")]
    public List<Transform> anchors = new List<Transform>(); // Station 주변 앵커들

    [Header("Height Range (world offset)")]
    public float minOffset = -3f;   // 아래로 최대 3m
    public float maxOffset =  6f;   // 위로 최대 6m

    [Header("Track Generator (optional, for rebuild)")]
    public Component generator;     // CoasterGenerator 등(있으면 드래그)
    public bool callRebuild = true;

    // 내부
    Vector3[] _origLocalPos;  // 기준 로컬 위치(자식인 경우 안정적)
    float _currentT01 = 0.5f; // 0~1

    void Awake()
    {
        CacheOriginals();
        Apply(_currentT01);
    }

    public void SetHeight01(float t01)
    {
        _currentT01 = Mathf.Clamp01(t01);
        Apply(_currentT01);
    }

    void CacheOriginals()
    {
        _origLocalPos = new Vector3[anchors.Count];
        for (int i = 0; i < anchors.Count; i++)
        {
            if (anchors[i]) _origLocalPos[i] = anchors[i].localPosition;
        }
    }

    void Apply(float t01)
    {
        if (_origLocalPos == null || _origLocalPos.Length != anchors.Count)
            CacheOriginals();

        float offset = Mathf.Lerp(minOffset, maxOffset, t01);

        for (int i = 0; i < anchors.Count; i++)
        {
            var a = anchors[i];
            if (!a) continue;
            Vector3 p = _origLocalPos[i];
            p.y += offset;                 // 로컬 Y 이동
            a.localPosition = p;
        }

        if (callRebuild) TryRebuild();
    }

    // CoasterGenerator에서 어떤 이름을 쓰든 최대한 찾아 호출
    void TryRebuild()
    {
        if (!generator) return;

        var type = generator.GetType();
        string[] methodNames = { "Rebuild", "Generate", "Regenerate", "Build", "Recreate", "UpdateMesh" };
        foreach (var name in methodNames)
        {
            var m = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (m != null) { m.Invoke(generator, null); return; }
        }
        // 못 찾으면 그냥 무시(런타임에서 자동 갱신되면 OK)
    }
}
