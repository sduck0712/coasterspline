using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CoasterSpline
{
    public class CoasterTrain : MonoBehaviour
    {
        [SerializeField] private CoasterGenerator generator;
        private List<CoasterCart> carts = new List<CoasterCart>();

        [SerializeField] private float _cartDistance = 1f;
        private float _totalSpeed = 0f;

        public List<CoasterSensor> CoasterSensorsInRange = new List<CoasterSensor>();
        public List<CoasterSensor> PreviousCoasterSensorsInRange = new List<CoasterSensor>();



        private void Start()
        {
            (int closestChainIndex, float closestDistance) = BezierCurve.GetIdAndDistanceByPoint(generator, transform.position - generator.transform.position);
            float localdistance = closestDistance;
            float localDirection = 1;
            int localChainIndex = closestChainIndex;

            CoasterAccelerator[] accelerators = GameObject.FindObjectsByType<CoasterAccelerator>(FindObjectsSortMode.None);
            CoasterSensor[] sensors = GameObject.FindObjectsByType<CoasterSensor>(FindObjectsSortMode.None);

            for (int i = 0; i < transform.childCount; i++)
            {
                CoasterCart cart = transform.GetChild(i).GetComponent<CoasterCart>();

                float distance = localdistance + _cartDistance * localDirection;
                if (distance > generator.Chains[localChainIndex].GetLength())
                {
                    int index = 0;
                    if (localDirection == 1)
                    {
                        index = generator.Chains[localChainIndex].Anchors.Count - 1;
                    }

                    (int closestLocalChainIndex, float closestLocalDistance) = BezierCurve.GetIdAndDistanceByPoint(generator, generator.Chains[localChainIndex].Anchors[index].Position + generator.transform.position, generator.Chains[localChainIndex], true);
                    if (closestLocalChainIndex != -1)
                    {
                        bool onEnd = closestLocalDistance >= generator.Chains[closestLocalChainIndex].GetLength();

                        if (!onEnd)
                        {

                            localdistance = distance - generator.Chains[localChainIndex].GetLength();
                            localChainIndex = closestLocalChainIndex;
                            localDirection = 1;
                        }
                        else
                        {
                            localChainIndex = closestLocalChainIndex;
                            localdistance = (distance - generator.Chains[localChainIndex].GetLength()) - generator.Chains[closestLocalChainIndex].GetLength();
                            localDirection = -1;
                        }
                    }
                    else
                    {
                        Debug.Log("No chain found for cart, removing cart");
                        carts.Remove(cart);
                    }

                }
                else { localdistance = distance; }

                cart.ChainIndex = localChainIndex;
                cart.DistanceAlongChain = localdistance;

                cart.CoasterAccelerators = accelerators;
                cart.coasterSensors = sensors;


                if (cart != null)
                {
                    carts.Add(cart);
                }
            }
        }

        private void Update()
        {
            CoasterSensorsInRange.Clear();
            foreach (CoasterCart cart in carts)
            {
                _totalSpeed -= _totalSpeed * 0.001f * Time.deltaTime;
                List<CoasterSensor> cartSensors;
                _totalSpeed += cart.GetForce(Time.deltaTime, _totalSpeed, out cartSensors) / 10;

                CoasterSensorsInRange.AddRange(cartSensors);
            }

            // remove duplicates from the sensor list
            CoasterSensorsInRange = CoasterSensorsInRange.Distinct().ToList();

            // check for changes in the sensor list, if a sensor is added or removed, call the OnTrainEnter or OnTrainExit event
            foreach (CoasterSensor sensor in CoasterSensorsInRange)
            {
                if (!PreviousCoasterSensorsInRange.Contains(sensor))
                {
                    Debug.Log("Train entered sensor");
                    sensor.OnTrainEnter.Invoke();
                }
            }

            foreach (CoasterSensor sensor in PreviousCoasterSensorsInRange)
            {
                if (!CoasterSensorsInRange.Contains(sensor))
                {
                    Debug.Log("Train exited sensor");
                    sensor.OnTrainExit.Invoke();
                }
            }

            PreviousCoasterSensorsInRange = new List<CoasterSensor>(CoasterSensorsInRange);

            //check if the trains is moving very slow, if so, stop the train
            if (Mathf.Abs(_totalSpeed) < 0.01f)
            {
                _totalSpeed = 0;
            }

            float deltaDistance = _totalSpeed * Time.deltaTime;

            foreach (CoasterCart cart in carts)
            {
                SplineChain chain = generator.Chains[cart.ChainIndex];
                cart.DistanceAlongChain += deltaDistance;

                bool outsideBounds = false;
                float leftoverDistance = 0;
                if (cart.DistanceAlongChain < 0)
                {
                    outsideBounds = true;
                    leftoverDistance = -cart.DistanceAlongChain;
                }
                else
                {
                    if (cart.DistanceAlongChain >= chain.GetLength())
                    {
                        outsideBounds = true;
                        leftoverDistance = cart.DistanceAlongChain - chain.GetLength();
                    }
                }

                if (outsideBounds)
                {
                    (int closestChainIndex, float closestDistance) = BezierCurve.GetIdAndDistanceByPoint(generator, cart.transform.position - generator.transform.position, chain, true);
                    if (closestChainIndex != -1)
                    {
                        cart.ChainIndex = closestChainIndex;

                        if (closestDistance != 0)
                        {
                            leftoverDistance = -Mathf.Abs(leftoverDistance);
                        }
                        else
                        {
                            leftoverDistance = Mathf.Abs(leftoverDistance);
                        }

                        cart.DistanceAlongChain = closestDistance + leftoverDistance;
                    }
                    else
                    {
                        Debug.Log("No chain found for cart, removing cart");
                        carts.Remove(cart);
                    }
                }
                OrientedVector point = generator.Chains[cart.ChainIndex].GetPoint(cart.DistanceAlongChain, false, true);

                Vector3 tangent = point.Direction.normalized;

                int currentAnchorIndex = 0;
                float currentAnchorDistance = 0;
                for (int i = 0; i < chain.Anchors.Count - 1; i++)
                {
                    float anchorLength = chain.GetLength(chain.Anchors[i], chain.Anchors[i + 1], true);
                    if (currentAnchorDistance + anchorLength > cart.DistanceAlongChain)
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


                Quaternion rotationQuaternion = Quaternion.AngleAxis(point.Rotation, tangent);
                Vector3 rotatedUp = rotationQuaternion * up;

                Quaternion targetRotation = Quaternion.LookRotation(tangent, rotatedUp);
                cart.transform.position = point.Position + generator.transform.position + rotatedUp * 1f;
                cart.transform.rotation = targetRotation;
            }
        }
    }
}