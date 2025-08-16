using UnityEngine;
using UnityEngine.Events;

namespace CoasterSpline
{
    public class CoasterSensor : MonoBehaviour
    {

        public float radius = 0.5f;
        public UnityEvent OnTrainEnter;
        public UnityEvent OnTrainExit;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}