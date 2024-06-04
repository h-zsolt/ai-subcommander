using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitQueue : MonoBehaviour
{
    public List<GameObject> objectToSpawn;
    public List<KeyCode> keyToSpawn;
    public List<float> timeToSpawn;
    public List<int> unitQueue;
    public RTSToolkitFree.ManualControl manualControl;
    private void Start()
    {
        manualControl = gameObject.GetComponent<RTSToolkitFree.ManualControl>();
    }
    // Update is called once per frame
    void Update()
    {
        if (manualControl != null && manualControl.isSelected)
        {
            for (int i = 0; i < keyToSpawn.Count; i++)
            {
                if (Input.GetKeyDown(keyToSpawn[i]))
                {
                    unitQueue.Add(i);
                }
            }
        }
    }
}
