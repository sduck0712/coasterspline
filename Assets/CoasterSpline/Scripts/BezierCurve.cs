using UnityEngine;

namespace CoasterSpline
{
    public static class BezierCurve
    {
        public static Vector3 GetPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;
            return oneMinusT * oneMinusT * oneMinusT * p0 +
                   3f * oneMinusT * oneMinusT * t * p1 +
                   3f * oneMinusT * t * t * p2 +
                   t * t * t * p3;
        }

        public static Vector3 getPoint(Vector3 ancor1, Vector3 ancorHandle1, Vector3 ancor2, Vector3 ancorHandle2, float t)
        {
            return GetPoint(ancor1, ancor1 + ancorHandle1, ancor2 + ancorHandle2, ancor2, t);
        }

        public static OrientedVector GetOrientedPoint(SplineAncor ancor1, SplineAncor ancor2, float t)
        {
            if (ancor1 == null)
            {
                ancor1 = new SplineAncor()
                {
                    Position = Vector3.zero,
                    Handle = Vector3.zero,
                    rotation = 0,
                    Up = Vector3.up
                };
            }

            if (ancor2 == null)
            {
                ancor2 = new SplineAncor()
                {
                    Position = Vector3.zero,
                    Handle = Vector3.zero,
                    rotation = 0,
                    Up = Vector3.up
                };
            }

            Vector3 point = GetPoint(ancor1.Position, ancor1.Position + ancor1.Handle, ancor2.Position + -ancor2.Handle, ancor2.Position, t);
            Vector3 direction = GetFirstDerivative(ancor1.Position, ancor1.Position + ancor1.Handle, ancor2.Position + -ancor2.Handle, ancor2.Position, t);
            float rotation = GetRotation(ancor1.rotation, ancor2.rotation, t);

            Vector3 up = Vector3.up;

            float deltaRotation = 0;
            if (ancor1.Up != Vector3.up)
            {
                up = ancor1.Up;
                if (ancor1.Handle.y > 0)
                {
                    deltaRotation = Mathf.Lerp(90, 0, 1 - t);
                }
                else
                {
                    deltaRotation = Mathf.Lerp(0, -90, t);
                }
            }
            else if (ancor2.Up != Vector3.up)
            {
                up = ancor2.Up;
                if (ancor2.Handle.y > 0)
                {
                    deltaRotation = Mathf.Lerp(-90, 0, t);
                }
                else
                {
                    deltaRotation = Mathf.Lerp(0, 90, 1 - t);
                }
            }

            rotation += deltaRotation;


            return new OrientedVector(point, direction, rotation, up, deltaRotation);
        }

        public static Vector3 GetFirstDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;
            return 3f * oneMinusT * oneMinusT * (p1 - p0) +
                   6f * oneMinusT * t * (p2 - p1) +
                   3f * t * t * (p3 - p2);
        }

        public static float GetRotation(float rotation1, float rotation2, float t)
        {
            return GetSmoothRotation(rotation1, rotation1, rotation2, rotation2, t);
        }

        public static float GetSmoothRotation(float rotation0, float rotation1, float rotation2, float rotation3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * (
                (2f * rotation1) +
                (-rotation0 + rotation2) * t +
                (2f * rotation0 - 5f * rotation1 + 4f * rotation2 - rotation3) * t2 +
                (-rotation0 + 3f * rotation1 - 3f * rotation2 + rotation3) * t3
            );
        }

        public static bool CheckCollision(Ray ray, float curveRadius, SplineAncor ancor1, SplineAncor ancor2)
        {
            Vector3 closestPoint = GetClosestPointOnCurve(ray, ancor1, ancor2);

            Vector3 toClosestPoint = closestPoint - ray.origin;

            float projection = Vector3.Dot(ray.direction, toClosestPoint);

            if (projection < 0)
            {
                return false;
            }

            Vector3 perpendicular = toClosestPoint - ray.direction * projection;
            float distanceToCurve = perpendicular.magnitude;

            return distanceToCurve <= curveRadius;
        }

        public static Vector3 GetClosestPointOnCurve(Ray ray, SplineAncor ancor1, SplineAncor ancor2)
        {
            Vector3 closestPoint = Vector3.zero;
            float closestDistance = float.MaxValue;

            for (float t = 0; t <= 1; t += 0.001f)
            {
                Vector3 point = GetPoint(ancor1.Position, ancor1.Position + ancor1.Handle, ancor2.Position + -ancor2.Handle, ancor2.Position, t);

                Vector3 toPoint = point - ray.origin;
                float projection = Vector3.Dot(ray.direction, toPoint);

                if (projection < 0) continue;

                Vector3 perpendicular = toPoint - ray.direction * projection;
                float distance = perpendicular.magnitude;

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPoint = point;
                }
            }
            return closestPoint;
        }

        public static (int, float) GetIdAndDistanceByPoint(CoasterGenerator generator, Vector3 position, SplineChain ignoreChain = null, bool onlyCheckends = false)
        {
            int chainIndex = -1;
            float distance = 0;

            float closestDistance = float.MaxValue;



            for (int i = 0; i < generator.Chains.Count; i++)
            {
                if (generator.Chains[i] == ignoreChain)
                {
                    continue;
                }

                if (onlyCheckends)
                {
                    OrientedVector point = generator.Chains[i].Anchors[0].ToOrientedVector();
                    float currentDistance = Vector3.Distance(point.Position, position);

                    if (currentDistance < closestDistance)
                    {
                        closestDistance = currentDistance;
                        chainIndex = i;
                        distance = 0;
                    }

                    point = generator.Chains[i].Anchors[generator.Chains[i].Anchors.Count - 1].ToOrientedVector();
                    currentDistance = Vector3.Distance(point.Position, position);

                    if (currentDistance < closestDistance)
                    {
                        closestDistance = currentDistance;
                        chainIndex = i;
                        distance = generator.Chains[i].GetLength();
                    }
                }
                else
                {
                    float l = generator.Chains[i].GetLength();
                    for (float j = 0; j < l; j += 0.1f)
                    {

                        OrientedVector point = generator.Chains[i].GetPoint(j);
                        float currentDistance = Vector3.Distance(point.Position, position);

                        if (currentDistance < closestDistance)
                        {
                            closestDistance = currentDistance;
                            chainIndex = i;
                            distance = j;
                        }
                    }
                }
            }

            return (chainIndex, distance);
        }


        public static float TToDistance(float t, float bezierLength)
        {
            return t * bezierLength;
        }

        public static float DistanceToT(float distance, float bezierLength)
        {
            return distance / bezierLength;
        }
    }
}