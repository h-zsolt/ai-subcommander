using UnityEngine;
using System.Collections.Generic;

// BSystem is core component for simulating RTS battles
// It has 6 phases for attack and gets all different game objects parameters inside.
// Attack phases are: Search, Approach target, Attack, Self-Heal, Die, Rot (Sink to ground).
// All 6 phases are running all the time and checking if object is matching criteria, then performing actions
// Movements between different phases are also described

namespace RTSToolkitFree
{
    public class BattleSystem : MonoBehaviour
    {
        public static BattleSystem active;

        public List<AI_Controller> listOfAI;

        public bool showStatistics;

        public List<UnitPars> allUnits = new List<UnitPars>();

        [HideInInspector] public List<UnitPars> sinks = new List<UnitPars>();

        public List<List<UnitPars>> targets = new List<List<UnitPars>>();
        List<List<UnitPars>> friendlyTargets = new List<List<UnitPars>>();
        List<List<UnitPars>> sourceTargets = new List<List<UnitPars>>();
        List<float> targetRefreshTimes = new List<float>();
        List<KDTree> targetKD = new List<KDTree>();
        List<KDTree> friendlyKD = new List<KDTree>();
        List<KDTree> sourceKD = new List<KDTree>();

        public int randomSeed = 0;

        public float shootUpdateFraction = 0.1f;
        public float searchUpdateFraction = 0.1f;
        public float retargetUpdateFraction = 0.1f;
        public float approachUpdateFraction = 0.1f;
        public float attackUpdateFraction = 0.1f;
        public float selfHealUpdateFraction = 1f;
        public float deathUpdateFraction = 0.05f;
        public float sinkUpdateFraction = 1f;
        public float chargeUpdateFraction = 0.1f;
        public float supplyUpdateFraction = 1f;
        public float resupplyUpdateFraction = 0.1f;
        public float cleanListsUpdateFraction = 0.05f;

        public KDTree GetSourceKD(int targetNation)
        {
            if(targetNation<sourceKD.Count)
            {
                return sourceKD[targetNation];
            }
            return null;
        }

        public List<UnitPars> GetSourceList(int targetNation)
        {
            if (targetNation < sourceTargets.Count)
            {
                return sourceTargets[targetNation];
            }
            return null;
        }

        public List<UnitPars> GetNationTargets(int nation)
        {
            if (nation < targets.Count)
            {
                return targets[nation];
            }
            return null;
        }

        void Awake()
        {
            active = this;
            Random.InitState(randomSeed);
        }

        void Start()
        {
            UnityEngine.AI.NavMesh.pathfindingIterationsPerFrame = 10000;
        }

        void Update()
        {
            if(showStatistics)
            {
                BattleSystemStatistics.UpdateWithStatistics(this, Time.deltaTime);
            }
            else
            {
                UpdateWithoutStatistics();
            }
        }

        void UpdateWithoutStatistics()
        {
            float deltaTime = Time.deltaTime;
            RefreshTargetList(deltaTime);
            SearchPhase();
            RetargetPhase();
            ApproachPhase();
            ShootPhase();
            ChargePhase();
            AttackPhase();
            SupplyPhase(deltaTime);
            ResupplyPhase();
            SelfHealingPhase(deltaTime);
            DeathPhase();
            SinkPhase(deltaTime);
            ManualMover();
            AddToAI();
            cleanUPLists();
        }

        int iCleanPhase = 0;
        float fCleanPhase = 0.0f;
        private void cleanUPLists()
        {
            fCleanPhase += allUnits.Count * cleanListsUpdateFraction;

            int nToLoop = (int)fCleanPhase;
            fCleanPhase -= nToLoop;
            UnitPars up;
            UnitPars cleaned;
            for (int i = 0; i < nToLoop; i++)
            {
                iCleanPhase++;

                if (iCleanPhase >= allUnits.Count)
                {
                    iCleanPhase = 0;
                }
                up = allUnits[iCleanPhase];
                for (int j = 0; j < up.noAttackers; j++)
                {
                    cleaned = up.attackers[j];
                    if(cleaned.target!=up)
                    {
                        up.attackers.Remove(cleaned);
                        up.noAttackers = up.attackers.Count;
                        j--;
                    }
                }
                for (int j = 0; j < up.noSuppliers; j++)
                {
                    cleaned = up.suppliers[j];
                    if (cleaned.target != up)
                    {
                        up.suppliers.Remove(cleaned);
                        up.noSuppliers = up.suppliers.Count;
                        j--;
                    }
                }
            }
        }

        private void AddToAI()
        {
            for(int i = 0; i < listOfAI.Count;i++)
            {
                if(Input.GetKeyDown(listOfAI[i].addUnitKey))
                {
                    for(int j = 0; j < allUnits.Count;j++)
                    {
                        if(allUnits[j].GetComponent<ManualControl>().isSelected && allUnits[j].nation == listOfAI[i].nationID)
                        {
                            for(int k = 0; k < listOfAI.Count; k++)
                            {
                                if(i!=k)
                                {
                                    listOfAI[k].RemoveUnitFromAI(allUnits[j]);
                                }
                            }
                            listOfAI[i].addUnitToAICommand(allUnits[j]);
                        }
                    }
                }
                if(Input.GetKeyDown(listOfAI[i].removeUnitKey))
                {
                    for (int j = 0; j < allUnits.Count; j++)
                    {
                        if (allUnits[j].GetComponent<ManualControl>().isSelected && allUnits[j].nation == listOfAI[i].nationID)
                        {
                            listOfAI[i].RemoveUnitFromAI(allUnits[j]);
                        }
                    }
                }
            }
        }

        int iResupplyPhase = 0;
        float fResupplyPhase = 0f;

        private void ResupplyPhase()
        {
            fResupplyPhase += allUnits.Count * resupplyUpdateFraction;

            int nToLoop = (int)fResupplyPhase;
            fResupplyPhase -= nToLoop;

            for (int i = 0; i < nToLoop; i++)
            {
                iResupplyPhase++;

                if (iResupplyPhase >= allUnits.Count)
                {
                    iResupplyPhase = 0;
                }

                UnitPars resUp = allUnits[iResupplyPhase];

                if (resUp.isMovingToResupply && resUp.target != null)
                {

                    UnitPars targ = resUp.target;

                    UnityEngine.AI.NavMeshAgent apprNav = resUp.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    UnityEngine.AI.NavMeshAgent targNav = targ.GetComponent<UnityEngine.AI.NavMeshAgent>();

                    if (targ.isApproachable == true)
                    {
                        // stopping condition for NavMesh

                        apprNav.stoppingDistance = apprNav.radius / (resUp.transform.localScale.x) + targNav.radius / (targ.transform.localScale.x);

                        // distance between approacher and target

                        float rTarget = (resUp.transform.position - targ.transform.position).magnitude;
                        float stoppDistance = (2f + resUp.transform.localScale.x * targ.transform.localScale.x * apprNav.stoppingDistance);

                        // counting increased distances (failure to approach) between attacker and target;
                        // if counter failedR becomes bigger than critFailedR, preparing for new target search.

                        if (resUp.prevR < rTarget)
                        {
                            resUp.failedR = resUp.failedR + 1;
                            if (resUp.failedR > resUp.critFailedR)
                            {
                                resUp.isMovingToResupply = false;
                                resUp.isResupplying = true;
                                resUp.failedR = 0;

                                if (resUp.target != null)
                                {
                                    Debug.Log("Failed to approach supply source.");
                                    targ.suppliers.Remove(resUp);
                                    targ.noSuppliers = targ.suppliers.Count;
                                    resUp.target = null;
                                    if (resUp.changeMaterial)
                                    {
                                        resUp.GetComponent<Renderer>().material.color = Color.yellow;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // if approachers already close to their targets
                            if (rTarget < stoppDistance)
                            {
                                apprNav.SetDestination(resUp.transform.position);

                                resUp.isMovingToResupply = false;
                                resUp.isReady = true;
                                float exchangedSupply = Mathf.Min(targ.currentSupply, resUp.maxSupply - resUp.currentSupply);
                                resUp.currentSupply += exchangedSupply;
                                targ.currentSupply -= exchangedSupply;
                                targ.suppliers.Remove(resUp);
                                targ.noSuppliers = targ.suppliers.Count;
                                Debug.Log("Successful Resupply");

                                if (resUp.changeMaterial)
                                {
                                    resUp.GetComponent<Renderer>().material.color = Color.yellow;
                                }
                            }
                            else
                            {
                                if (resUp.changeMaterial)
                                {
                                    resUp.GetComponent<Renderer>().material.color = Color.green;
                                }

                                // starting to move
                                if (resUp.isMovable)
                                {
                                    Vector3 destination = apprNav.destination;
                                    if ((destination - targ.transform.position).sqrMagnitude > 1f)
                                    {
                                        apprNav.SetDestination(targ.transform.position);
                                        apprNav.speed = resUp.moveSpeed;
                                    }
                                }
                            }
                        }

                        // saving previous R
                        resUp.prevR = rTarget;
                    }
                    // condition for non approachable targets	
                    else
                    {
                        Debug.Log("Unreachable Supply");
                        targ.suppliers.Remove(resUp);
                        targ.noSuppliers = targ.suppliers.Count;
                        resUp.target = null;
                        apprNav.SetDestination(resUp.transform.position);

                        resUp.isMovingToResupply = false;
                        resUp.isReady = true;

                        if (resUp.changeMaterial)
                        {
                            resUp.GetComponent<Renderer>().material.color = Color.yellow;
                        }
                    }
                }
            }
        }
        

        int iSupplyPhase = 0;
        float fSupplyPhase = 0f;

        private void SupplyPhase(float deltaTime)
        {
            fSupplyPhase += allUnits.Count * supplyUpdateFraction;

            int nToLoop = (int)fSupplyPhase;
            fSupplyPhase -= nToLoop;

            for (int i = 0; i < nToLoop; i++)
            {
                iSupplyPhase++;

                if (iSupplyPhase >= allUnits.Count)
                {
                    iSupplyPhase = 0;
                }

                UnitPars supplyUp = allUnits[iSupplyPhase];

                if (supplyUp.isSupplying && supplyUp.target != null)
                {
                    UnitPars targ = supplyUp.target;

                    UnityEngine.AI.NavMeshAgent suppNav = supplyUp.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    UnityEngine.AI.NavMeshAgent targNav = targ.GetComponent<UnityEngine.AI.NavMeshAgent>();

                    if (targ.isApproachable)
                    {
                        suppNav.stoppingDistance = suppNav.radius / (suppNav.transform.localScale.x) + targNav.radius / (targ.transform.localScale.x);

                        float rTarget = (suppNav.transform.position - targ.transform.position).magnitude;
                        float stoppDistance = (2f + suppNav.transform.localScale.x * targ.transform.localScale.x * suppNav.stoppingDistance);

                        if (supplyUp.prevR < rTarget)
                        {
                            supplyUp.failedR = supplyUp.failedR + 1;
                            if (supplyUp.failedR > supplyUp.critFailedR)
                            {
                                supplyUp.isSupplying = false;
                                supplyUp.isReady = true;
                                supplyUp.failedR = 0;

                                if (supplyUp.target != null)
                                {
                                    Debug.Log("Failed to reach supply target");
                                    supplyUp.target.suppliers.Remove(supplyUp);
                                    supplyUp.target.noSuppliers = supplyUp.target.suppliers.Count;
                                    supplyUp.target = null;
                                }

                                if (supplyUp.changeMaterial)
                                {
                                    supplyUp.GetComponent<Renderer>().material.color = Color.yellow;
                                }
                            }
                        }
                        else
                        {
                            // if approachers already close to their targets
                            if (rTarget < stoppDistance)
                            {
                                suppNav.SetDestination(supplyUp.transform.position);

                                float supplyExchange = supplyUp.supplyTransfer * deltaTime / supplyUpdateFraction;

                                if (supplyUp.currentSupply - supplyExchange > supplyUp.healingSupplyCap)
                                {
                                    targ.currentSupply += supplyExchange;
                                    if (targ.currentSupply > targ.maxSupply)
                                    {
                                        supplyExchange -= targ.currentSupply - targ.maxSupply;
                                        targ.currentSupply = targ.maxSupply;
                                        supplyUp.isSupplying = false;
                                        supplyUp.isReady = true;

                                        Debug.Log("Exchanged supplies with another unit");
                                        targ.suppliers.Remove(supplyUp);
                                        targ.noSuppliers = targ.suppliers.Count;
                                        supplyUp.target = null;
                                    }
                                    supplyUp.currentSupply -= supplyExchange;
                                }
                                else
                                {
                                    Debug.Log("Supplier needs to resupply");
                                    supplyUp.isSupplying = false;
                                    supplyUp.isResupplying = true;
                                    supplyUp.isReady = true;
                                    targ.suppliers.Remove(supplyUp);
                                    targ.noSuppliers = targ.suppliers.Count;
                                    supplyUp.target = null;
                                    if (supplyUp.changeMaterial)
                                    {
                                        supplyUp.GetComponent<Renderer>().material.color = Color.cyan;
                                    }
                                }
                            }
                            else
                            {
                                if (supplyUp.changeMaterial)
                                {
                                    supplyUp.GetComponent<Renderer>().material.color = Color.green;
                                }

                                // starting to move
                                if (supplyUp.isMovable)
                                {
                                    Vector3 destination = suppNav.destination;
                                    if ((destination - targ.transform.position).sqrMagnitude > 1f)
                                    {
                                        suppNav.SetDestination(targ.transform.position);
                                        suppNav.speed = supplyUp.moveSpeed;
                                    }
                                }
                            }
                        }

                        // saving previous R
                        supplyUp.prevR = rTarget;
                    }
                    // condition for non approachable targets	
                    else
                    {
                        supplyUp.target = null;
                        suppNav.SetDestination(supplyUp.transform.position);

                        supplyUp.isSupplying = false;
                        supplyUp.isReady = true;

                        if (supplyUp.changeMaterial)
                        {
                            supplyUp.GetComponent<Renderer>().material.color = Color.yellow;
                        }
                    }
                }
            }
        }
        

        int iChargePhase = 0;
        float fChargePhase = 0f;

        private void ChargePhase()
        {
            fChargePhase += allUnits.Count * chargeUpdateFraction;

            int nToLoop = (int)fChargePhase;
            fChargePhase -= nToLoop;

            // checking through allUnits list which units are set to approach (isApproaching)
            for (int i = 0; i < nToLoop; i++)
            {
                iChargePhase++;

                if (iChargePhase >= allUnits.Count)
                {
                    iChargePhase = 0;
                }

                UnitPars apprPars = allUnits[iChargePhase];

                if (apprPars.isCharging && apprPars.target != null)
                {

                    UnitPars targ = apprPars.target;

                    UnityEngine.AI.NavMeshAgent apprNav = apprPars.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    UnityEngine.AI.NavMeshAgent targNav = targ.GetComponent<UnityEngine.AI.NavMeshAgent>();

                    if (targ.isApproachable == true)
                    {
                        // stopping condition for NavMesh

                        apprNav.stoppingDistance = apprNav.radius / (apprPars.transform.localScale.x) + targNav.radius / (targ.transform.localScale.x);

                        // distance between approacher and target

                        float rTarget = (apprPars.transform.position - targ.transform.position).magnitude;
                        float stoppDistance = (2f + apprPars.transform.localScale.x * targ.transform.localScale.x * apprNav.stoppingDistance);

                        // counting increased distances (failure to approach) between attacker and target;
                        // if counter failedR becomes bigger than critFailedR, preparing for new target search.

                        if (apprPars.prevR <= rTarget)
                        {
                            apprPars.failedR = apprPars.failedR + 1;
                            if (apprPars.failedR > apprPars.critFailedR)
                            {
                                apprPars.isCharging = false;
                                apprPars.isReady = true;
                                apprPars.failedR = 0;

                                if (apprPars.target != null)
                                {
                                    apprPars.target.attackers.Remove(apprPars);
                                    apprPars.target.noAttackers = apprPars.target.attackers.Count;
                                    apprPars.target = null;
                                }

                                if (apprPars.changeMaterial)
                                {
                                    apprPars.GetComponent<Renderer>().material.color = Color.yellow;
                                }
                            }
                        }
                        else
                        {
                            // if approachers already close to their targets
                            if (rTarget < stoppDistance)
                            {
                                apprNav.SetDestination(apprPars.transform.position);

                                // pre-setting for attacking
                                apprPars.isCharging = false;
                                apprPars.isAttacking = true;

                                if (apprPars.changeMaterial)
                                {
                                    apprPars.GetComponent<Renderer>().material.color = Color.red;
                                }
                            }
                            else
                            {
                                if (apprPars.changeMaterial)
                                {
                                    apprPars.GetComponent<Renderer>().material.color = Color.green;
                                }

                                // starting to move
                                if (apprPars.isMovable)
                                {
                                    Vector3 destination = apprNav.destination;
                                    if ((destination - targ.transform.position).sqrMagnitude > 1f)
                                    {
                                        apprNav.SetDestination(targ.transform.position);
                                        apprNav.speed = apprPars.moveSpeed;
                                    }
                                }
                            }
                        }

                        // saving previous R
                        apprPars.prevR = rTarget;
                    }
                    // condition for non approachable targets	
                    else
                    {
                        apprPars.target = null;
                        apprNav.SetDestination(apprPars.transform.position);

                        apprPars.isApproaching = false;
                        apprPars.isReady = true;

                        if (apprPars.changeMaterial)
                        {
                            apprPars.GetComponent<Renderer>().material.color = Color.yellow;
                        }
                    }
                }
            }
        }

        int iShootPhase = 0;
        float fShootPhase = 0.0f;

        private void ShootPhase()
        {
            fShootPhase += allUnits.Count * shootUpdateFraction;

            int nToLoop = (int)fShootPhase;
            fShootPhase -= nToLoop;

            // checking through allUnits list which units are set to approach (isAttacking)
            for (int i = 0; i < nToLoop; i++)
            {
                iShootPhase++;

                if (iShootPhase >= allUnits.Count)
                {
                    iShootPhase = 0;
                }

                UnitPars attPars = allUnits[iShootPhase];

                if (attPars.isShooting && attPars.target != null && attPars.firearm.isReadyToFire && !attPars.firearm.isReloading)
                {
                    UnitPars targPars = attPars.target;

                    UnityEngine.AI.NavMeshAgent attNav = attPars.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    UnityEngine.AI.NavMeshAgent targNav = targPars.GetComponent<UnityEngine.AI.NavMeshAgent>();

                    attNav.stoppingDistance = attNav.radius / (attPars.transform.localScale.x) + targNav.radius / (targPars.transform.localScale.x);

                    // distance between attacker and target

                    float rTarget = (attPars.transform.position - targPars.transform.position).magnitude;

                    // if target moves away, resetting back to approach target phase

                    if (rTarget > attPars.firearm.range)
                    {
                        Debug.Log("Out of range log");
                        attPars.isApproaching = true;
                        attPars.isShooting = false;
                    }
                    // if targets becomes immune, attacker is reset to start searching for new target 	
                    else if (targPars.isImmune == true)
                    {
                        attPars.isShooting = false;
                        attPars.isReady = true;

                        targPars.attackers.Remove(attPars);
                        targPars.noAttackers = targPars.attackers.Count;

                        if (attPars.changeMaterial)
                        {
                            attPars.GetComponent<Renderer>().material.color = Color.yellow;
                        }
                    }
                    // attacker starts attacking their target	
                    else
                    {
                        if (attPars.changeMaterial)
                        {
                            attPars.GetComponent<Renderer>().material.color = Color.black;
                        }
                        //Debug.Log(iShootPhase.ToString()+" shot at someone");
                        if(targPars.alertable && !targPars.alerted)
                        {
                            alertEnemies(targPars, attPars.nation);
                        }
                        attPars.firearm.fireAtTarget(targPars);
                        float strength = attPars.firearm.strength;
                        float defence = Mathf.Max(targPars.defence - attPars.firearm.armourPen, 0.0f);

                        // if attack passes target through target defence, cause damage to target
                        if (Random.value > (strength / (strength + defence)))
                        {
                            var damageDealt = Mathf.Max(strength * Random.value, 0.5f * strength);
                            targPars.health -= damageDealt;
                            //Debug.Log("Hit, Damage " + damageDealt.ToString());
                        }
                    }
                }
            }
        }

        public void alertEnemies(UnitPars up, int nationTrigger)
        {
            for (int i = 0; i < targets[nationTrigger].Count; i++)
            {
                if (targets[nationTrigger][i].nation == up.nation && targets[nationTrigger][i].alertable && !targets[nationTrigger][i].alerted)
                {
                    float aDistance = (up.transform.position - targets[nationTrigger][i].transform.position).magnitude;
                    if (aDistance < up.alertRange)
                    {
                        targets[nationTrigger][i].aggressionRange = 1000;
                        targets[nationTrigger][i].alerted = true;
                        targets[nationTrigger][i].isReady = true;
                        alertEnemies(targets[nationTrigger][i], nationTrigger);
                    }
                }
            }
        }

        void OnGUI()
        {
            // Display performance UI
            if (showStatistics)
            {
                BattleSystemStatistics.OnGUI();
            }
        }

        Rect GUIRect(float height)
        {
            return new Rect(Screen.width * 0.05f, Screen.height * height, 500f, 20f);
        }

        int iSearchPhase = 0;
        float fSearchPhase = 0f;

        // The main search method, which starts to search for nearest enemies neighbours and set them for attack
        // NN search works with kdtree.cs NN search class, implemented by A. Stark at 2009.
        // Target candidates are put on kdtree, while attackers used to search for them.
        // NN searches are based on position coordinates in 3D.
        public void SearchPhase()
        {
            fSearchPhase += allUnits.Count * searchUpdateFraction;

            int nToLoop = (int)fSearchPhase;
            fSearchPhase -= nToLoop;

            for (int i = 0; i < nToLoop; i++)
            {
                iSearchPhase++;

                if (iSearchPhase >= allUnits.Count)
                {
                    iSearchPhase = 0;
                }

                UnitPars up = allUnits[iSearchPhase];
                int nation = up.nation;

                if (up.isReady && !up.setToResupply && targets[nation].Count > 0)
                {
                    int targetId = targetKD[nation].FindNearest(up.transform.position);
                    UnitPars targetUp = targets[nation][targetId];

                    if (targetUp.health > 0f &&
                        targetUp.noAttackers < targetUp.maxAttackers && up.fireAtWill && up.firearm != null)
                    {
                        targetUp.attackers.Add(up);
                        targetUp.noAttackers = targetUp.attackers.Count;
                        up.target = targetUp;
                        UnityEngine.AI.NavMeshAgent upNav = up.GetComponent<UnityEngine.AI.NavMeshAgent>();
                        UnityEngine.AI.NavMeshAgent targNav = targetUp.GetComponent<UnityEngine.AI.NavMeshAgent>();

                        upNav.stoppingDistance = upNav.radius / (upNav.transform.localScale.x) + targNav.radius / (targetUp.transform.localScale.x);

                        // distance between attacker and target

                        float rTarget = (up.transform.position - targetUp.transform.position).magnitude;

                        if (rTarget < up.firearm.range + up.aggressionRange)
                        {
                            Debug.Log("Fire at will log");
                            up.isApproaching = true;
                            up.isReady = false;
                        }
                        else
                        {
                            targetUp.attackers.Remove(up);
                            targetUp.noAttackers = targetUp.attackers.Count;
                            up.target = null;
                        }
                    }
                }
                if(up.isReady && up.setToResupply && friendlyTargets[nation].Count > 0)
                {
                    int targetId = friendlyKD[nation].FindNearest(up.transform.position);
                    UnitPars targetUp = friendlyTargets[nation][targetId];
                    if (targetUp.health > 0f &&
                        targetUp.noAttackers == 0 &&
                        targetUp.noSuppliers < targetUp.maxSuppliers)
                    {
                        Debug.Log("Searching unit requiring supply");
                        targetUp.suppliers.Add(up);
                        targetUp.noSuppliers = targetUp.suppliers.Count;
                        up.target = targetUp;
                        up.isSupplying = true;
                        up.isReady = false;
                    }
                }
                if(up.isResupplying && sourceTargets[nation].Count > 0)
                {
                    int targetId = sourceKD[nation].FindNearest(up.transform.position);
                    UnitPars targetUp = sourceTargets[nation][targetId];
                    if(targetUp.health > 0f && targetUp.noSuppliers < targetUp.maxSuppliers)
                    {
                        Debug.Log("Searching Supply source");
                        targetUp.suppliers.Add(up);
                        targetUp.noSuppliers = targetUp.suppliers.Count;
                        up.target = targetUp;
                        up.isResupplying = false;
                        up.isMovingToResupply = true;
                    }
                }
            }
        }

        private void RefreshTargetList(float deltaTime)
        {
            UnitPars up;
            for (int i = 0; i < targetRefreshTimes.Count; i++)
            {
                targetRefreshTimes[i] -= deltaTime;
                if (targetRefreshTimes[i] < 0f)
                {
                    targetRefreshTimes[i] = 1f;

                    List<UnitPars> nationTargets = new List<UnitPars>();
                    List<UnitPars> nationFriendlies = new List<UnitPars>();
                    List<UnitPars> nationSources = new List<UnitPars>();
                    List<Vector3> nationTargetPositions = new List<Vector3>();
                    List<Vector3> nationFriendlyPositions = new List<Vector3>();
                    List<Vector3> nationSourcePositions = new List<Vector3>();

                    for (int j = 0; j < allUnits.Count; j++)
                    {
                        up = allUnits[j];

                        if (up.nation != i &&
                            up.isApproachable &&
                            up.health > 0f &&
                            up.attackers.Count < up.maxAttackers && 
                            Diplomacy.active.relations.Count > up.nation &&
                            Diplomacy.active.relations[up.nation].Count > i &&
                            Diplomacy.active.relations[up.nation][i] == 1)
                        {
                            nationTargets.Add(up);
                            nationTargetPositions.Add(up.transform.position);
                        }
                        else if(up.nation == i &&
                            up.isApproachable &&
                            up.health > 0f &&
                            up.noSuppliers < up.maxSuppliers)
                        {
                            if(up.isSource)
                            {
                                nationSources.Add(up);
                                nationSourcePositions.Add(up.transform.position);
                            }
                            else
                            {
                                if (up.currentSupply < up.healingSupplyCap)
                                {
                                    nationFriendlies.Add(up);
                                    nationFriendlyPositions.Add(up.transform.position);
                                }
                            }
                        }
                    }
                    targets[i] = nationTargets;
                    //Debug.Log("Nation " + i.ToString() + " target count: " + targets[i].Count);
                    friendlyTargets[i] = nationFriendlies;
                    sourceTargets[i] = nationSources;
                    targetKD[i] = KDTree.MakeFromPoints(nationTargetPositions.ToArray());
                    friendlyKD[i] = KDTree.MakeFromPoints(nationFriendlyPositions.ToArray());
                    sourceKD[i] = KDTree.MakeFromPoints(nationSourcePositions.ToArray());
                }
            }
        }

        int iRetargetPhase = 0;
        float fRetargetPhase = 0f;

        // Similar as SearchPhase but is used to retarget approachers to closer targets.
        public void RetargetPhase()
        {
            fRetargetPhase += allUnits.Count * retargetUpdateFraction;

            int nToLoop = (int)fRetargetPhase;
            fRetargetPhase -= nToLoop;

            for (int i = 0; i < nToLoop; i++)
            {
                iRetargetPhase++;

                if (iRetargetPhase >= allUnits.Count)
                {
                    iRetargetPhase = 0;
                }

                UnitPars up = allUnits[iRetargetPhase];
                int nation = up.nation;

                if (up.canRetarget && (up.isCharging || up.isShooting || up.isApproaching) && up.target != null && targets[nation].Count > 0)
                {
                    int targetId = targetKD[nation].FindNearest(up.transform.position);
                    UnitPars targetUp = targets[nation][targetId];

                    if (targetUp.health > 0f &&
                        targetUp.attackers.Count < targetUp.maxAttackers)
                    {
                        float oldTargetDistanceSq = (up.target.transform.position - up.transform.position).sqrMagnitude;
                        float newTargetDistanceSq = (targetUp.transform.position - up.transform.position).sqrMagnitude;

                        if (newTargetDistanceSq < oldTargetDistanceSq)
                        {
                            up.target.attackers.Remove(up);
                            up.target.noAttackers = up.target.attackers.Count;

                            targetUp.attackers.Add(up);
                            targetUp.noAttackers = targetUp.attackers.Count;
                            up.target = targetUp;
                        }
                    }
                }

                if(up.isMovingToResupply && up.target != null && sourceTargets[nation].Count > 0)
                {
                    int targetId = sourceKD[nation].FindNearest(up.transform.position);
                    UnitPars targetUp = sourceTargets[nation][targetId];
                    if(targetUp.health > 0f && targetUp.noSuppliers < targetUp.maxSuppliers)
                    {
                        float oldTargetDistanceSq = (up.target.transform.position - up.transform.position).sqrMagnitude;
                        float newTargetDistanceSq = (targetUp.transform.position - up.transform.position).sqrMagnitude;

                        if (newTargetDistanceSq < oldTargetDistanceSq)
                        {
                            Debug.Log("Found closer supply source");
                            up.target.suppliers.Remove(up);
                            up.target.noSuppliers = up.target.suppliers.Count;

                            targetUp.suppliers.Add(up);
                            targetUp.noSuppliers = targetUp.suppliers.Count;
                            up.target = targetUp;
                        }
                    }
                }

                //Might not need redirection for supplying
                /*if(up.isSupplying && up.target != null && friendlyTargets[nation].Count > 0)
                {

                }*/
            }
        }

        int iApproachPhase = 0;
        float fApproachPhase = 0f;

        // this phase starting attackers to move towards their targets
        public void ApproachPhase()
        {
            fApproachPhase += allUnits.Count * approachUpdateFraction;

            int nToLoop = (int)fApproachPhase;
            fApproachPhase -= nToLoop;

            // checking through allUnits list which units are set to approach (isApproaching)
            for (int i = 0; i < nToLoop; i++)
            {
                iApproachPhase++;

                if (iApproachPhase >= allUnits.Count)
                {
                    iApproachPhase = 0;
                }

                UnitPars apprPars = allUnits[iApproachPhase];

                if (apprPars.isApproaching && apprPars.target != null && apprPars.firearm != null)
                {
                    UnitPars targ = apprPars.target;

                    UnityEngine.AI.NavMeshAgent apprNav = apprPars.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    UnityEngine.AI.NavMeshAgent targNav = targ.GetComponent<UnityEngine.AI.NavMeshAgent>();

                    if (targ.isApproachable == true)
                    {
                        // stopping condition for NavMesh

                        apprNav.stoppingDistance = apprNav.radius / (apprPars.transform.localScale.x) + targNav.radius / (targ.transform.localScale.x);

                        // distance between approacher and target

                        float rTarget = (apprPars.transform.position - targ.transform.position).magnitude;
                        float stoppDistance = apprPars.firearm.range;

                        // counting increased distances (failure to approach) between attacker and target;
                        // if counter failedR becomes bigger than critFailedR, preparing for new target search.

                        if (apprPars.prevR <= rTarget)
                        {
                            apprPars.failedR = apprPars.failedR + 1;
                            if (apprPars.failedR > apprPars.critFailedR)
                            {
                                Debug.Log("Approach Crit Fail");
                                apprPars.isApproaching = false;
                                apprPars.isReady = true;
                                apprPars.failedR = 0;

                                if (apprPars.target != null)
                                {
                                    apprPars.target.attackers.Remove(apprPars);
                                    apprPars.target.noAttackers = apprPars.target.attackers.Count;
                                    apprPars.target = null;
                                }

                                if (apprPars.changeMaterial)
                                {
                                    apprPars.GetComponent<Renderer>().material.color = Color.yellow;
                                }
                            }
                        }
                        // if approachers already close to their targets
                        if (rTarget < stoppDistance)
                        {
                            apprNav.SetDestination(apprPars.transform.position);

                            // pre-setting for attacking
                            apprPars.isApproaching = false;
                            apprPars.isShooting = true;

                            if (apprPars.changeMaterial)
                            {
                                apprPars.GetComponent<Renderer>().material.color = Color.magenta;
                            }
                        }
                        else
                        {
                            if (apprPars.changeMaterial)
                            {
                                apprPars.GetComponent<Renderer>().material.color = Color.green;
                            }

                            // starting to move
                            if (apprPars.isMovable)
                            {
                                Vector3 destination = apprNav.destination;
                                if ((destination - targ.transform.position).sqrMagnitude > 1f)
                                {
                                    apprNav.SetDestination(targ.transform.position);
                                    apprNav.speed = apprPars.moveSpeed;
                                }
                            }
                        }


                        // saving previous R
                        apprPars.prevR = rTarget;
                    }
                    // condition for non approachable targets	
                    else
                    {
                        apprPars.target = null;
                        apprNav.SetDestination(apprPars.transform.position);

                        apprPars.isApproaching = false;
                        apprPars.isReady = true;

                        if (apprPars.changeMaterial)
                        {
                            apprPars.GetComponent<Renderer>().material.color = Color.yellow;
                        }
                    }
                }
            }
        }

        int iAttackPhase = 0;
        float fAttackPhase = 0f;

        // Attacking phase set attackers to attack their targets and cause damage when they already approached their targets
        public void AttackPhase()
        {
            fAttackPhase += allUnits.Count * attackUpdateFraction;

            int nToLoop = (int)fAttackPhase;
            fAttackPhase -= nToLoop;

            // checking through allUnits list which units are set to approach (isAttacking)
            for (int i = 0; i < nToLoop; i++)
            {
                iAttackPhase++;

                if (iAttackPhase >= allUnits.Count)
                {
                    iAttackPhase = 0;
                }

                UnitPars attPars = allUnits[iAttackPhase];

                if (attPars.isAttacking && attPars.target != null)
                {
                    UnitPars targPars = attPars.target;

                    UnityEngine.AI.NavMeshAgent attNav = attPars.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    UnityEngine.AI.NavMeshAgent targNav = targPars.GetComponent<UnityEngine.AI.NavMeshAgent>();

                    attNav.stoppingDistance = attNav.radius / (attPars.transform.localScale.x) + targNav.radius / (targPars.transform.localScale.x);

                    // distance between attacker and target

                    float rTarget = (attPars.transform.position - targPars.transform.position).magnitude;
                    float stoppDistance = (2.5f + attPars.transform.localScale.x * targPars.transform.localScale.x * attNav.stoppingDistance);

                    // if target moves away, resetting back to approach target phase

                    if (rTarget > stoppDistance)
                    {
                        //Debug.Log("Attack Log");
                        attPars.isCharging = true;
                        attPars.isAttacking = false;
                    }
                    // if targets becomes immune, attacker is reset to start searching for new target 	
                    else if (targPars.isImmune == true)
                    {
                        attPars.isAttacking = false;
                        attPars.isReady = true;

                        targPars.attackers.Remove(attPars);
                        targPars.noAttackers = targPars.attackers.Count;

                        if (attPars.changeMaterial)
                        {
                            attPars.GetComponent<Renderer>().material.color = Color.yellow;
                        }
                    }
                    // attacker starts attacking their target	
                    else
                    {
                        if (attPars.changeMaterial)
                        {
                            attPars.GetComponent<Renderer>().material.color = Color.red;
                        }

                        float strength = attPars.strength;
                        float defence = targPars.defence;

                        // if attack passes target through target defence, cause damage to target
                        if (Random.value > (strength / (strength + defence)))
                        { 
                            targPars.health -= Mathf.Min(strength * Random.value, 0.5f * strength);
                        }
                    }
                }
            }
        }

        int iSelfHealingPhase = 0;
        float fSelfHealingPhase = 0f;

        // Self-Healing phase heals damaged units over time
        public void SelfHealingPhase(float deltaTime)
        {
            fSelfHealingPhase += allUnits.Count * selfHealUpdateFraction;

            int nToLoop = (int)fSelfHealingPhase;
            fSelfHealingPhase -= nToLoop;

            // checking which units are damaged	
            for (int i = 0; i < nToLoop; i++)
            {
                iSelfHealingPhase++;

                if (iSelfHealingPhase >= allUnits.Count)
                {
                    iSelfHealingPhase = 0;
                }

                UnitPars shealPars = allUnits[iSelfHealingPhase];
                // if unit has less health than 0, preparing it to die
                if (shealPars.health < 0f)
                {
                    //shealPars.isHealing = false;
                    shealPars.isImmune = true;
                    shealPars.isDying = true;
                }
                // healing unit	
                else
                {
                    if(shealPars.isGenerating)
                    {
                        shealPars.currentSupply += shealPars.supplyDrain * deltaTime / selfHealUpdateFraction;
                        if(shealPars.currentSupply > shealPars.maxSupply)
                        {
                            shealPars.currentSupply = shealPars.maxSupply;
                        }
                    }
                    else
                    {
                        if(shealPars.autoResupply && !shealPars.isMovingToResupply && shealPars.currentSupply < shealPars.healingSupplyCap && !shealPars.isShooting && !shealPars.isAttacking)
                        {
                            shealPars.isResupplying = true;
                        }

                        if (shealPars.health < shealPars.maxHealth && shealPars.currentSupply > shealPars.healingSupplyCap)
                        {
                            shealPars.health += shealPars.selfHealFactor * deltaTime / selfHealUpdateFraction;
                            shealPars.currentSupply -= shealPars.healingSupplyDrain * deltaTime / selfHealUpdateFraction;
                            // if unit health reaches maximum, unset self-healing
                            if (shealPars.health > shealPars.maxHealth)
                            {
                                shealPars.health = shealPars.maxHealth;
                            }
                        }
                        else
                        {
                            if (shealPars.currentSupply > 0)
                            {
                                shealPars.currentSupply -= shealPars.supplyDrain * deltaTime / selfHealUpdateFraction;
                            }
                            else
                            {
                                shealPars.health -= shealPars.lackOfSupplyDamage * deltaTime / selfHealUpdateFraction;
                            }
                        }
                    }
                }
            }
        }

        int iDeathPhase = 0;
        float fDeathPhase = 0f;

        // Death phase unset all unit activity and prepare to die
        public void DeathPhase()
        {
            fDeathPhase += allUnits.Count * deathUpdateFraction;

            int nToLoop = (int)fDeathPhase;
            fDeathPhase -= nToLoop;

            // Getting dying units		
            for (int i = 0; i < nToLoop; i++)
            {
                iDeathPhase++;

                if (iDeathPhase >= allUnits.Count)
                {
                    iDeathPhase = 0;
                }

                UnitPars deadPars = allUnits[iDeathPhase];

                if (deadPars.isDying)
                {
                    // If unit is dead long enough, prepare for rotting (sinking) phase and removing from the unitss list
                    if (deadPars.deathCalls > deadPars.maxDeathCalls)
                    {
                        deadPars.isDying = false;
                        deadPars.isSinking = true;

                        deadPars.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
                        sinks.Add(deadPars);
                        allUnits.Remove(deadPars);

                        for (int j = 0; j < targetRefreshTimes.Count; j++)
                        {
                            targetRefreshTimes[j] = -1f;
                        }
                    }
                    // unsetting unit activity and keep it dying	
                    else
                    {
                        deadPars.autoResupply = false;
                        deadPars.setToResupply = false;
                        deadPars.fireAtWill = false;
                        deadPars.isCharging = false;
                        deadPars.isMovable = true;
                        deadPars.isSource = false;
                        deadPars.isGenerating = false;
                        deadPars.isResupplying = false;
                        deadPars.isMovingToResupply = false;
                        deadPars.isSupplying = false;
                        deadPars.isShooting = false;
                        deadPars.isReady = false;
                        deadPars.isApproaching = false;
                        deadPars.isAttacking = false;
                        deadPars.isApproachable = false;
                        deadPars.isHealing = false;

                        if (deadPars.target != null)
                        {
                            deadPars.target.attackers.Remove(deadPars);
                            deadPars.target.noAttackers = deadPars.target.attackers.Count;
                            deadPars.target.suppliers.Remove(deadPars);
                            deadPars.target.noSuppliers = deadPars.target.suppliers.Count;
                            deadPars.target = null;
                        }

                        // unselecting deads	
                        ManualControl manualControl = deadPars.GetComponent<ManualControl>();

                        if (manualControl != null)
                        {
                            manualControl.isSelected = false;
                            UnitControls.active.Refresh();
                        }

                        deadPars.transform.gameObject.tag = "Untagged";

                        UnityEngine.AI.NavMeshAgent nma = deadPars.GetComponent<UnityEngine.AI.NavMeshAgent>();
                        nma.SetDestination(deadPars.transform.position);
                        nma.avoidancePriority = 0;
                        nma.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.NoObstacleAvoidance;
                        nma.radius = 0.05f;

                        deadPars.deathCalls++;

                        if (deadPars.changeMaterial)
                        {
                            deadPars.GetComponent<Renderer>().material.color = Color.blue;
                        }
                    }
                }
            }
        }

        int iSinkPhase = 0;
        float fSinkPhase = 0f;

        // rotting or sink phase includes time before unit is destroyed: for example to perform rotting animation or sink object into the ground
        public void SinkPhase(float deltaTime)
        {
            float sinkSpeed = -0.2f;

            fSinkPhase += sinks.Count * sinkUpdateFraction;

            int nToLoop = (int)fSinkPhase;
            fSinkPhase -= nToLoop;

            // checking in sinks array, which is already different from main units array
            for (int i = 0; i < sinks.Count; i++)
            {
                iSinkPhase++;

                if (iSinkPhase >= sinks.Count)
                {
                    iSinkPhase = 0;
                }

                UnitPars sinkPars = sinks[iSinkPhase];

                if (sinkPars.isSinking)
                {
                    if (sinkPars.changeMaterial)
                    {
                        sinkPars.GetComponent<Renderer>().material.color = new Color((148.0f / 255.0f), (0.0f / 255.0f), (211.0f / 255.0f), 1.0f);
                    }

                    // moving sinking object down into the ground	
                    if (sinkPars.transform.position.y > -1.0f)
                    {
                        sinkPars.transform.position += new Vector3(0f, sinkSpeed * deltaTime / sinkUpdateFraction, 0f);
                    }
                    // destroy object if it has sinked enough
                    else
                    {
                        sinks.Remove(sinkPars);
                        Destroy(sinkPars.gameObject);
                    }
                }
            }
        }
        
        int iManualMover = 0;
        float fManualMover = 0f;

        // ManualMover controls unit if it is selected and target is defined by player
        public void ManualMover()
        {
            fManualMover += allUnits.Count * 0.1f;

            int nToLoop = (int)fManualMover;
            fManualMover -= nToLoop;

            for (int i = 0; i < nToLoop; i++)
            {
                iManualMover++;

                if (iManualMover >= allUnits.Count)
                {
                    iManualMover = 0;
                }

                UnitPars up = allUnits[iManualMover];
                ManualControl manualControl = up.GetComponent<ManualControl>();

                if (manualControl.isMoving)
                {
                    float r = (up.transform.position - manualControl.manualDestination).magnitude;

                    if (r >= manualControl.prevDist)
                    {
                        manualControl.failedDist++;
                        if (manualControl.failedDist > manualControl.critFailedDist)
                        {
                            manualControl.failedDist = 0;
                            manualControl.isMoving = false;
                            ResetSearching(up);
                        }
                    }

                    manualControl.prevDist = r;
                }

                if (manualControl.prepareMoving)
                {
                    if (up.isMovable)
                    {
                        if (up.target == null)
                        {
                            UnSetSearching(up);
                        }

                        manualControl.prepareMoving = false;
                        manualControl.isMoving = true;

                        up.GetComponent<UnityEngine.AI.NavMeshAgent>().SetDestination(manualControl.manualDestination);
                    }
                }
            }
        }

        public void ResetSearching(UnitPars up)
        {
            if (up.setToResupply && up.target != null)
            {
                up.target.suppliers.Remove(up);
                up.target.noSuppliers = up.target.suppliers.Count;
            }
            else if(up.target != null)
            {
                up.target.attackers.Remove(up);
                up.target.noAttackers = up.target.attackers.Count;
            }
            up.target = null;

            up.isCharging = false;
            up.isResupplying = false;
            up.isMovingToResupply = false;
            up.isSupplying = false;
            up.isShooting = false;
            up.isApproaching = false;
            up.isAttacking = false;
            //up.isApproachable = false;
            up.isHealing = false;

            up.GetComponent<UnityEngine.AI.NavMeshAgent>().SetDestination(up.transform.position);

            if (up.changeMaterial)
            {
                up.GetComponent<Renderer>().material.color = Color.yellow;
            }

            up.isReady = true;
        }

        public void UnSetSearching(UnitPars up)
        {
            up.isReady = false;
            if(up.setToResupply && up.target != null)
            {
                up.target.suppliers.Remove(up);
                up.target.noSuppliers = up.target.suppliers.Count;
            }
            else if(up.target != null)
            {
                up.target.attackers.Remove(up);
                up.target.noAttackers = up.target.attackers.Count;
            }
            up.target = null;

            up.isCharging = false;
            up.isResupplying = false;
            up.isMovingToResupply = false;
            up.isSupplying = false;
            up.isShooting = false;
            up.isApproaching = false;
            up.isAttacking = false;
            //up.isApproachable = false;
            up.isHealing = false;

            up.GetComponent<UnityEngine.AI.NavMeshAgent>().SetDestination(up.transform.position);

            if (up.changeMaterial)
            {
                up.GetComponent<Renderer>().material.color = Color.grey;
            }
        }

        public void AddNation()
        {
            targets.Add(new List<UnitPars>());
            friendlyTargets.Add(new List<UnitPars>());
            sourceTargets.Add(new List<UnitPars>());
            targetRefreshTimes.Add(-1f);
            targetKD.Add(null);
            friendlyKD.Add(null);
            sourceKD.Add(null);
        }
    }
}
