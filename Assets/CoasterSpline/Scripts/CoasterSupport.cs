using UnityEngine;

namespace CoasterSpline
{
    public class CoasterSupport : MonoBehaviour
    {
        public float Rotation;
        public float Verticality;

        public Transform[] Legs;
        public float supportRadius = 0.5f;

        public MeshFilter _meshFilter;

        public bool Intersects(float minHeight, float cartRadius, SplineChain chain, Vector3 coasterOffset)
        {
            for (int i = 0; i < Legs.Length; i++)
            {
                Vector3 legPosition = Legs[i].position - Legs[i].up * (supportRadius + cartRadius) - coasterOffset;
                Vector3 legDirection = -Legs[i].up;

                for (int j = 0; j < chain.Anchors.Count - 1; j++)
                {
                    SplineAncor anchor1 = chain.Anchors[j];
                    SplineAncor anchor2 = chain.Anchors[j + 1];

                    if (BezierCurve.CheckCollision(new Ray(legPosition, legDirection), supportRadius + cartRadius, anchor1, anchor2))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void ExtendLegs(float minHeight, GameObject footerPrefab)
        {
            Mesh mesh = _meshFilter.mesh;

            if (!mesh.isReadable)
            {
                Debug.LogWarning("Mesh is not readable: " + mesh.name + ". Enabling read/write.");
            }

            mesh.MarkDynamic();

            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;

            for (int i = 0; i < Legs.Length; i++)
            {
                Vector3 legPosition = Legs[i].position;
                Vector3 legDirection = -Legs[i].up;

                for (int j = 0; j < vertices.Length; j++)
                {
                    Vector3 vertexWorldPosition = _meshFilter.transform.TransformPoint(vertices[j]);

                    if (Vector3.Distance(vertexWorldPosition, legPosition) < supportRadius)
                    {
                        Ray ray = new Ray(legPosition, legDirection);
                        Plane groundPlane = new Plane(Vector3.up, new Vector3(0, minHeight, 0));

                        if (groundPlane.Raycast(ray, out float distance))
                        {
                            Vector3 targetPosition = vertexWorldPosition + legDirection * distance;

                            vertices[j] = _meshFilter.transform.InverseTransformPoint(targetPosition);
                        }
                    }
                }

                Ray ray1 = new Ray(legPosition, legDirection);
                Plane groundPlane1 = new Plane(Vector3.up, new Vector3(0, minHeight, 0));

                if (groundPlane1.Raycast(ray1, out float distance1))
                {
                    Vector3 footerPosition = legPosition + legDirection * distance1;

                    GameObject footer = Instantiate(footerPrefab, footerPosition, Quaternion.identity);
                    footer.transform.parent = transform;
                }
            }

            mesh.vertices = vertices;
            mesh.RecalculateBounds();
            _meshFilter.mesh = mesh;
        }

    }
}