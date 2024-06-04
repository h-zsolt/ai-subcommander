using UnityEngine;

namespace RTSToolkitFree
{
    public class SelectionManager : MonoBehaviour
    {
        public static SelectionManager active;
        public Texture marqueeGraphics;
        private Vector2 marqueeOrigin;
        private Vector2 marqueeSize;
        public Rect marqueeRect;
        private Rect backupRect;
        public Color rectColor = new Color(1f, 1f, 1f, 0.3f);
        bool selectedByClickRunning = false;

        public int unitControlMode;

        void Awake()
        {
            active = this;
        }

        private void OnGUI()
        {
            marqueeRect = new Rect(marqueeOrigin.x, marqueeOrigin.y, marqueeSize.x, marqueeSize.y);
            GUI.color = rectColor;
            GUI.DrawTexture(marqueeRect, marqueeGraphics);
        }

        void Update()
        {
            Vector2 invertedMousePosition = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

            if (
                Input.GetMouseButtonDown(0) &&
                unitControlMode == 0 &&
                !UnitControls.active.IsMouseOverButtons(invertedMousePosition)
            )
            {
                //Poppulate the selectableUnits array with all the selectable units that exist

                float _invertedY = Screen.height - Input.mousePosition.y;
                marqueeOrigin = new Vector2(Input.mousePosition.x, _invertedY);

                //Check if the player just wants to select a single unit opposed to drawing a marquee and selecting a range of units
                Vector3 camPos = Camera.main.transform.position;

                for (int i = 0; i < BattleSystem.active.allUnits.Count; i++)
                {
                    UnitPars up = BattleSystem.active.allUnits[i];
                    ManualControl manualControl = up.GetComponent<ManualControl>();

                    if (manualControl != null)
                    {
                        manualControl.isSelected = false;
                    }
                }

                UnitControls.active.Refresh();

                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                int smallestDistanceIndex = CheckForClickTarget(ray);

                if (smallestDistanceIndex != -1)
                {
                    UnitPars up = BattleSystem.active.allUnits[smallestDistanceIndex];
                    ManualControl manualControl = up.GetComponent<ManualControl>();

                    if (up.health > 0f && manualControl != null)
                    {
                        manualControl.isSelected = true;
                        selectedByClickRunning = true;
                        UnitControls.active.Refresh();
                    }
                }
            }

            if ((Input.GetMouseButtonDown(1) && unitControlMode != 0))
            {
                unitControlMode = 0;
            }

            if (
                Input.GetMouseButton(0) &&
                unitControlMode == 0 &&
                !UnitControls.active.IsMouseOverButtons(invertedMousePosition)
            )
            {
                float _invertedY = Screen.height - Input.mousePosition.y;
                marqueeSize = new Vector2(Input.mousePosition.x - marqueeOrigin.x, (marqueeOrigin.y - _invertedY) * -1);

                //FIX FOR RECT.CONTAINS NOT ACCEPTING NEGATIVE VALUES
                if (marqueeRect.width < 0)
                {
                    backupRect = new Rect(marqueeRect.x - Mathf.Abs(marqueeRect.width), marqueeRect.y, Mathf.Abs(marqueeRect.width), marqueeRect.height);
                }
                else if (marqueeRect.height < 0)
                {
                    backupRect = new Rect(marqueeRect.x, marqueeRect.y - Mathf.Abs(marqueeRect.height), marqueeRect.width, Mathf.Abs(marqueeRect.height));
                }
                if (marqueeRect.width < 0 && marqueeRect.height < 0)
                {
                    backupRect = new Rect(marqueeRect.x - Mathf.Abs(marqueeRect.width), marqueeRect.y - Mathf.Abs(marqueeRect.height), Mathf.Abs(marqueeRect.width), Mathf.Abs(marqueeRect.height));
                }
            }

            if (Input.GetMouseButtonUp(0) && unitControlMode == 0)
            {
                if (!selectedByClickRunning)
                {
                    for (int i = 0; i < BattleSystem.active.allUnits.Count; i++)
                    {
                        UnitPars up = BattleSystem.active.allUnits[i];

                        //Convert the world position of the unit to a screen position and then to a GUI point
                        Vector3 screenPos = Camera.main.WorldToScreenPoint(up.transform.position);
                        Vector2 screenPoint = new Vector2(screenPos.x, Screen.height - screenPos.y);

                        if (marqueeRect.Contains(screenPoint) || backupRect.Contains(screenPoint))
                        {
                            UnitPars unit = up.GetComponent<UnitPars>();

                            if (
                                unit.nation == Diplomacy.active.playerNation &&
                                unit.isMovable &&
                                unit.health > 0f
                            )
                            {
                                ManualControl manualControl = up.GetComponent<ManualControl>();

                                if (manualControl != null)
                                {
                                    manualControl.isSelected = true;
                                }
                            }
                        }
                    }

                    UnitControls.active.Refresh();
                }

                selectedByClickRunning = false;
            }

            if(!UnitControls.active.IsMouseOverButtons(invertedMousePosition))
            {
                CheckForUnitCommands();
            }

            if (Input.GetMouseButtonUp(0))
            {
                //Reset the marquee so it no longer appears on the screen.
                marqueeRect.width = 0;
                marqueeRect.height = 0;
                backupRect.width = 0;
                backupRect.height = 0;
                marqueeSize = Vector2.zero;
            }
        }

        void CheckForUnitCommands()
        {
            if (
                (Input.GetMouseButtonUp(1) && SelectionManager.active.unitControlMode == 0) ||
                (Input.GetMouseButtonUp(0) && SelectionManager.active.unitControlMode == 1) ||
                (Input.GetMouseButtonUp(0) && SelectionManager.active.unitControlMode == 2)
            )
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    int targetIndex = CheckForClickTarget(ray);

                    for (int i = 0; i < BattleSystem.active.allUnits.Count; i++)
                    {
                        UnitPars up = BattleSystem.active.allUnits[i];
                        ManualControl manualControl = up.GetComponent<ManualControl>();

                        if (manualControl != null && manualControl.isSelected && up.nation == Diplomacy.active.playerNation)
                        {
                            if (!(SelectionManager.active.unitControlMode == 2 && targetIndex == -1))
                            {
                                manualControl.manualDestination = hit.point;
                                manualControl.prepareMoving = true;
                            }

                            if (up.target != null)
                            {
                                UnitPars currentTarget = up.target.GetComponent<UnitPars>();
                                currentTarget.attackers.Remove(up);
                                currentTarget.noAttackers = currentTarget.attackers.Count;
                                up.target = null;
                            }

                            if (
                                targetIndex != -1 &&
                                SelectionManager.active.unitControlMode != 1
                            )
                            {
                                UnitPars target = BattleSystem.active.allUnits[targetIndex];

                                if (target != up)
                                {
                                    up.target = target;
                                    target.attackers.Add(up);
                                    target.noAttackers = target.attackers.Count;
                                }
                            }
                        }
                    }
                }

                SelectionManager.active.unitControlMode = 0;
            }
        }

        public int CheckForClickTarget(Ray ray)
        {
            Vector3 rayDirection = ray.direction;
            Vector3 camPos = Camera.main.transform.position;

            float smallestDistance = float.MaxValue;
            int smallestDistanceIndex = -1;

            for (int i = 0; i < BattleSystem.active.allUnits.Count; i++)
            {
                UnitPars up = BattleSystem.active.allUnits[i];

                if (up.health > 0f)
                {
                    Vector3 pos = up.transform.position;

                    float unitDistFromCamera = (pos - camPos).magnitude;

                    float distFromRay = Vector3.Distance(rayDirection * unitDistFromCamera, pos - camPos);

                    if (distFromRay < smallestDistance)
                    {
                        MeshRenderer mr = up.GetComponent<MeshRenderer>();

                        if (distFromRay < mr.bounds.extents.magnitude)
                        {
                            smallestDistance = distFromRay;
                            smallestDistanceIndex = i;
                        }
                    }
                }
            }

            return smallestDistanceIndex;
        }

        public void StopUnits()
        {
            for (int i = 0; i < BattleSystem.active.allUnits.Count; i++)
            {
                UnitPars up = BattleSystem.active.allUnits[i];
                ManualControl manualControl = up.GetComponent<ManualControl>();

                if (manualControl != null && manualControl.isSelected && up.health > 0f && up.nation == Diplomacy.active.playerNation)
                {
                    manualControl.isMoving = false;
                    BattleSystem.active.ResetSearching(up);
                }
            }
        }
    }
}
