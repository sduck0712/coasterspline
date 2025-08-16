using System;
using UnityEngine;

namespace CoasterSpline
{
    [Serializable]
    public class SplineAncor
    {
        public Vector3 Position;
        public Vector3 Handle;
        public float rotation;
        public Vector3 Up = Vector3.up;

        public OrientedVector ToOrientedVector()
        {
            OrientedVector ov = new OrientedVector();
            ov.Position = Position;
            ov.Up = Up;
            ov.Direction = Handle.normalized;
            ov.Rotation = rotation;
            return ov;
        }
    }
}