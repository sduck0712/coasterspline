using UnityEngine;

namespace CoasterSpline
{
    public class CoasterAccelerator : MonoBehaviour
    {
        public float MaxSpeed = 10.0f;
        public float Force = 1.0f;
        public float BreakForce = 0.0f;

        public float radius = 1.0f;

        public float GetForce(float speed)
        {
            if (speed > 0)
            {
                if (speed > MaxSpeed)
                {
                    return 0;
                }
            }
            else
            {
                if (speed < -MaxSpeed)
                {
                    return 0;
                }
            }

            return Force;
        }

        public float GetBreakForce()
        {
            return BreakForce;
        }

        public bool InBounds(Vector3 position, float radius)
        {
            return Vector3.Distance(transform.position, position) < radius;
        }

        private void OnDrawGizmos()
        {
            // color based on speed
            Color color = Color.Lerp(Color.green, Color.red, (Force + 30) / 60);
            Gizmos.color = color;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}