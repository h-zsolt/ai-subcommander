using UnityEngine;

namespace RTSToolkitFree
{
    public class SpawnPoint : MonoBehaviour
    {
        public UnitQueue unitQueue;
        public GameObject objectToSpawn;
        public float timestep = 0.01f;
        public int numberOfObjects = 10000;
        public float size = 1.0f;

        Terrain ter;
        public bool randomizeRotation = true;
        public Vector3 posOffset;

        void Awake()
        {

        }

        void Start()
        {
            ter = FindObjectOfType<Terrain>();
            unitQueue = gameObject.GetComponent<UnitQueue>();
        }

        void Update()
        {
            Spawn();
        }

        float tSpawn = 0f;
        void Spawn()
        {
            if (numberOfObjects <= 0)
            {
                if(unitQueue!= null && unitQueue.unitQueue.Count>0)
                {
                    objectToSpawn = unitQueue.objectToSpawn[unitQueue.unitQueue[0]];
                    tSpawn = unitQueue.timeToSpawn[unitQueue.unitQueue[0]];
                    timestep = tSpawn;
                    numberOfObjects = 1;
                    unitQueue.unitQueue.RemoveAt(0);
                }
                return;
            }

            tSpawn -= Time.deltaTime;
            if (tSpawn > 0f)
            {
                return;
            }

            UnitPars spawnPointUp = GetComponent<UnitPars>();
            if(spawnPointUp != null)
            {
                if(spawnPointUp.health <= 0)
                {
                    numberOfObjects = 0;
                    return;
                }
            }

            Quaternion rot = transform.rotation;
            if (randomizeRotation)
            {
                rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }

            Vector2 randPos = Random.insideUnitCircle * size;

            Vector3 pos = transform.position + new Vector3(randPos.x, 0f, randPos.y) + transform.rotation * posOffset;
            pos = TerrainVector(pos, ter);

            GameObject instance = Instantiate(objectToSpawn, pos, rot);
            UnitPars instanceUp = instance.GetComponent<UnitPars>();

            if (instanceUp != null)
            {
                if(instanceUp.nation >= Diplomacy.active.numberNations)
                {
                    Diplomacy.active.AddNation();
                }

                instanceUp.isReady = true;

                if (instanceUp.changeMaterial)
                {
                    instanceUp.GetComponent<Renderer>().material.color = Color.yellow;
                }
            }

            BattleSystem.active.allUnits.Add(instanceUp);

            numberOfObjects--;
            tSpawn = timestep;
        }

        Vector3 TerrainVector(Vector3 origin, Terrain ter1)
        {
            if (ter1 == null)
            {
                return origin;
            }

            Vector3 planeVect = new Vector3(origin.x, 0f, origin.z);
            float y = ter1.SampleHeight(planeVect);

            y = y + ter1.transform.position.y;

            Vector3 tv = new Vector3(origin.x, y, origin.z);
            return tv;
        }
    }
}
