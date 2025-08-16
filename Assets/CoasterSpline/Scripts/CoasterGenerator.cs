using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CoasterSpline
{
    public class CoasterGenerator : MonoBehaviour
    {
        public List<SplineChain> Chains = new List<SplineChain>()
    {
        new SplineChain()
        {
            Anchors = new List<SplineAncor>()
            {
                new SplineAncor()
                {
                    Position = new Vector3(0, 2, 0),
                    Handle = new Vector3(2, 0, 0),
                    Up = Vector3.up
                },
                new SplineAncor()
                {
                    Position = new Vector3(5, 2, 0),
                    Handle = new Vector3(2, 0, 0),
                    Up = Vector3.up
                }
            }
        }
    };

        [Header("Train")]
        [SerializeField] private float _cartRadius = 0.6f;

        [Header("Supports")]
        [SerializeField] private float _minFloorHeight = 0;
        [SerializeField] private float _maxSupportDistance = 4;

        [Header("Gizmos")]
        [SerializeField] private bool _showGizmos = true;
        [SerializeField][Range(0.5f, 5)] private float _gizmoPreviewResolution = 1f;

        private void OnDrawGizmos()
        {
            if (!_showGizmos)
                return;

            foreach (var chain in Chains)
            {

                DrawBezierCurve(chain, _gizmoPreviewResolution);

                float clearanceRadius = _cartRadius;

                // Dimensions for the footer
                Vector3 footerSize = new Vector3(0.2f, 0.1f, 0.2f);

                int supportCount = Mathf.CeilToInt(chain.GetLength() / _maxSupportDistance);

                for (int i = 0; i < supportCount; i++)
                {
                    float distance = i * _maxSupportDistance;

                    OrientedVector point = chain.GetPoint(distance);

                    Vector3 groundHitPoint = point.Position;
                    groundHitPoint.y = _minFloorHeight;

                    Gizmos.color = Color.gray;
                    Gizmos.DrawCube(groundHitPoint + transform.position, footerSize);

                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(groundHitPoint + transform.position, point.Position + transform.position);
                }

            }
        }

        private void DrawBezierCurve(SplineChain chain, float resolution)
        {
            Vector3 previousPoint = chain.Anchors[0].Position;
            float length = chain.GetLength();

            for (float t = 0; t <= length; t += resolution)
            {
                OrientedVector point = chain.GetPoint(t);
                Gizmos.color = chain.Color;
                Gizmos.DrawLine(previousPoint + transform.position, point.Position + transform.position);

                Vector3 tangent = point.Direction.normalized;

                int currentAnchorIndex = 0;
                float currentAnchorDistance = 0;
                for (int i = 0; i < chain.Anchors.Count - 1; i++)
                {
                    float anchorLength = chain.GetLength(chain.Anchors[i], chain.Anchors[i + 1], true);
                    if (currentAnchorDistance + anchorLength > t)
                    {
                        currentAnchorIndex = i;
                        break;
                    }
                    currentAnchorDistance += anchorLength;
                }

                Vector3 targetUp = Vector3.up;
                // check if this or one of its neigbours has a different up vector use that instead
                if (chain.Anchors[currentAnchorIndex].Up != Vector3.up)
                {
                    targetUp = chain.Anchors[currentAnchorIndex].Up;
                }
                else if (currentAnchorIndex < chain.Anchors.Count - 1 && chain.Anchors[currentAnchorIndex + 1].Up != Vector3.up)
                {
                    targetUp = chain.Anchors[currentAnchorIndex + 1].Up;
                }

                Gizmos.color = new Color(1, 0, 1);
                Gizmos.DrawLine(point.Position + transform.position, point.Position + targetUp + transform.position);

                Vector3 right = Vector3.Cross(tangent, targetUp).normalized;

                Vector3 up = Vector3.Cross(right, tangent).normalized;


                Quaternion rotationQuaternion = Quaternion.AngleAxis(point.Rotation, tangent);
                Vector3 rotatedUp = rotationQuaternion * up;

                Gizmos.color = new Color(0, 1, 1);
                Gizmos.DrawLine(point.Position + transform.position, point.Position + rotatedUp + transform.position);

                previousPoint = point.Position;
            }

            // connect the last point to the end point
            Vector3 endpoint = chain.Anchors[chain.Anchors.Count - 1].Position;
            Gizmos.color = chain.Color;
            Gizmos.DrawLine(previousPoint + transform.position, endpoint + transform.position);
        }

        public Mesh GenerateSubMesh(SplineChain chain, Mesh segment, bool vertical)
        {
            Vector3[] vertices = segment.vertices;
            Vector3[] normals = segment.normals;
            Vector2[] uvs = segment.uv;
            int[] triangles = segment.triangles;

            float length = chain.GetLength();
            float meshLength = 1;
            int numSegments = Mathf.CeilToInt(length / meshLength);
            meshLength = length / numSegments;

            float meshMaxLength = 0;
            for (int i = 0; i < vertices.Length; i++)
            {
                meshMaxLength = Mathf.Max(meshMaxLength, vertices[i].x * meshLength);
            }
            float meshMinLength = 0;
            for (int i = 0; i < vertices.Length; i++)
            {
                meshMinLength = Mathf.Min(meshMinLength, vertices[i].x * meshLength);
            }

            bool inverted = false;
            if (Mathf.Abs(meshMinLength) > Mathf.Abs(meshMaxLength))
            {
                inverted = true;
                meshMaxLength = Mathf.Abs(meshMinLength);
            }

            List<Vector3> newVertices = new List<Vector3>();
            List<Vector3> newNormals = new List<Vector3>();
            List<Vector2> newUVs = new List<Vector2>();
            List<int> newTriangles = new List<int>();

            // Margin tolerance for t values to reuse points
            float margin = 0.0005f;

            // Cache to store calculated points for reuse
            Dictionary<float, OrientedVector> pointCache = new Dictionary<float, OrientedVector>();

            for (int seg = 0; seg < numSegments; seg++)
            {
                for (int i = 0; i < vertices.Length; i++)
                {
                    float t = Mathf.Clamp(seg * meshMaxLength + Mathf.Abs(vertices[i].x) * meshLength, 0, length);

                    OrientedVector point;
                    if (!pointCache.TryGetValue(t, out point))
                    {
                        float closestT = pointCache.Keys.FirstOrDefault(k => Mathf.Abs(k - t) <= margin);
                        if (closestT != 0)
                        {
                            point = pointCache[closestT];
                        }
                        else
                        {
                            point = chain.GetPoint(t);
                            pointCache[t] = point;
                        }


                    }

                    int currentAnchorIndex = 0;
                    float currentAnchorDistance = 0;
                    for (int i1 = 0; i1 < chain.Anchors.Count - 1; i1++)
                    {
                        float anchorLength = chain.GetLength(chain.Anchors[i1], chain.Anchors[i1 + 1], true);
                        if (currentAnchorDistance + anchorLength > t)
                        {
                            currentAnchorIndex = i1;
                            break;
                        }
                        currentAnchorDistance += anchorLength;
                    }

                    Vector3 targetUp = Vector3.up;
                    if (chain.Anchors[currentAnchorIndex].Up != Vector3.up)
                    {
                        targetUp = chain.Anchors[currentAnchorIndex].Up;
                    }
                    else if (currentAnchorIndex < chain.Anchors.Count - 1 && chain.Anchors[currentAnchorIndex + 1].Up != Vector3.up)
                    {
                        targetUp = chain.Anchors[currentAnchorIndex + 1].Up;
                    }

                    // Get the local point on the spline in world coordinates
                    Vector3 bezierPoint = point.LocalToWorld(vertices[i].z * Vector3.right + vertices[i].y * Vector3.up, targetUp);
                    newVertices.Add(bezierPoint);

                    // Calculate the TNB frame (Tangent, Normal, Binormal)
                    Vector3 tangent = point.Direction.normalized;
                    Vector3 right = Vector3.Cross(tangent, targetUp).normalized;
                    Vector3 up = Vector3.Cross(right, tangent).normalized;
                    Quaternion rotationQuaternion = Quaternion.AngleAxis(point.Rotation, tangent);
                    Vector3 normal = rotationQuaternion * up;
                    Vector3 binormal = Vector3.Cross(tangent, normal);

                    // Transform the mesh normal using the TNB frame
                    Vector3 localNormal = normals[i].z * binormal + normals[i].y * normal + normals[i].x * tangent;

                    newNormals.Add(localNormal.normalized);

                    // Add corresponding UV
                    newUVs.Add(uvs[i]);
                }

                int offset = seg * vertices.Length;
                if (inverted)
                {
                    // Reverse the triangle order to flip the normals
                    for (int i = 0; i < triangles.Length; i += 3)
                    {
                        newTriangles.Add(triangles[i + 2] + offset);
                        newTriangles.Add(triangles[i + 1] + offset);
                        newTriangles.Add(triangles[i] + offset);
                    }
                }
                else
                {
                    for (int i = 0; i < triangles.Length; i++)
                    {
                        newTriangles.Add(triangles[i] + offset);
                    }
                }
            }



            Mesh newMesh = new Mesh();
            newMesh.vertices = newVertices.ToArray();
            newMesh.normals = newNormals.ToArray();
            newMesh.uv = newUVs.ToArray();
            newMesh.triangles = newTriangles.ToArray();
            if (newVertices.Count >= 65534)
            {
                Debug.LogWarning($"Mesh has too many vertices for a single chain. Use a new chain to continue. Chain with {newVertices.Count} vertices: {(chain.MeshGO ? chain.MeshGO.name : ' ')}");
            }

            newMesh.RecalculateBounds();

            return newMesh;
        }

        public Mesh GenerateLowPolySubMesh(SplineChain chain, Mesh segment, float distanceThreshold = 0.5f)
        {
            Vector3[] vertices = segment.vertices;
            Vector3[] normals = segment.normals;
            Vector2[] uvs = segment.uv;
            int[] triangles = segment.triangles;

            float length = chain.GetLength();
            float meshLength = 1;
            int numSegments = Mathf.CeilToInt(length / meshLength);
            meshLength = length / numSegments;

            float meshMaxLength = 0;
            for (int i = 0; i < vertices.Length; i++)
            {
                meshMaxLength = Mathf.Max(meshMaxLength, vertices[i].x * meshLength);
            }
            float meshMinLength = 0;
            for (int i = 0; i < vertices.Length; i++)
            {
                meshMinLength = Mathf.Min(meshMinLength, vertices[i].x * meshLength);
            }

            bool inverted = false;
            if (Mathf.Abs(meshMinLength) > Mathf.Abs(meshMaxLength))
            {
                inverted = true;
                meshMaxLength = Mathf.Abs(meshMinLength);
            }

            List<Vector3> newVertices = new List<Vector3>();
            List<Vector3> newNormals = new List<Vector3>();
            List<Vector2> newUVs = new List<Vector2>();
            List<int> newTriangles = new List<int>();

            float margin = 0.005f;
            Dictionary<float, OrientedVector> pointCache = new Dictionary<float, OrientedVector>();

            Vector3 lastAddedVertex = Vector3.zero;

            for (int seg = 0; seg < numSegments; seg++)
            {
                for (int i = 0; i < vertices.Length; i++)
                {
                    float t = Mathf.Clamp(seg * meshMaxLength + Mathf.Abs(vertices[i].x) * meshLength, 0, length);

                    OrientedVector point;
                    if (!pointCache.TryGetValue(t, out point))
                    {
                        float closestT = pointCache.Keys.FirstOrDefault(k => Mathf.Abs(k - t) <= margin);
                        if (closestT != 0)
                        {
                            point = pointCache[closestT];
                        }
                        else
                        {
                            point = chain.GetPoint(t);
                            pointCache[t] = point;
                        }
                    }

                    Vector3 bezierPoint = point.LocalToWorld(vertices[i].z * Vector3.right + vertices[i].y * Vector3.up, Vector3.up);

                    if (newVertices.Count == 0 || Vector3.Distance(bezierPoint, lastAddedVertex) >= distanceThreshold)
                    {
                        newVertices.Add(bezierPoint);
                        lastAddedVertex = bezierPoint;

                        Vector3 tangent = point.Direction.normalized;
                        Vector3 right = Vector3.Cross(tangent, Vector3.up).normalized;
                        Vector3 up = Vector3.Cross(right, tangent).normalized;
                        Quaternion rotationQuaternion = Quaternion.AngleAxis(point.Rotation, tangent);
                        Vector3 normal = rotationQuaternion * up;
                        Vector3 binormal = Vector3.Cross(tangent, normal);
                        Vector3 localNormal = normals[i].z * binormal + normals[i].y * normal + normals[i].x * tangent;
                        newNormals.Add(localNormal.normalized);

                        newUVs.Add(uvs[i]);
                    }
                }

                int offset = seg * vertices.Length;

                if (inverted)
                {
                    for (int i = 0; i < triangles.Length; i += 3)
                    {
                        newTriangles.Add(triangles[i + 2] + offset);
                        newTriangles.Add(triangles[i + 1] + offset);
                        newTriangles.Add(triangles[i] + offset);
                    }
                }
                else
                {
                    for (int i = 0; i < triangles.Length; i++)
                    {
                        newTriangles.Add(triangles[i] + offset);
                    }
                }
            }

            Debug.Log($"Low-poly mesh: {newVertices.Count} vertices and {pointCache.Count} cached points.");

            Mesh lowPolyMesh = new Mesh();
            lowPolyMesh.vertices = newVertices.ToArray();
            lowPolyMesh.normals = newNormals.ToArray();
            lowPolyMesh.uv = newUVs.ToArray();
            lowPolyMesh.triangles = newTriangles.ToArray();

            lowPolyMesh.RecalculateBounds();

            return lowPolyMesh;
        }


        public OrientedVector[] getSupportLocations(SplineChain chain)
        {
            List<OrientedVector> supportLocations = new List<OrientedVector>();

            int supportCount = Mathf.CeilToInt(chain.GetLength() / _maxSupportDistance);

            for (int i = 0; i < supportCount; i++)
            {
                float distance = i * _maxSupportDistance;

                OrientedVector point = chain.GetPoint(distance);

                supportLocations.Add(point);
            }

            return supportLocations.ToArray();
        }

        public SplineChain GetAdjasentChain(SplineChain currentChain, float distance)
        {
            Vector3 currentPoint = currentChain.GetPoint(distance, true).Position;

            foreach (var chain in Chains)
            {
                if (chain == currentChain)
                    continue;

                if (Vector3.Distance(currentPoint, chain.GetPoint(0, true).Position) < 0.25f)
                    return chain;

                if (Vector3.Distance(currentPoint, chain.GetPoint(chain.GetLength(), true).Position) < 0.25f)
                    return chain;
            }
            return null;
        }
    }
}