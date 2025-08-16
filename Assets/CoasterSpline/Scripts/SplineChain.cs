using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace CoasterSpline
{
    [Serializable]
    public class SplineChain
    {
        public List<SplineAncor> Anchors = new List<SplineAncor>();
        private Vector3 offset = Vector3.zero;
        public Vector3 Offset
        {
            get
            {
                return offset;
            }
            set
            {
                offset = value;
                // move the mesh
                if (MeshGO != null)
                {
                    MeshGO.transform.localPosition = offset;
                }
            }
        }

        public GameObject MeshGO = null;

        // Cache for lengths
        private List<float> cachedLengths = new List<float>();
        private float totalLength;
        private bool isDirty = true;  // Flag to check if the cache needs to be updated
        private Color color = Color.white;
        public Color Color
        {
            get
            {
                if (color == Color.white)
                {
                    color = UnityEngine.Random.ColorHSV();
                }
                return color;
            }
            set { color = value; }
        }

        public void InsertAnchor(int index, SplineAncor anchor)
        {
            Anchors.Insert(index, anchor);
            isDirty = true; // Mark cache as dirty when an anchor is inserted
        }

        public void RemoveAnchorAt(int index)
        {
            Anchors.RemoveAt(index);
            isDirty = true; // Mark cache as dirty when an anchor is removed
        }

        public void SetDirty()
        {
            isDirty = true;
        }

        public float GetLength()
        {
            if (isDirty) // Check if the cache needs to be updated
            {
                UpdateLengthCache();
            }
            return totalLength;
        }

        public void UpdateLengthCache()
        {
            cachedLengths.Clear();
            totalLength = 0;

            for (int i = 0; i < Anchors.Count - 1; i++)
            {
                float length = GetLength(Anchors[i], Anchors[i + 1]);
                cachedLengths.Add(length);
                totalLength += length;
            }
            //lets also recalculate all up vectors to be perpendicular to the curve
            for (int i = 0; i < Anchors.Count - 1; i++)
            {
                Vector3 tangent = Anchors[i].Handle.normalized;

                if (Mathf.Abs(Vector3.Dot(tangent, Vector3.up)) > 0.99f)
                {
                    // calculate the right veto based on the plane created by this point nad its neighbours
                    Vector3 prevPosition = Anchors[Mathf.Max(i - 1, 0)].Position;
                    Vector3 nextPosition = Anchors[Mathf.Min(i + 1, Anchors.Count - 1)].Position;

                    Vector3 prevDirection = (Anchors[i].Position - prevPosition).normalized;
                    Vector3 nextDirection = (nextPosition - Anchors[i].Position).normalized;

                    Vector3 right = Vector3.Cross(prevDirection, nextDirection).normalized;

                    // snap to the closest axis
                    if (Mathf.Abs(right.x) > 0.5f)
                    {
                        right = new Vector3(Mathf.Sign(right.x), 0, 0);
                    }
                    else if (Mathf.Abs(right.y) > 0.5f)
                    {
                        right = new Vector3(0, Mathf.Sign(right.y), 0);
                    }
                    else if (Mathf.Abs(right.z) > 0.5f)
                    {
                        right = new Vector3(0, 0, Mathf.Sign(right.z));
                    }

                    Anchors[i].Up = right;
                }
                else
                {
                    Anchors[i].Up = Vector3.up;
                }
            }

            isDirty = false; // Cache is now up to date
        }

        public float GetLength(SplineAncor start, SplineAncor end, bool useCashed = false)
        {
            if (useCashed && cachedLengths.Count == Anchors.Count - 1)
            {
                return cachedLengths[Anchors.IndexOf(start)];
            }

            int numThreads = 10; // Number of threads
            float length = 0;
            Vector3 previousPoint = start.Position;

            float precision = 0.00001f;  // Fine-tune this value for better accuracy
            if (start.Up != Vector3.up || end.Up != Vector3.up)
            {
                precision = 0.00001f;
            }

            // Divide the range [0, 1] into numThreads parts
            float step = 1f / numThreads;

            // Use a list of tasks to perform the length computation in parallel
            Task<float>[] tasks = new Task<float>[numThreads];

            for (int i = 0; i < numThreads; i++)
            {
                float tStart = i * step;
                float tEnd = (i + 1) * step;

                // Assign each task a portion of the curve to calculate its length
                tasks[i] = Task<float>.Factory.StartNew(() =>
                {
                    float segmentLength = 0;
                    Vector3 segPreviousPoint = BezierCurve.GetOrientedPoint(start, end, tStart).Position;

                    for (float t = tStart + precision; t <= tEnd; t += precision)
                    {
                        OrientedVector point = BezierCurve.GetOrientedPoint(start, end, t);
                        segmentLength += Vector3.Distance(segPreviousPoint, point.Position);
                        segPreviousPoint = point.Position;
                    }

                    return segmentLength;
                });
            }

            // Wait for all tasks to complete and sum the results
            Task.WaitAll(tasks);

            for (int i = 0; i < numThreads; i++)
            {
                length += tasks[i].Result;
            }

            return length;
        }


        public OrientedVector GetPoint(float distance, bool useSnappedPoints = false, bool precise = false)
        {
            int curveIndex = 0;
            float curveDistance = 0;
            float totalLength = GetLength();

            distance = Mathf.Clamp(distance, 0, totalLength);

            if (distance == totalLength && useSnappedPoints)
            {
                OrientedVector point1 = Anchors[Anchors.Count - 1].ToOrientedVector();
                point1.Position += offset;
                return point1;
            }

            if (distance == 0 && useSnappedPoints)
            {
                OrientedVector point2 = Anchors[0].ToOrientedVector();
                point2.Position += offset;
                return point2;
            }

            if (cachedLengths.Count != Anchors.Count - 1)
            {
                UpdateLengthCache();
            }

            for (int i = 0; i < cachedLengths.Count; i++)
            {
                float length = cachedLengths[i];
                if (curveDistance + length > distance)
                {
                    curveIndex = i;
                    break;
                }

                if (i == cachedLengths.Count - 1)
                {
                    curveIndex = i;
                    break;
                }

                curveDistance += length;
            }

            float distanceOnCurve = distance - curveDistance;
            float curveLength = cachedLengths[curveIndex];

            float t = FindTForDistance(Anchors[curveIndex], Anchors[curveIndex + 1], distanceOnCurve, curveLength, precise);

            OrientedVector point = BezierCurve.GetOrientedPoint(Anchors[curveIndex], Anchors[curveIndex + 1], t);
            point.Position += offset;

            return point;
        }

        private float FindTForDistance(SplineAncor start, SplineAncor end, float distanceOnCurve, float curveLength, bool precise = false)
        {
            float t = 0;
            float stepSize = 0.005f;
            if (precise)
            {
                stepSize = 0.0004f;
            }

            float accumulatedLength = 0;
            Vector3 previousPoint = start.Position;

            for (t = 0; t <= 1; t += stepSize)
            {
                OrientedVector point = BezierCurve.GetOrientedPoint(start, end, t);
                float segmentLength = Vector3.Distance(previousPoint, point.Position);
                accumulatedLength += segmentLength;

                if (accumulatedLength >= distanceOnCurve)
                {
                    return t;
                }

                previousPoint = point.Position;
            }

            return 1f;
        }
    }
}