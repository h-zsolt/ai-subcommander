using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Firearm : MonoBehaviour
{
        public float roundsPerMinute = 120.0f;
        public float rateOfFireCooldown = 0.0f;
        public int magazineCurrent = 5;
        public int magazineSize = 5;
        public int ammoSupply = 20;
        public int ammoMaximum = 100;
        public float reloadTime = 5.0f;
        public float moveReloadModifier = 0.5f;
        public float reloadCooldown = 0.0f;
        public bool isReadyToFire = true;
        public bool isMoving = false;
        public bool isReloading = false;
        public float strength = 50.0f;
        public float armourPen = 0.0f;
        public float range = 100.0f;

    // Update is called once per frame
    void Update()
    {
        if(isReloading && ammoSupply > 0)
        {
            if(isMoving)
            {
                reloadCooldown -= Time.deltaTime * moveReloadModifier;
            }
            else
            {
                reloadCooldown -= Time.deltaTime;
            }
            if(reloadCooldown <= 0)
            {
                isReloading = false;
                int reloadCapacity = Mathf.Min(ammoSupply, magazineSize);
                magazineCurrent = reloadCapacity;
                ammoSupply -= reloadCapacity;
            }
        }
        if(!isReadyToFire)
        {
            rateOfFireCooldown -= Time.deltaTime;
            if(rateOfFireCooldown<0)
            {
                isReadyToFire = true;
            }
        }
    }

    public void fireAtTarget(RTSToolkitFree.UnitPars target)
    {
        isReadyToFire = false;
        magazineCurrent -= 1;
        if(magazineCurrent <= 0)
        {
            isReloading = true;
            reloadCooldown = reloadTime;
        }
        rateOfFireCooldown = 60.0f / roundsPerMinute;
        Vector3 elevation = new Vector3(0, 3, 0);
        Debug.DrawRay(gameObject.transform.position + elevation, target.transform.position - gameObject.transform.position + elevation, Color.cyan, 1.0f);
    }
}
