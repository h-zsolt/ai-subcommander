using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkitFree
{
    public class UnitPars : MonoBehaviour
    {
        public Firearm firearm;
        public bool canRetarget = true;
        public bool setToResupply = false;
        public bool autoResupply = true;
        public bool fireAtWill = true;
        public bool isCharging = false;
        public bool isMovable = true;
        public bool isSource = false;
        public bool isGenerating = false;
        public bool isResupplying = false;
        public bool isMovingToResupply = false;
        public bool isSupplying = false;
        public bool isShooting = false;
        public bool isReady = false;
        public bool isApproaching = false;
        public bool isAttacking = false;
        [HideInInspector] public bool isApproachable = true;
        public bool isHealing = false;
        public bool isImmune = false;
        public bool isDying = false;
        public bool isSinking = false;

        public UnitPars target = null;
        public List<UnitPars> attackers = new List<UnitPars>();
        public List<UnitPars> suppliers = new List<UnitPars>();

        public int noAttackers = 0;
        public int maxAttackers = 5;
        public int noSuppliers = 0;
        public int maxSuppliers = 3;

        [HideInInspector] public float prevR;
        [HideInInspector] public int failedR = 0;
        public int critFailedR = 100;

        public float health = 100.0f;
        public float maxHealth = 100.0f;
        public float aggressionRange = 20.0f;
        public float moveSpeed = 4.0f;

        public float alertRange = 30.0f;
        public bool alerted = false;
        public bool alertable = false;

        public float currentSupply = 10.0f;
        public float maxSupply = 300.0f;
        public float supplyDrain = 1.0f;
        public float healingSupplyDrain = 5.0f;
        public float healingSupplyCap = 120.0f;
        public float selfHealFactor = 10.0f;
        public float lackOfSupplyDamage = 1.0f;
        public float supplyTransfer = 20.0f;

        public float strength = 10.0f;
        public float defence = 10.0f;

        [HideInInspector] public int deathCalls = 0;
        public int maxDeathCalls = 5;

        [HideInInspector] public int sinkCalls = 0;
        public int maxSinkCalls = 5;

        [HideInInspector] public bool changeMaterial = true;

        public int nation = 1;

        void Start()
        {
            UnityEngine.AI.NavMeshAgent nma = GetComponent<UnityEngine.AI.NavMeshAgent>();
            
            if (nma != null)
            {
                nma.enabled = true;
            }
        }
    }
}
