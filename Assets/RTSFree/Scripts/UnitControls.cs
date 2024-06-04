using UnityEngine;

namespace RTSToolkitFree
{
    public class UnitControls : MonoBehaviour
    {
        public static UnitControls active;

        [HideInInspector] public bool isEnabled;

        GUIStyle unclickedStyle;
        GUIStyle notAllowedStyle;
        GUIStyle alowedStyle;

        Rect stopRect;
        Rect moveRect;
        Rect attackRect;

        void Awake()
        {
            active = this;
            Initialize();
        }

        void Initialize()
        {
            unclickedStyle = ColorToStyle(Color.white, Color.black);
            notAllowedStyle = ColorToStyle(Color.white, Color.gray);
            alowedStyle = ColorToStyle(Color.white, Color.green);

            int width = 100;
            int height = 50;

            stopRect = new Rect(Screen.width / 2 - (width + 10) - width / 2, Screen.height - 10 - height, width, height);
            moveRect = new Rect(Screen.width / 2 - width / 2, Screen.height - 10 - height, width, height);
            attackRect = new Rect(Screen.width / 2 + (width + 10) - width / 2, Screen.height - 10 - height, width, height);
        }

        Texture2D ColorToTexture(Color color)
        {
            int res = 4;

            Color[] pixels = new Color[res * res];

            for (int i = 0; i < res * res; i++)
            {
                pixels[i] = color;
            }

            Texture2D tex = new Texture2D(res, res);
            tex.SetPixels(pixels);

            return tex;
        }

        GUIStyle ColorToStyle(Color background, Color text)
        {
            return new GUIStyle
            {
                normal = new GUIStyleState
                {
                    background = ColorToTexture(background),
                    textColor = text
                },
                alignment = TextAnchor.MiddleCenter,
            };
        }

        public void Refresh()
        {
            for (int i = 0; i < BattleSystem.active.allUnits.Count; i++)
            {
                UnitPars up = BattleSystem.active.allUnits[i];
                ManualControl manualControl = up.GetComponent<ManualControl>();

                if (up.isMovable && up.nation == Diplomacy.active.playerNation && manualControl.isSelected)
                {
                    isEnabled = true;
                    return;
                }
            }

            isEnabled = false;
        }

        void OnGUI()
        {
            if (isEnabled)
            {
                if (GUI.Button(stopRect, "Stop", unclickedStyle))
                {
                    SelectionManager.active.StopUnits();
                }

                if (GUI.Button(moveRect, "Move", GetMoveStyle()))
                {
                    SelectionManager.active.unitControlMode = 1;
                }

                if (GUI.Button(attackRect, "Attack", GetAttackStyle()))
                {
                    SelectionManager.active.unitControlMode = 2;
                }
            }
        }

        public bool IsMouseOverButtons(Vector2 mousePosition)
        {
            if (stopRect.Contains(mousePosition) || moveRect.Contains(mousePosition) || attackRect.Contains(mousePosition))
            {
                return true;
            }

            return false;
        }

        GUIStyle GetMoveStyle()
        {
            if (SelectionManager.active.unitControlMode == 1)
            {
                return alowedStyle;
            }

            return unclickedStyle;
        }

        GUIStyle GetAttackStyle()
        {
            if (SelectionManager.active.unitControlMode == 2)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                int targetIndex = SelectionManager.active.CheckForClickTarget(ray);

                if (targetIndex == -1)
                {
                    return notAllowedStyle;
                }

                return alowedStyle;
            }

            return unclickedStyle;
        }
    }
}
