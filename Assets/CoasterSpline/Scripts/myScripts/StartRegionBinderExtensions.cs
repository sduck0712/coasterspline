// Assets/CoasterSpline/Scripts/myScripts/Compat/StartRegionBinderExtensions.cs
using UnityEngine;
using System.Reflection;

namespace CoasterSpline
{
    public static class StartRegionBinderExtensions
    {
        public static void SetAbsoluteHeight(this StartRegionBinder binder, float worldY, bool clampToGroundRange = true)
        {
            if (binder == null) return;

            // 만약 새 버전에 동일 메서드가 있으면 그걸 호출
            var t = typeof(StartRegionBinder);
            var mi = t.GetMethod("SetAbsoluteHeight", BindingFlags.Instance | BindingFlags.Public);
            if (mi != null) { mi.Invoke(binder, new object[] { worldY, clampToGroundRange }); return; }

            // 필요한 필드/메서드 반사 접근
            var host = binder as MonoBehaviour; if (!host) return;
            Transform stationRoot = GetField<Transform>(binder, "stationRoot");
            Transform trainRoot   = GetField<Transform>(binder, "trainRoot") ?? GetTrainRootFromRb(binder);
            Transform probeTf     = stationRoot ? stationRoot : (trainRoot ? trainRoot : host.transform);

            bool   useAbs        = GetField<bool>(binder, "useAbsoluteHeight");
            float  minClearance  = GetField<float>(binder, "minClearance");
            float  maxAboveGround= GetField<float>(binder, "maxAboveGround");
            float  groundY       = SampleGroundY(binder, probeTf.position);

            float targetY = worldY;
            if (clampToGroundRange && useAbs)
                targetY = Mathf.Clamp(worldY, groundY + minClearance, groundY + maxAboveGround);

            float baseY  = TryGetPrivateField(binder, "_baseStationY", probeTf.position.y);
            float offset = targetY - baseY;

            var apply = t.GetMethod("ApplyOffset", BindingFlags.Instance | BindingFlags.NonPublic);
            if (apply != null) apply.Invoke(binder, new object[] { offset });
        }

        static T GetField<T>(StartRegionBinder b, string name)
        {
            var fi = typeof(StartRegionBinder).GetField(name, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
            if (fi != null && fi.GetValue(b) is T v) return v; return default;
        }
        static T TryGetPrivateField<T>(StartRegionBinder b, string name, T fallback)
        {
            var fi = typeof(StartRegionBinder).GetField(name, BindingFlags.Instance|BindingFlags.NonPublic);
            if (fi != null && fi.GetValue(b) is T v) return v; return fallback;
        }
        static Transform GetTrainRootFromRb(StartRegionBinder b)
        {
            var rb = GetField<Rigidbody>(b, "trainRb"); return rb ? rb.transform : null;
        }
        static float SampleGroundY(StartRegionBinder b, Vector3 worldPos)
        {
            var terrain = GetField<Terrain>(b, "terrain");
            if (terrain) return terrain.SampleHeight(worldPos) + terrain.transform.position.y;
            if (Physics.Raycast(worldPos + Vector3.up*200f, Vector3.down, out var hit, 1000f)) return hit.point.y;
            return 0f;
        }
    }
}
