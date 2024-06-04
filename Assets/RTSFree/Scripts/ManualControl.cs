using UnityEngine;

namespace RTSToolkitFree
{
    public class ManualControl : MonoBehaviour
    {
        public bool isSelected = false;
        public bool prepareMoving = false;
        public bool isMoving = false;

        [HideInInspector] public float prevDist = 0.0f;
        [HideInInspector] public int failedDist = 0;
        public int critFailedDist = 10;

        public Vector3 manualDestination;

        void Start()
        {
            
        }
    }
}
