using UnityEngine;

namespace RTSToolkitFree
{
    public class RTSCamera : MonoBehaviour
    {
        [HideInInspector] public Terrain terrain;
        public float moveSpeed = 1f;
        public float rotationSpeed = 2f;
        public float scrollSpeed = 5f;

        public float minAbsoluteHeight = 0f;
        public float minHeightAboveTerrain = 2f;
        public float maxHeightAboveTerrain = 200f;

        public bool showControls = true;

        void Awake()
        {
            terrain = FindObjectOfType<Terrain>();
        }

        void Start()
        {

        }

        void Update()
        {
            Move();
            Rotate();
            Zoom();
        }

        void Move()
        {
            Vector3 movingDirection = Vector3.zero;
            if (Input.GetKey(KeyCode.W))
            {
                movingDirection += transform.forward;
            }
            if (Input.GetKey(KeyCode.A))
            {
                movingDirection += Vector3.Cross(transform.forward, Vector3.up).normalized * transform.forward.magnitude;
            }
            if (Input.GetKey(KeyCode.S))
            {
                movingDirection += -transform.forward;
            }
            if (Input.GetKey(KeyCode.D))
            {
                movingDirection += -Vector3.Cross(transform.forward, Vector3.up).normalized * transform.forward.magnitude;
            }

            float h = terrain.SampleHeight(transform.position);
            if (h < 0f)
            {
                h = 0f;
            }

            float y = transform.position.y - h;
            Vector3 velocity = movingDirection * Time.deltaTime * y * moveSpeed;
            transform.position += velocity;
            float newHeight = terrain.SampleHeight(transform.position);

            if (newHeight < 0f)
            {
                newHeight = 0f;
            }

            transform.position = new Vector3(transform.position.x, newHeight + y, transform.position.z);
        }

        void Rotate()
        {
            if (Input.GetMouseButton(1))
            {
                float h = -rotationSpeed * Input.GetAxis("Mouse X");
                float v = rotationSpeed * Input.GetAxis("Mouse Y");

                transform.Rotate(0, h, 0, Space.World);
                transform.Rotate(v, 0, 0);

                if ((transform.rotation.eulerAngles.z >= 160) && (transform.rotation.eulerAngles.z <= 200))
                {
                    transform.Rotate(-v, 0, 0);
                }
            }
        }

        void Zoom()
        {
            float msw = Input.GetAxis("Mouse ScrollWheel");
            if (msw != 0)
            {
                float h = terrain.SampleHeight(transform.position);
                if (h < minAbsoluteHeight)
                {
                    h = minAbsoluteHeight;
                }
                float y = transform.position.y - h;

                transform.position += msw * transform.forward * scrollSpeed * y * Time.deltaTime;

                if (y < minHeightAboveTerrain)
                {
                    transform.position = new Vector3(transform.position.x, h + minHeightAboveTerrain, transform.position.z);
                }
                else if (y > maxHeightAboveTerrain)
                {
                    transform.position = new Vector3(transform.position.x, h + maxHeightAboveTerrain, transform.position.z);
                }
            }
        }

        void OnGUI()
        {
            if (showControls)
            {
                GUI.Label(new Rect(Screen.width * 0.05f, Screen.height * 0.7f, 500f, 20f), "WASD Keys - Move");
                GUI.Label(new Rect(Screen.width * 0.05f, Screen.height * 0.75f, 500f, 20f), "Mouse Right - Rotate");
                GUI.Label(new Rect(Screen.width * 0.05f, Screen.height * 0.8f, 500f, 20f), "Mouse Wheel - Zoom");
            }
        }
    }
}
