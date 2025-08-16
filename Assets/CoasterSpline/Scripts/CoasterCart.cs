using System.Collections.Generic;
using UnityEngine;

namespace CoasterSpline
{
    public class CoasterCart : MonoBehaviour
    {
        private Vector3 _force;
        public float DistanceAlongChain = 0f;
        public int ChainIndex = 0;

        public float radius = 0.5f;
        public CoasterAccelerator[] CoasterAccelerators;
        public CoasterSensor[] coasterSensors;

        public List<CoasterSensor> CoasterSensorsInRange = new List<CoasterSensor>();

        public float GetForce(float deltaTime, float speed, out List<CoasterSensor> coasterSensorsInRange)
        {
            foreach (var sensor in coasterSensors)
            {
                if (Vector3.Distance(sensor.transform.position, transform.position) < sensor.radius)
                {
                    if (!CoasterSensorsInRange.Contains(sensor))
                    {
                        CoasterSensorsInRange.Add(sensor);
                    }
                }
                else
                {
                    if (CoasterSensorsInRange.Contains(sensor))
                    {
                        CoasterSensorsInRange.Remove(sensor);
                    }
                }
            }

            coasterSensorsInRange = CoasterSensorsInRange;

            // standard gravtity
            _force = Vector3.down * 9.81f * deltaTime;

            // get the force from the accelerators
            foreach (var accelerator in CoasterAccelerators)
            {
                if (Vector3.Distance(accelerator.transform.position, transform.position) < accelerator.radius)
                {
                    _force += accelerator.GetForce(speed) * transform.forward * deltaTime;

                    _force -= speed * accelerator.GetBreakForce() * transform.forward * deltaTime;
                }
            }
            return Vector3.Dot(_force, transform.forward);
        }
    }
}