using UnityEngine;

namespace CoasterSpline
{
    public struct OrientedVector
    {
        public Vector3 Position;
        public Vector3 Direction;
        public float Rotation;
        public Vector3 Up;
        public float DeltaRotation;

        public OrientedVector(Vector3 position, Vector3 direction, float rotation, Vector3 up, float deltaRotation = 0)
        {
            this.Position = position;
            this.Direction = direction;
            this.Rotation = rotation;
            this.Up = up;
            this.DeltaRotation = deltaRotation;
        }

        public Vector3 LocalToWorld(Vector3 local, Vector3 closestUp)
        {
            Vector3 tangent = Direction.normalized;

            Vector3 right = Vector3.Cross(tangent, closestUp).normalized;

            Vector3 up = Vector3.Cross(right, tangent).normalized;


            Quaternion rotationQuaternion = Quaternion.AngleAxis(Rotation, tangent);
            Vector3 rotatedUp = rotationQuaternion * up;
            Vector3 rotatedRight = rotationQuaternion * right;

            Vector3 transformedPosition = Position + local.x * rotatedRight + local.y * rotatedUp + local.z * tangent;
            return transformedPosition;
        }
    }
}