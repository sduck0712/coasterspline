using UnityEngine;

namespace CoasterSpline
{
    public class CoasterSnapper : MonoBehaviour
    {
        private void Start()
        {
            // find the closest coaster generator
            CoasterGenerator closestGenerator = null;
            float closestDistance = float.MaxValue;
            foreach (CoasterGenerator generator in FindObjectsByType<CoasterGenerator>(FindObjectsSortMode.None))
            {
                float distance = Vector3.Distance(transform.position, generator.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestGenerator = generator;
                }
            }

            // snap to the closest generator
            if (closestGenerator != null)
            {
                (int closestLocalChainIndex, float closestLocalDistance) = BezierCurve.GetIdAndDistanceByPoint(closestGenerator, transform.position - closestGenerator.transform.position);
                if (closestLocalChainIndex != -1)
                {
                    SplineChain chain = closestGenerator.Chains[closestLocalChainIndex];
                    OrientedVector v = chain.GetPoint(closestLocalDistance);

                    Vector3 tangent = v.Direction.normalized;

                    int currentAnchorIndex = 0;
                    float currentAnchorDistance = 0;
                    for (int i = 0; i < chain.Anchors.Count - 1; i++)
                    {
                        float anchorLength = chain.GetLength(chain.Anchors[i], chain.Anchors[i + 1], true);
                        if (currentAnchorDistance + anchorLength > closestLocalDistance)
                        {
                            currentAnchorIndex = i;
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
                    Vector3 right = Vector3.Cross(tangent, targetUp).normalized;

                    Vector3 up = Vector3.Cross(right, tangent).normalized;


                    Quaternion rotationQuaternion = Quaternion.AngleAxis(v.Rotation, tangent);
                    Vector3 rotatedUp = rotationQuaternion * up;

                    Quaternion targetRotation = Quaternion.LookRotation(tangent, rotatedUp);
                    transform.position = v.Position + closestGenerator.transform.position + rotatedUp * 0.5f;
                    transform.rotation = targetRotation;
                }
            }
        }
    }
}