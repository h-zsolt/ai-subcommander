using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AI_Controller : MonoBehaviour
{
    [Header("Return values")]
    public RTSToolkitFree.UnitPars reccommendedUnit;
    private RTSToolkitFree.UnitPars bestCombat;
    private RTSToolkitFree.UnitPars bestSupply;
    [Header("Configurable Variables")]
    public int nationID = 0;
    public const float DPS_WEIGHT_AGAINST_SUPPLY = 1.0f;
    public bool forcePush = false;
    public float aggressionSlider = 1.0f;
    public float reactiveSlider = 1.0f;
    public float desiredBalance = 1.0f;
    public float desiredAdvantage = 0.1f;
    public float scanDistance = 100.0f;
    public Vector3 noisePerUnit = new Vector3(0.25f, 0, 0.25f);
    public KeyCode addUnitKey;
    public KeyCode startPlanningKey;
    public KeyCode startPushingKey;
    public KeyCode removeUnitKey;

    [Header("Lists")]
    public List<RTSToolkitFree.UnitPars> controlList = new List<RTSToolkitFree.UnitPars>();
    public List<UnityEngine.AI.NavMeshAgent> controlledNavMeshes = new List<UnityEngine.AI.NavMeshAgent>();
    public List<int> designatedSpot = new List<int>();
    public List<float> predictedUnitDPS = new List<float>();
    public List<AI_Designation> AIDesignations = new List<AI_Designation>();

    public List<RTSToolkitFree.UnitPars> nextWave = new List<RTSToolkitFree.UnitPars>();
    public List<RTSToolkitFree.UnitPars> targetList = new List<RTSToolkitFree.UnitPars>();
    public List<int> enemyTrailSpot = new List<int>();

    [Header("Utility Variables")]
    public Vector3 homeBaseLocation;
    public RTSToolkitFree.KDTree trailTree;
    public List<Vector3> trailSpots = new List<Vector3>();
    public int enemyControlLine = 0;
    public int selfControlLine = 0;
    public int fallBackLine = 0;
    public float totalDistanceTime;
    public float targetSpotTime;
    public AI_States AIState = AI_States.DEFENCE;
    public Vector2 aggressionClamper = new Vector2(0.8f, 10.0f);
    public Vector2 reactionClamper = new Vector2(1.0f, 2.0f);

    [Header("Battle Simulation Variables")]
    public List<float> enemyHealthWeights = new List<float>();
    public List<float> effectiveDPS = new List<float>();
    public List<float> enemyDPS = new List<float>();
    public List<Vector2> effectiveHealth = new List<Vector2>();
    public List<Vector2> enemyHealth = new List<Vector2>();
    public float enemyTTKScore;
    public float playerTTKScore;

    [Header("Supply Simulation Variables")]
    public float idleSupplyDrain;
    public float maxSupplyDrain;
    public float supplyRateBase;
    public float supplyRateTarget;
    public float supplyRouteTime;
    public float newSupplyTime;

    [Header("Path Measuring Objects")]
    public GameObject routingObject;
    public GameObject supplyTimeObject;

    public enum AI_States
    {
        DEFENCE = 0,
        ROUTING,
        PLANNING,
        ANALYZING,
        ENGAGED
    }

    public enum AI_Designation
    {
        FALLBACK = 0,
        FALLINGBACK,
        PUSHTHROUGH,
        ENGAGED,
        RESTING,
        NONE
    }

    [Header("Function Specific Variables")]
    public bool measureDistanceStarted = false;
    Vector3 keepHoldOfTarget = new Vector3(274.35f, 3.08f, 955.19f);
    public void DesignateNewTarget(Vector3 targetLocation)
    {
        AIState = AI_States.ROUTING;
        ResetData();
        measureDistanceStarted = true;
        routingObject.transform.position = targetLocation;
        keepHoldOfTarget = targetLocation;

        var nma = routingObject.GetComponent<UnityEngine.AI.NavMeshAgent>();
        UnityEngine.AI.NavMeshAgent upNma = controlledNavMeshes[0];
        float slowest = Mathf.Infinity;
        for (int i = 0; i < controlList.Count; i++)
        {
            var checkNma = controlledNavMeshes[i];
            if (checkNma.speed < slowest)
            {
                slowest = checkNma.speed;
                upNma = checkNma;
            }
        }
        nma.speed = 2 * upNma.speed;
        nma.angularSpeed = 2 * upNma.angularSpeed;
        nma.acceleration = 2 * upNma.acceleration;
        //nma.radius = 0.05f;

        nma.SetDestination(homeBaseLocation);

        RouteSupply(targetLocation);
    }

    private void ResetData()
    {
        designatedSpot.Clear();
        AIDesignations.Clear();
        for (int i = 0; i < controlList.Count; i++)
        {
            designatedSpot.Add(0);
            AIDesignations.Add(AI_Designation.NONE);
        }
        nextWave.Clear();
        targetList.Clear();
        enemyTrailSpot.Clear();
        trailSpots.Clear();
        effectiveDPS.Clear();
        enemyDPS.Clear();
        effectiveHealth.Clear();
        enemyHealth.Clear();
        enemyHealthWeights.Clear();
        enemyControlLine = 0;
        fallBackLine = 0;
        totalDistanceTime = 0;
    }

    private void Start()
    {
        homeBaseLocation = gameObject.transform.position;
    }

    public void addUnitToAICommand(RTSToolkitFree.UnitPars up)
    {
        if (!controlList.Contains(up))
        {
            up.canRetarget = false;
            controlList.Add(up);
            controlledNavMeshes.Add(up.GetComponent<UnityEngine.AI.NavMeshAgent>());
            designatedSpot.Add(0);
            AIDesignations.Add(AI_Designation.NONE);
            predictedUnitDPS.Add(0);
        }
    }
    
    public void RemoveUnitFromAI(RTSToolkitFree.UnitPars up)
    {
        for(int i = 0; i<controlList.Count;i++)
        {
            if(controlList[i] == up)
            {
                controlList.RemoveAt(i);
                controlledNavMeshes.RemoveAt(i);
                designatedSpot.RemoveAt(i);
                AIDesignations.RemoveAt(i);
                predictedUnitDPS.RemoveAt(i);
            }
        }
    }

    public bool DEBUG = true;
    public bool trailingStarted = false;
    // Update is called once per frame
    void Update()
    {
        float deltaTime = Time.deltaTime;
        VarianceAdjusterAndClamper();
        RemoveDead();
        PathingAgents(deltaTime);
        CalculateSupply(deltaTime);
        PushSupply();
        CalculatePredictedUnitDPS();

        if (isInCombatState(AIState))
        {
            CombatRefresh(deltaTime);
            ReportBalanceOfPower(deltaTime);
            CheckForEngagement(deltaTime);
            if (AIState == AI_States.ANALYZING)
            {
                PushNextPoint(deltaTime);
            }
            if (AIState == AI_States.ENGAGED)
            {
                CheckEndangered();
                ExecuteFallBack(deltaTime);
                if(nextWave.Count == 0)
                {
                    AIState = AI_States.ANALYZING;
                    EstablishEnemyControl();
                    ExecutePush();
                }
            }
        }
        reccommendUnits(deltaTime);

        if (DEBUG)
        {
            HighlightPath();
        }
    }

    public void VarianceAdjusterAndClamper()
    {
        if(aggressionSlider < aggressionClamper.x)
        {
            aggressionSlider = aggressionClamper.x;
        }
        if (aggressionSlider > aggressionClamper.y)
        {
            aggressionSlider = aggressionClamper.y;
        }
        if(reactiveSlider < reactionClamper.x)
        {
            reactiveSlider = reactionClamper.x;
        }
        if(reactiveSlider > reactionClamper.y)
        {
            reactiveSlider = reactionClamper.y;
        }
        endageredFraction = 0.1f * ((reactiveSlider * 9.0f) - 8.0f);
        targetExecuteFallback = 0.5f * (1.5f - (reactiveSlider / 2.0f));
        targetEngagementCheckTime = 0.5f * (1.5f - (reactiveSlider / 2.0f));
        targetCombatRefresh = 1.0f * (1.5f - (reactiveSlider / 2.0f));
        targetPushCheck = 0.15f * (1.5f - (reactiveSlider / 2.0f));
    }

    public void ForceEngagement()
    {
        List<Vector3> enemyPositions = new List<Vector3>();
        List<RTSToolkitFree.UnitPars> partialList = new List<RTSToolkitFree.UnitPars>();
        for(int i = 0; i<nextWave.Count;i++)
        {
            if(nextWave[i].noAttackers<nextWave[i].maxAttackers)
            {
                enemyPositions.Add(nextWave[i].transform.position);
                partialList.Add(nextWave[i]);
            }
        }
        RTSToolkitFree.KDTree waveKD = RTSToolkitFree.KDTree.MakeFromPoints(enemyPositions.ToArray());
        for(int i = 0; i<controlList.Count;i++)
        {
            if(!controlList[i].setToResupply && controlList[i].target == null && (AIDesignations[i] == AI_Designation.ENGAGED || AIDesignations[i] == AI_Designation.NONE) && nextWave.Count>0)
            {
                controlList[i].target = partialList[waveKD.FindNearest(controlList[i].transform.position)];
                controlList[i].isReady = false;
                controlList[i].isApproaching = true;
            }
        }
    }


    public float targetExecuteFallback = 0.5f;
    public float currentExecuteFallback = 0.5f;
    public const float CHECKBOX_DIMS = 2.0f;
    public const float PUSHVECTOR = 2.5f;
    public void ExecuteFallBack(float deltaTime)
    {
        currentExecuteFallback -= deltaTime;
        if (currentExecuteFallback < 0)
        {
            currentExecuteFallback = targetExecuteFallback;
            ForceEngagement();
            for (int i = 0; i < AIDesignations.Count; i++)
            {
                if (AIDesignations[i] == AI_Designation.FALLBACK)
                {
                    //New destination
                    AIDesignations[i] = AI_Designation.FALLINGBACK;
                    var manualControl = controlList[i].GetComponent<RTSToolkitFree.ManualControl>();
                    manualControl.prepareMoving = true;
                    manualControl.manualDestination = trailSpots[fallBackLine];

                    //Create Bounding box
                    Vector3 dir = trailSpots[fallBackLine] - controlList[i].transform.position;
                    Vector3 left = Vector3.Cross(dir, Vector3.up).normalized;
                    Vector3[] boundingBox = new Vector3[4];
                    boundingBox[0] = controlList[i].transform.position + (left * CHECKBOX_DIMS);
                    boundingBox[1] = trailSpots[fallBackLine] + (left * CHECKBOX_DIMS);
                    boundingBox[2] = trailSpots[fallBackLine] - (left * CHECKBOX_DIMS);
                    boundingBox[3] = controlList[i].transform.position - (left * CHECKBOX_DIMS);
                    //Check if any valid allies are in box
                    for (int j = 0; j < controlList.Count; j++)
                    {
                        if (!controlList[j].setToResupply && AIDesignations[j] != AI_Designation.PUSHTHROUGH && AIDesignations[j] != AI_Designation.FALLBACK && AIDesignations[j] != AI_Designation.FALLINGBACK)
                        {
                            if (IsInsideRect(boundingBox, controlList[j].transform.position))
                            {
                                AIDesignations[j] = AI_Designation.PUSHTHROUGH;
                                var pushControl = controlList[j].GetComponent<RTSToolkitFree.ManualControl>();
                                pushControl.prepareMoving = true;
                                if ((controlList[i].transform.position + left - controlList[j].transform.position).magnitude < (controlList[i].transform.position - left - controlList[j].transform.position).magnitude)
                                {
                                    Debug.Log("Pushing a unit left");
                                    pushControl.manualDestination = controlList[j].transform.position + left * PUSHVECTOR;
                                    RecursivePush(controlList[j].transform.position, controlList[j].transform.position + left * PUSHVECTOR);
                                }
                                else
                                {
                                    Debug.Log("Pushing a unit right");
                                    pushControl.manualDestination = controlList[j].transform.position - left * PUSHVECTOR;
                                    RecursivePush(controlList[j].transform.position, controlList[j].transform.position - left * PUSHVECTOR);
                                }
                            }
                        }
                    }
                    //Vector3 right = -left;
                }
            }
        }
    }

    public void RecursivePush(Vector3 origin, Vector3 target)
    {
        Vector3 pushVector = target - origin;
        Vector3 left = Vector3.Cross(pushVector, Vector3.up).normalized;
        Vector3[] boundingBox = new Vector3[4];
        for (int i = 0; i < controlList.Count; i++)
        {
            if (!controlList[i].setToResupply && AIDesignations[i] != AI_Designation.PUSHTHROUGH && AIDesignations[i] != AI_Designation.FALLBACK && AIDesignations[i] != AI_Designation.FALLINGBACK)
            {
                boundingBox[0] = controlList[i].transform.position + (left * CHECKBOX_DIMS);
                boundingBox[1] = target + (left * CHECKBOX_DIMS);
                boundingBox[2] = target - (left * CHECKBOX_DIMS);
                boundingBox[3] = controlList[i].transform.position - (left * CHECKBOX_DIMS);
                if (IsInsideRect(boundingBox, controlList[i].transform.position))
                {
                    AIDesignations[i] = AI_Designation.PUSHTHROUGH;
                    var pushControl = controlList[i].GetComponent<RTSToolkitFree.ManualControl>();
                    pushControl.prepareMoving = true;
                    pushControl.manualDestination = controlList[i].transform.position + pushVector;
                    RecursivePush(controlList[i].transform.position, controlList[i].transform.position + pushVector);
                }
            }
        }
    }

    public bool IsInsideRect(Vector3[] points, Vector3 target)
    {
        bool answer = true;
        float direction = 0f;
        int nextPoint = 1;
        for (int i = 0; i < 4; i++)
        {
            nextPoint = i + 1;
            if (nextPoint == 4)
            {
                nextPoint = 0;
            }
            direction += Mathf.Sign(((target.x - points[i].x) * (points[nextPoint].z - points[i].z)) - ((target.z - points[i].z) * (points[nextPoint].x - points[i].x)));
        }
        //Debug.Log("Inside Rectangle result: " + direction.ToString());
        if (Mathf.Abs(direction) != 4)
        {
            answer = false;
        }
        return answer;
    }

    public float endageredFraction = 0.1f;
    public float fEndangeredPhase = 0f;
    public int iEndangeredPhase = 0;
    public void CheckEndangered()
    {
        fEndangeredPhase += controlList.Count * endageredFraction;

        int nToLoop = (int)fEndangeredPhase;
        fEndangeredPhase -= nToLoop;

        for (int i = 0; i < nToLoop; i++)
        {
            iEndangeredPhase++;

            if (iEndangeredPhase >= controlList.Count)
            {
                iEndangeredPhase = 0;
            }

            var up = controlList[iEndangeredPhase];
            float upDefence;
            float attackerDPS = 0f;
            for (int j = 0; j < up.noAttackers; j++)
            {
                var attacker = up.attackers[j];
                upDefence = up.defence - attacker.firearm.armourPen;
                attackerDPS += attacker.firearm.strength * attacker.firearm.roundsPerMinute * (attacker.firearm.strength / (attacker.firearm.strength + upDefence)) / 45.0f;
            }
            float takeRisk = (aggressionSlider * 2) / (aggressionSlider + 1.0f);
            if (attackerDPS * takeRisk > up.health)
            {
                AIDesignations[iEndangeredPhase] = AI_Designation.FALLBACK;
            }
            else
            {
                if(!up.setToResupply && up.health * takeRisk >= up.maxHealth)
                {
                    AIDesignations[iEndangeredPhase] = AI_Designation.NONE;
                    if(AIState == AI_States.ENGAGED)
                    {
                        AIDesignations[iEndangeredPhase] = AI_Designation.ENGAGED;
                    }
                }
            }
        }
    }

    public float targetEngagementCheckTime = 0.5f;
    public float currentEngagementCheckTime = 0.5f;
    public void CheckForEngagement(float deltaTime)
    {
        currentEngagementCheckTime -= deltaTime;
        if (currentEngagementCheckTime < 0)
        {
            currentEngagementCheckTime = targetEngagementCheckTime;
            for (int i = 0; i < controlList.Count && AIState != AI_States.ENGAGED; i++)
            {
                if (controlList[i].noAttackers > 0 || (controlList[i].target != null && nextWave.Contains(controlList[i].target)))
                {
                    AIState = AI_States.ENGAGED;
                    HaltMovement();
                }
            }
        }
    }

    public void HaltMovement()
    {
        for(int i = 0; i<controlList.Count;i++)
        {
            var up = controlList[i];
            if (!up.setToResupply && AIDesignations[i]!=AI_Designation.FALLINGBACK && AIDesignations[i] !=AI_Designation.RESTING)
            {
                var manualControl = up.GetComponent<RTSToolkitFree.ManualControl>();
                manualControl.isMoving = false;
                RTSToolkitFree.BattleSystem.active.ResetSearching(up);
            }
        }
    }

    public float pushedSupplyRateMin = 0.8f;
    public float pushSupplyFraction = 0.1f;
    public float fPushSupplyPhase = 0f;
    public int iPushSupplyPhase = 0;
    public void PushSupply()
    {
        fPushSupplyPhase += controlList.Count * pushSupplyFraction;

        int nToLoop = (int)fPushSupplyPhase;
        fPushSupplyPhase -= nToLoop;

        for (int i = 0; i < nToLoop; i++)
        {
            iPushSupplyPhase++;

            if (iPushSupplyPhase >= controlList.Count)
            {
                iPushSupplyPhase = 0;
            }

            var supplier = controlList[iPushSupplyPhase];

            if (supplier.setToResupply && supplier.isReady && !supplier.isMovingToResupply && !supplier.isResupplying)
            {
                RTSToolkitFree.UnitPars lowestUP = null;
                float lowestSupply = 2.0f;
                for (int j = 0; j < controlList.Count; j++)
                {
                    var checkAgainst = controlList[j];
                    if (!checkAgainst.setToResupply && checkAgainst.noSuppliers == 0 && (AIDesignations[j] != AI_Designation.ENGAGED || AIDesignations[j] != AI_Designation.PUSHTHROUGH))
                    {
                        float supplyRate = checkAgainst.currentSupply / checkAgainst.maxSupply;
                        if (supplyRate < pushedSupplyRateMin && supplyRate < lowestSupply)
                        {
                            lowestSupply = supplyRate;
                            lowestUP = checkAgainst;
                        }
                    }
                }
                if (lowestUP != null)
                {
                    supplier.target = lowestUP;
                    lowestUP.suppliers.Add(supplier);
                    lowestUP.noSuppliers = lowestUP.suppliers.Count;
                    supplier.isReady = false;
                    supplier.isSupplying = true;
                }
            }
        }
    }

    public int noiseCounter = 0;
    public float targetPushCheck = 0.15f;
    public float currentPushCheck = 0.15f;
    public bool pushTimerStarted = false;
    public void PushNextPoint(float deltaTime)
    {
        currentPushCheck -= deltaTime;
        if(currentPushCheck < 0)
        {
            currentPushCheck = targetPushCheck;
            for (int i = 0; i < controlList.Count; i++)
            {
                var up = controlList[i];
                
                if (AIState != AI_States.DEFENCE && (up.transform.position - trailSpots[selfControlLine]).magnitude < PROXIMITY_TOLERANCE / 2.0f && (balanceReport || forcePush))
                {
                    selfControlLine++;
                    pushTimerStarted = true;
                    HaltMovement();
                    //Offensive complete
                    if (selfControlLine >= trailSpots.Count - 1)
                    {
                        AIState = AI_States.DEFENCE;
                        ResetData();
                        homeBaseLocation = keepHoldOfTarget;
                        supplyRateBase = supplyRateTarget;
                        supplyRateTarget = 0;
                        newSupplyTime = 0;
                        selfControlLine = 0;
                    }
                }
                if ((balanceReport || forcePush) && !pushTimerStarted)
                {
                    noiseCounter = (int)aggressionSlider;
                    ExecutePush();
                }
                else
                {
                    pushTimerStarted = false;
                }
            }
        }
    }

    private void ExecutePush()
    {
        for (int j = 0; j < controlList.Count; j++)
        {
            var updateUP = controlList[j];
            var manualControl = updateUP.GetComponent<RTSToolkitFree.ManualControl>();
            if (!updateUP.setToResupply && AIDesignations[j]!=AI_Designation.RESTING && AIState!=AI_States.DEFENCE)
            {
                noiseCounter++;
                Vector3 randomisedNoise = new Vector3(Random.Range(-aggressionSlider, aggressionSlider) * noisePerUnit.x, noisePerUnit.y * Random.Range(-aggressionSlider, aggressionSlider), noisePerUnit.z * Random.Range(-aggressionSlider, aggressionSlider));
                manualControl.manualDestination = trailSpots[selfControlLine] + randomisedNoise;
                manualControl.prepareMoving = true;
            }
        }
    }

    float targetReportTime = 1.0f;
    float currentReportTime = 1.0f;
    public bool balanceReport = false;
    public void ReportBalanceOfPower(float deltaTime)
    {
        currentReportTime -= deltaTime;
        if (currentReportTime < 0)
        {
            currentReportTime = targetReportTime;
            if (playerTTKScore != 0 && enemyTTKScore != 0)
            {
                float balanceOfPower = playerTTKScore / enemyTTKScore;
                float balanceTarget = desiredBalance + (desiredAdvantage / aggressionSlider);
                if (balanceOfPower > balanceTarget || nextWave.Count == 0)
                {
                    balanceReport = true;
                    Debug.Log("Balance is in our favour");
                }
                else
                {
                    balanceReport = false;
                    Debug.Log("We are outmatched, waiting for support or recovery");
                }
            }
        }
    }

    private bool isInCombatState(AI_States currentState)
    {
        return (currentState == AI_States.ANALYZING || currentState == AI_States.ENGAGED || currentState == AI_States.PLANNING);
    }

    private void LateUpdate()
    {
        if (Input.GetKeyDown(startPlanningKey))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                DesignateNewTarget(hit.point);
            }
        }
        if (Input.GetKeyDown(startPushingKey) && AIState == AI_States.PLANNING)
        {
            AIState = AI_States.ANALYZING;
            ExecutePush();
            forcePush = true;
        }
    }

    public float targetCombatRefresh = 1.0f;
    public float currentCombatRefresh = 0.0f;
    private void CombatRefresh(float deltaTime)
    {
        currentCombatRefresh -= deltaTime;
        if (currentCombatRefresh < 0)
        {
            currentCombatRefresh = targetCombatRefresh;
            if (nextWave.Count > 0)
            {
                CountWaveArmour();
                CountSelfArmour();
                CountSelfDPS();
                CountEnemyDPS();
            }
        }
    }

    private void HighlightPath()
    {
        for (int i = 0; i < trailSpots.Count; i++)
        {
            var trailColour = Color.yellow;
            if (i == enemyControlLine)
            {
                trailColour = Color.red;
            }
            if (i == selfControlLine)
            {
                trailColour = Color.green;
            }
            Debug.DrawRay(trailSpots[i], new Vector3(0, 60, 0), trailColour, targetCombatRefresh);
        }
    }

    public void RemoveDead()
    {
        for (int i = 0; i < controlList.Count; i++)
        {
            if (controlList[i].isDying || controlList[i].isSinking || controlList[i] == null)
            {
                controlList.RemoveAt(i);
                controlledNavMeshes.RemoveAt(i);
                designatedSpot.RemoveAt(i);
                AIDesignations.RemoveAt(i);
                predictedUnitDPS.RemoveAt(i);
                i--;
            }
        }
        for (int i = 0; i < targetList.Count; i++)
        {
            if (targetList[i].isDying || targetList[i].isSinking || targetList[i] == null)
            {
                targetList.RemoveAt(i);
                enemyTrailSpot.RemoveAt(i);
                i--;
            }
        }
        for(int i = 0; i<nextWave.Count;i++)
        {
            if (nextWave[i].isDying || nextWave[i].isSinking || nextWave[i] == null)
            {
                nextWave.RemoveAt(i);
                i--;
            }
        }
    }

    public float currentSpotTime = 0.0f;
    public float attemptSourcingTimer = 1.0f;
    public float targetSourcingTimer = 1.0f;
    public const float PROXIMITY_TOLERANCE = 10.0f;
    private void PathingAgents(float deltaTime)
    {
        if (supplyRouteCheck)
        {
            var suppNma = supplyTimeObject.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if ((suppNma.destination - suppNma.transform.position).magnitude > PROXIMITY_TOLERANCE)
            {
                newSupplyTime += deltaTime * 10;
            }
            else
            {
                supplyRouteCheck = false;
                if (supplyRouteTime == 0.0f)
                {
                    supplyRouteTime = newSupplyTime;
                    newSupplyTime = 0;
                }
            }
        }
        else
        {
            attemptSourcingTimer -= deltaTime;
            if (attemptSourcingTimer < 0)
            {
                attemptSourcingTimer = targetSourcingTimer;
                if (supplyRouteTime == 0 && homeBaseLocation != null)
                {
                    RouteSupply(homeBaseLocation);
                }
            }
        }
        var nma = routingObject.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (measureDistanceStarted)
        {
            ScanForTargets(deltaTime);
            if ((nma.transform.position - homeBaseLocation).magnitude > PROXIMITY_TOLERANCE / 2.0f)
            {
                totalDistanceTime += deltaTime * 2.0f;
            }
            else
            {
                measureDistanceStarted = false;
                trailingStarted = true;
                targetSpotTime = totalDistanceTime / 60.0f;
                nma.speed = nma.speed / 2.0f;
                nma.angularSpeed = nma.angularSpeed / 2.0f;
                nma.acceleration = nma.acceleration / 2.0f;
                nma.SetDestination(keepHoldOfTarget);
            }
        }

        if (trailingStarted)
        {
            if ((nma.transform.position - keepHoldOfTarget).magnitude > PROXIMITY_TOLERANCE / 2.0f)
            {
                currentSpotTime -= deltaTime;
                if (currentSpotTime < 0.0f)
                {
                    currentSpotTime = targetSpotTime;
                    trailSpots.Add(routingObject.transform.position);
                }
            }
            else
            {
                trailingStarted = false;
                EstablishEnemyControl();
                AIState = AI_States.PLANNING;
            }
        }
    }

    private void EstablishEnemyControl()
    {
        trailTree = RTSToolkitFree.KDTree.MakeFromPoints(trailSpots.ToArray());
        enemyControlLine = trailSpots.Count - 1;
        for (int i = 0; i < targetList.Count; i++)
        {
            enemyTrailSpot[i] = trailTree.FindNearest(targetList[i].transform.position);
            if (enemyTrailSpot[i] < enemyControlLine)
            {
                enemyControlLine = enemyTrailSpot[i];
            }
        }
        PopulateNextWave();
        forcePush = false;
    }

    public void PopulateNextWave()
    {
        for (int i = 0; i < enemyTrailSpot.Count; i++)
        {
            if (enemyTrailSpot[i] == enemyControlLine)
            {
                nextWave.Add(targetList[i]);
                findAlertedEnemies(targetList[i]);
            }
        }
    }

    public void findAlertedEnemies(RTSToolkitFree.UnitPars up)
    {
        for (int i = 0; i < targetList.Count; i++)
        {
            if (!nextWave.Contains(targetList[i]))
            {
                float aDistance = (up.transform.position - targetList[i].transform.position).magnitude;
                if (aDistance < up.alertRange)
                {
                    nextWave.Add(targetList[i]);
                    findAlertedEnemies(targetList[i]);
                }
            }
        }
    }

    public float targetScanTime = 0.1f;
    public float currentScanTime = 0.0f;
    public void ScanForTargets(float deltaTime)
    {
        currentScanTime -= deltaTime;
        if (currentScanTime < 0.0f)
        {
            currentScanTime = targetScanTime;
            var listOfTargets = RTSToolkitFree.BattleSystem.active.GetNationTargets(nationID);
            for (int i = 0; i < listOfTargets.Count; i++)
            {
                if ((listOfTargets[i].transform.position - routingObject.transform.position).magnitude < scanDistance && !targetList.Contains(listOfTargets[i]))
                {
                    targetList.Add(listOfTargets[i]);
                    enemyTrailSpot.Add(trailSpots.Count);
                }
            }
        }
    }

    const float RESUPPLYTIMECONSTANT = 10.0f;
    public bool supplyRouteCheck = false;
    public void RouteSupply(Vector3 routeFrom)
    {
        RTSToolkitFree.KDTree sourceKDTree = RTSToolkitFree.BattleSystem.active.GetSourceKD(nationID);
        if (sourceKDTree != null)
        {
            supplyTimeObject.transform.position = routeFrom;
            var sourceList = RTSToolkitFree.BattleSystem.active.GetSourceList(nationID);
            if (sourceList != null && sourceList.Count > 0 && controlledNavMeshes.Count > 0)
            {
                var targetSource = sourceList[sourceKDTree.FindNearest(routeFrom)];
                //Debug.Log("Target Source Location: " + targetSource.transform.position.ToString());
                var nma = supplyTimeObject.GetComponent<UnityEngine.AI.NavMeshAgent>();
                UnityEngine.AI.NavMeshAgent upNma = controlledNavMeshes[0];
                float slowest = Mathf.Infinity;
                for (int i = 0; i < controlList.Count; i++)
                {
                    var checkNma = controlledNavMeshes[i];
                    if (checkNma.speed < slowest && controlList[i].setToResupply)
                    {
                        slowest = checkNma.speed;
                        upNma = checkNma;
                    }
                }
                nma.speed = 10 * upNma.speed;
                nma.acceleration = 10 * upNma.acceleration;
                nma.angularSpeed = 10 * upNma.angularSpeed;
                supplyRouteCheck = true;
                newSupplyTime = RESUPPLYTIMECONSTANT;
                if (nma.SetDestination(targetSource.transform.position))
                {
                    Debug.Log("Successfully Set Sourcing Destination");
                }
                else
                {
                    Debug.Log("Error setting source destination");
                }
            }
        }
    }

    public float targetRecommendUnitTime = 3.6f;
    public float currentRecommendUnitTime = 3.6f;
    public float supplierRatio = 0.25f;

    public void reccommendUnits(float deltaTime)
    {
        currentRecommendUnitTime -= deltaTime;
        float highestSupplyRate = Mathf.NegativeInfinity;
        float bestCombatScore = Mathf.NegativeInfinity;
        float nrOfSuppliers = 0;
        float nrOfCombat = 0;
        if(currentRecommendUnitTime < 0.0f)
        {
            currentRecommendUnitTime = 0;
            for (int i = 0; i < controlList.Count; i++)
            {
                if (controlList[i].setToResupply)
                {
                    nrOfSuppliers += 1.0f;
                    float supplyRating = controlList[i].maxSupply - controlList[i].healingSupplyCap - (controlList[i].supplyDrain * supplyRouteTime)
                        - predictedUnitDPS[i] * DPS_WEIGHT_AGAINST_SUPPLY * aggressionSlider;
                    if (supplyRating > highestSupplyRate)
                    {
                        bestSupply = controlList[i];
                        highestSupplyRate = supplyRating;
                    }
                }
                else 
                {
                    nrOfCombat += 1.0f;
                    if(nextWave.Count > 0)
                    {
                        //Currently only takes DPS into account
                        //Better solution: run balance with unit type virtually added to the pool
                        //Would require slightly different data point stucture
                        if(predictedUnitDPS[i] > bestCombatScore)
                        {
                            bestCombatScore = predictedUnitDPS[i];
                            bestCombat = controlList[i];
                        }
                    }
                    else
                    {
                        bestCombat = null;
                    }
                }
            }
            if (nrOfCombat > 0f && nrOfSuppliers / nrOfCombat > supplierRatio && supplyRateBase < idleSupplyDrain * 1.2f)
            {
                reccommendedUnit = bestSupply;
            }
            else
            {
                reccommendedUnit = bestCombat;
            }
        }
    }

    public float targetSupplyRefreshTime = 1.0f;
    public float currentSupplyRefreshTime = 0.1f;
    public void CalculateSupply(float deltaTime)
    {
        int nrOfSuppliers = 0;
        currentSupplyRefreshTime -= deltaTime;
        if (currentSupplyRefreshTime < 0.0f)
        {
            maxSupplyDrain = 0;
            idleSupplyDrain = 0;
            supplyRateBase = 0;
            supplyRateTarget = 0;
            currentSupplyRefreshTime = targetSupplyRefreshTime;
            for (int i = 0; i < controlList.Count; i++)
            {
                if (supplyRouteTime != 0)
                {
                    if (controlList[i].setToResupply)
                    {
                        nrOfSuppliers++;
                        float transferCapacity;
                        if (supplyRouteTime != 0)
                        {
                            transferCapacity = controlList[i].maxSupply - controlList[i].healingSupplyCap - (controlList[i].supplyDrain * supplyRouteTime);
                            supplyRateBase += transferCapacity / supplyRouteTime / 2.0f;
                        }
                        if (newSupplyTime != 0)
                        {
                            transferCapacity = controlList[i].maxSupply - controlList[i].healingSupplyCap - (controlList[i].supplyDrain * newSupplyTime);
                            supplyRateTarget += transferCapacity / newSupplyTime / 2.0f;
                        }
                    }
                    else
                    {
                        maxSupplyDrain += controlList[i].healingSupplyDrain;
                        idleSupplyDrain += controlList[i].supplyDrain;
                    }
                }
                else
                {
                    //Handle invalid supply time by recalc?
                }
            }

            List<float> supplyRating = new List<float>();
            int iterator;
            float bestRating = Mathf.NegativeInfinity;

            if (supplyRouteTime != 0 && supplyRateBase < idleSupplyDrain)
            {
                Debug.Log("Adding suppliers: " + supplyRateBase.ToString() + " / " + idleSupplyDrain.ToString());
                float transferCapacity;
                //Need additional suppliers
                for (iterator = 0; iterator < controlList.Count; iterator++)
                {
                    supplyRating.Add(Mathf.NegativeInfinity);
                    if (!controlList[iterator].setToResupply)
                    {
                        supplyRating[iterator] = controlList[iterator].maxSupply - controlList[iterator].healingSupplyCap - (controlList[iterator].supplyDrain * supplyRouteTime);
                        supplyRating[iterator] -= predictedUnitDPS[iterator] * DPS_WEIGHT_AGAINST_SUPPLY * aggressionSlider;
                        if (supplyRating[iterator] > bestRating)
                        {
                            bestRating = supplyRating[iterator];
                        }
                    }
                }
                for (iterator = 0; iterator < controlList.Count; iterator++)
                {
                    if (supplyRateBase < idleSupplyDrain && supplyRating[iterator] == bestRating)
                    {
                        transferCapacity = controlList[iterator].maxSupply - controlList[iterator].healingSupplyCap - (controlList[iterator].supplyDrain * supplyRouteTime);
                        supplyRateBase += transferCapacity / supplyRouteTime / 2.0f;
                        if (newSupplyTime != 0)
                        {
                            supplyRateTarget += transferCapacity / newSupplyTime / 2.0f;
                        }
                        controlList[iterator].setToResupply = true;
                        controlList[iterator].autoResupply = true;
                    }
                }
                supplyRating.Clear();
            }
            if (supplyRouteTime != 0 && supplyRateBase > maxSupplyDrain && nrOfSuppliers > 1)
            {
                bestRating = Mathf.Infinity;
                Debug.Log("Dropping suppliers: " + supplyRateBase.ToString() + " / " + maxSupplyDrain.ToString());
                //Can drop suppliers
                float transferCapacity;
                for (iterator = 0; iterator < controlList.Count; iterator++)
                {
                    supplyRating.Add(Mathf.Infinity);
                    if (controlList[iterator].setToResupply)
                    {
                        supplyRating[iterator] = controlList[iterator].maxSupply - controlList[iterator].healingSupplyCap - (controlList[iterator].supplyDrain * supplyRouteTime);
                        supplyRating[iterator] -= predictedUnitDPS[iterator] * DPS_WEIGHT_AGAINST_SUPPLY * aggressionSlider;
                        //Debug.Log(iterator.ToString() + ": " + supplyRating[iterator] + " with DPS rating " + predictedUnitDPS[iterator]);
                        if (supplyRating[iterator] < bestRating)
                        {
                            bestRating = supplyRating[iterator];
                        }
                    }
                }
                Debug.Log("Best rating to drop: " + bestRating.ToString());
                for (iterator = 0; iterator < controlList.Count; iterator++)
                {
                    if (supplyRateBase > maxSupplyDrain && supplyRating[iterator] == bestRating)
                    {
                        Debug.Log("Attempting to drop ID: " + iterator.ToString());
                        transferCapacity = controlList[iterator].maxSupply - controlList[iterator].healingSupplyCap - (controlList[iterator].supplyDrain * supplyRouteTime);
                        supplyRateBase -= transferCapacity / supplyRouteTime / 2.0f;
                        if (newSupplyTime != 0)
                        {
                            supplyRateTarget -= transferCapacity / newSupplyTime / 2.0f;
                        }
                        controlList[iterator].setToResupply = false;
                        controlList[iterator].autoResupply = false;
                    }
                }
            }
        }
    }

    const float BASE_ARMOUR = 50.0f;
    float predictedDPSfraction = 0.1f;
    int ipredictedDPSPhase = 0;
    float fpredictedDPSPhase = 0.0f;
    public void CalculatePredictedUnitDPS()
    {
        fpredictedDPSPhase += controlList.Count * predictedDPSfraction;

        int nToLoop = (int)fpredictedDPSPhase;
        fpredictedDPSPhase -= nToLoop;

        for (int i = 0; i < nToLoop; i++)
        {
            ipredictedDPSPhase++;

            if (ipredictedDPSPhase >= controlList.Count)
            {
                ipredictedDPSPhase = 0;
            }

            var up = controlList[ipredictedDPSPhase];
            if (nextWave.Count > 0)
            {
                predictedUnitDPS[ipredictedDPSPhase] = 0.0f;
                for (int j = 0; j < enemyHealthWeights.Count; j++)
                {
                    float targetArmour = Mathf.Max(enemyHealth[j].x - up.firearm.armourPen, 0);
                    predictedUnitDPS[ipredictedDPSPhase] += enemyHealthWeights[j] * up.firearm.strength * up.firearm.roundsPerMinute * (up.firearm.strength / (up.firearm.strength + targetArmour)) / 45.0f;
                }
            }
            else
            {
                float predictedArmour = Mathf.Max(BASE_ARMOUR - up.firearm.armourPen, 0);
                predictedUnitDPS[ipredictedDPSPhase] = up.firearm.strength * up.firearm.roundsPerMinute * (up.firearm.strength / (up.firearm.strength + predictedArmour)) / 45.0f;
            }
        }
    }

    float playerHealthTotal = 0;
    float enemyHealthTotal = 0;
    public void CountWaveArmour()
    {
        bool congregated = false;
        enemyHealth.Clear();
        enemyHealthWeights.Clear();
        effectiveDPS.Clear();
        enemyHealthTotal = 0;
        playerTTKScore = 0;
        for (int i = 0; i < nextWave.Count; i++)
        {
            var up = nextWave[i];
            enemyHealthTotal += up.health;
            for (int j = 0; j < enemyHealth.Count; j++)
            {
                if (enemyHealth[j].x == up.defence)
                {
                    enemyHealth[j] = new Vector2(enemyHealth[j].x, enemyHealth[j].y + up.health);
                    congregated = true;
                }
            }
            if (!congregated)
            {
                enemyHealth.Add(new Vector2(up.defence, up.health));
                enemyHealthWeights.Add(0);
                effectiveDPS.Add(0.0f);
            }
            else
            {
                congregated = false;
            }
        }
    }

    public void CountSelfArmour()
    {
        bool congregated = false;
        effectiveHealth.Clear();
        enemyDPS.Clear();
        playerHealthTotal = 0;
        enemyTTKScore = 0;
        for (int i = 0; i < controlList.Count; i++)
        {
            var up = controlList[i];
            if (!up.setToResupply)
            {
                playerHealthTotal += up.health;
                for (int j = 0; j < effectiveHealth.Count; j++)
                {
                    if (effectiveHealth[j].x == up.defence)
                    {
                        effectiveHealth[j] = new Vector2(effectiveHealth[j].x, effectiveHealth[j].y + up.health);
                        congregated = true;
                    }
                }
                if (!congregated)
                {
                    effectiveHealth.Add(new Vector2(up.defence, up.health));
                    enemyDPS.Add(0.0f);
                }
                else
                {
                    congregated = false;
                }
            }
        }
    }

    public void CountSelfDPS()
    {
        RTSToolkitFree.UnitPars up;
        for (int i = 0; i < controlList.Count; i++)
        {
            up = controlList[i];
            if (!up.setToResupply)
            {
                for (int j = 0; j < enemyHealth.Count; j++)
                {
                    float enemyDefence = Mathf.Max(enemyHealth[j].x - up.firearm.armourPen, 0);
                    effectiveDPS[j] += up.firearm.strength * up.firearm.roundsPerMinute * (up.firearm.strength / (up.firearm.strength + enemyDefence)) / 45.0f;
                }
            }
        }
        float healthWeight = 0.0f;
        for (int i = 0; i < enemyHealth.Count; i++)
        {
            healthWeight = enemyHealth[i].y / enemyHealthTotal;
            enemyHealthWeights[i] = healthWeight;
            effectiveDPS[i] *= healthWeight;
            playerTTKScore += effectiveDPS[i] / enemyHealthTotal;
        }
    }

    public void CountEnemyDPS()
    {
        RTSToolkitFree.UnitPars up;
        for (int i = 0; i < nextWave.Count; i++)
        {
            up = nextWave[i];
            for (int j = 0; j < effectiveHealth.Count; j++)
            {
                float unitDefence = Mathf.Max(effectiveHealth[j].x - up.firearm.armourPen, 0);
                enemyDPS[j] += up.firearm.strength * up.firearm.roundsPerMinute * (up.firearm.strength / (up.firearm.strength + unitDefence)) / 45.0f;
            }
        }
        float healthWeight = 0.0f;
        for (int i = 0; i < effectiveHealth.Count; i++)
        {
            healthWeight = effectiveHealth[i].y / playerHealthTotal;
            enemyDPS[i] *= healthWeight;
            enemyTTKScore += enemyDPS[i] / playerHealthTotal;
        }
    }
}
