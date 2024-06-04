using UnityEngine;
using System.Collections.Generic;

namespace RTSToolkitFree
{
    public class SelectionMarks : MonoBehaviour
    {
        public static SelectionMarks active;

        [HideInInspector] public List<Texture2D> texture = new List<Texture2D>();
        [HideInInspector] public List<Texture2D> texture2 = new List<Texture2D>();
        [HideInInspector] public List<Texture2D> texture3 = new List<Texture2D>();
        [HideInInspector] public List<Texture2D> healthBar = new List<Texture2D>();
        float scale;

        float screenHeight;

        bool unlocked = false;

        int colorsCount = 31;

        int healthBarLevels = 60;


        public bool useSelectionMarks = false;
        public bool useHealthBars = false;


        void Awake()
        {
            active = this;
        }

        void Start()
        {

            bool clearSel = false;
            bool clearHealth = false;
            for (int i = 0; i < texture.Count; i++)
            {
                if (texture[i] == null)
                {
                    clearSel = true;
                }
            }
            for (int i = 0; i < healthBar.Count; i++)
            {
                if (healthBar[i] == null)
                {
                    clearHealth = true;
                }
            }

            if (clearSel)
            {
                texture.Clear();
            }
            if (clearHealth)
            {
                healthBar.Clear();
            }

            if ((texture.Count < colorsCount) || (texture2.Count < colorsCount) || (texture3.Count < colorsCount))
            {
                BuildSelectionMarkTextures();
            }
            if (healthBar.Count < colorsCount)
            {
                BuildHealthBarTextures();
            }

            screenHeight = Screen.height;
            unlocked = true;

        }

        public void BuildSelectionMarkTextures()
        {
            texture.Clear();
            texture2.Clear();
            texture3.Clear();

            for (int i = 0; i < colorsCount; i++)
            {
                float j = 0f;
                if (i < 0.5f * (colorsCount - 1))
                {
                    j = 2.0f * i / (colorsCount - 1);
                    texture.Add(SetTextures(new Color(1f, j, 0f, 1f), 0));
                }
                else if (i == 0.5f * (colorsCount - 1))
                {
                    texture.Add(SetTextures(new Color(1f, 1f, 0f, 1f), 0));
                }
                else
                {
                    j = 2.0f * ((colorsCount - 1) - i) / (colorsCount - 1);
                    texture.Add(SetTextures(new Color(j, 1f, 0f, 1f), 0));
                }
            }

            for (int i = 0; i < colorsCount; i++)
            {
                float j = 0f;
                if (i < 0.5f * (colorsCount - 1))
                {
                    j = 2.0f * i / (colorsCount - 1);
                    texture2.Add(SetTextures(new Color(1f, j, 0f, 1f), 1));
                }
                else if (i == 0.5f * (colorsCount - 1))
                {
                    texture2.Add(SetTextures(new Color(1f, 1f, 0f, 1f), 1));
                }
                else
                {
                    j = 2.0f * ((colorsCount - 1) - i) / (colorsCount - 1);
                    texture2.Add(SetTextures(new Color(j, 1f, 0f, 1f), 1));
                }
            }

            for (int i = 0; i < colorsCount; i++)
            {
                float j = 0f;
                if (i < 0.5f * (colorsCount - 1))
                {
                    j = 2.0f * i / (colorsCount - 1);
                    texture3.Add(SetTextures(new Color(1f, j, 0f, 1f), 2));
                }
                else if (i == 0.5f * (colorsCount - 1))
                {
                    texture3.Add(SetTextures(new Color(1f, 1f, 0f, 1f), 2));
                }
                else
                {
                    j = 2.0f * ((colorsCount - 1) - i) / (colorsCount - 1);
                    texture3.Add(SetTextures(new Color(j, 1f, 0f, 1f), 2));
                }
            }
            int iii1 = 0;
            for (int i = 0; i < texture.Count; i++)
            {
                if (texture[i] == null)
                {
                    iii1++;
                }
            }

        }

        public void BuildHealthBarTextures()
        {
            healthBar.Clear();
            for (int i = 0; i < healthBarLevels; i++)
            {
                healthBar.Add(healthBarSetup(i, healthBarLevels));
            }
        }

        Texture2D healthBarSetup(int step, int maxStep)
        {
            int res = 128;
            Color color = Color.green;

            Texture2D textureLoc = new Texture2D(res, res, TextureFormat.ARGB32, false);

            for (int i = 0; i < res; i++)
            {
                for (int j = 0; j < res; j++)
                {
                    textureLoc.SetPixel(i, j, Color.clear);
                }
            }

            for (int i = 0; i < res; i++)
            {
                for (int j = (int)(0.9f * res); j < res; j++)
                {
                    if (1.0f * i / res < 1.0f * step / (maxStep - 1))
                    {
                        color = Color.green;
                    }
                    else
                    {
                        color = Color.red;
                    }
                    textureLoc.SetPixel(i, j, color);
                }
            }

            textureLoc.wrapMode = TextureWrapMode.Clamp;

            textureLoc.Apply();
            return textureLoc;

        }

        Texture2D SetTextures(Color color, int mode)
        {
            int res = 128;

            Texture2D textureLoc = new Texture2D(res, res, TextureFormat.ARGB32, false);
            for (int i = 0; i < res; i++)
            {
                for (int j = 0; j < res; j++)
                {
                    textureLoc.SetPixel(i, j, Color.clear);
                }
            }

            for (int i = 0; i < 0.2f * res; i++)
            {
                for (int j = 0; j < 0.06f * res; j++)
                {

                    if (mode != 2)
                    {
                        textureLoc.SetPixel(i, j, color);
                    }
                    else
                    {
                        textureLoc.SetPixel(i, (int)(j + 0.14f * res), color);
                    }
                }
            }

            for (int i = res; i > res * (1f - 0.2f); i--)
            {
                for (int j = 0; j < 0.06f * res; j++)
                {
                    if (mode != 2)
                    {
                        textureLoc.SetPixel(i, j, color);
                    }
                    else
                    {
                        textureLoc.SetPixel(i, (int)(j + 0.14f * res), color);
                    }
                }
            }



            for (int i = 0; i < 0.2f * res; i++)
            {
                for (int j = res; j > (1f - 0.06f) * res; j--)
                {
                    if (mode != 2)
                    {
                        textureLoc.SetPixel(i, j, color);
                    }
                    else
                    {
                        textureLoc.SetPixel(i, (int)(j - 0.14f * res), color);
                    }
                }
            }

            for (int i = res; i > res * (1f - 0.2f); i--)
            {
                for (int j = res; j > (1f - 0.06f) * res; j--)
                {
                    if (mode != 2)
                    {
                        textureLoc.SetPixel(i, j, color);
                    }
                    else
                    {
                        textureLoc.SetPixel(i, (int)(j - 0.14f * res), color);
                    }
                }
            }




            for (int i = 0; i < 0.2f * res; i++)
            {
                for (int j = 0; j < 0.06f * res; j++)
                {
                    if (mode != 2)
                    {
                        textureLoc.SetPixel(j, i, color);
                    }
                    else
                    {
                        textureLoc.SetPixel((int)(j + 0.14f * res), i, color);
                    }
                }
            }

            for (int i = res; i > res * (1f - 0.2f); i--)
            {
                for (int j = 0; j < 0.06f * res; j++)
                {
                    if (mode != 2)
                    {
                        textureLoc.SetPixel(j, i, color);
                    }
                    else
                    {
                        textureLoc.SetPixel((int)(j + 0.14f * res), i, color);
                    }
                }
            }



            for (int i = 0; i < 0.2f * res; i++)
            {
                for (int j = res; j > (1f - 0.06f) * res; j--)
                {
                    if (mode != 2)
                    {
                        textureLoc.SetPixel(j, i, color);
                    }
                    else
                    {
                        textureLoc.SetPixel((int)(j - 0.14f * res), i, color);
                    }
                }
            }

            for (int i = res; i > res * (1f - 0.2f); i--)
            {
                for (int j = res; j > (1f - 0.06f) * res; j--)
                {
                    if (mode != 2)
                    {
                        textureLoc.SetPixel(j, i, color);
                    }
                    else
                    {
                        textureLoc.SetPixel((int)(j - 0.14f * res), i, color);
                    }
                }
            }


            if (mode == 1)
            {

                for (int i = 0; i < res; i++)
                {
                    for (int j = 0; j < 0.06f * res; j++)
                    {
                        textureLoc.SetPixel(j, i, color);
                    }
                }
                for (int i = 0; i < res; i++)
                {
                    for (int j = res; j > (1f - 0.06f) * res; j--)
                    {
                        textureLoc.SetPixel(j, i, color);
                    }
                }
            }

            textureLoc.wrapMode = TextureWrapMode.Clamp;

            textureLoc.Apply();




            return textureLoc;

        }


        void SaveTexture(Texture2D tex, string tname)
        {
#if !UNITY_WEBPLAYER
            byte[] bytes = tex.EncodeToPNG();
            System.IO.File.WriteAllBytes(Application.dataPath + "/../" + tname + ".png", bytes);
#endif
        }


        void OnGUI()
        {
            if (unlocked == true)
            {
                screenHeight = Screen.height;
                int index = 0;
                int indexHb = 0;
                Camera camera = Camera.main;
                Vector3 screenPos;
                Vector3 forw = camera.transform.forward;
                Vector3 camPos = camera.transform.position;

                for (int i = 0; i < BattleSystem.active.allUnits.Count; i++)
                {
                    UnitPars up = BattleSystem.active.allUnits[i];
                    ManualControl manualControl = up.GetComponent<ManualControl>();

                    if (manualControl != null && manualControl.isSelected)
                    {
                        MeshRenderer mr = up.GetComponent<MeshRenderer>();

                        Vector3 _unitCenter = Vector3.zero;
                        float _unitSize = mr.bounds.extents.magnitude;

                        screenPos = camera.WorldToScreenPoint(up.transform.position + _unitCenter);
                        Vector3 heading = up.transform.position + _unitCenter - camPos;

                        float unitSize = _unitSize;

                        if (Vector3.Dot(forw, heading) > 0)
                        {
                            if (screenPos.z < 52 * _unitSize)
                            {
                                scale = 1000f * unitSize / screenPos.z;
                            }
                            else
                            {
                                scale = 1000f * unitSize / (52f * unitSize);
                            }

                            if (up.health < 0)
                            {
                                index = 0;
                                indexHb = 0;

                            }
                            else if (up.health > up.maxHealth)
                            {
                                index = colorsCount - 1;
                                indexHb = healthBarLevels - 1;
                            }
                            else
                            {
                                index = (int)((up.health / up.maxHealth) * (colorsCount - 1));
                                indexHb = (int)((up.health / up.maxHealth) * (healthBarLevels - 1));
                            }

                            if (useSelectionMarks)
                            {
                                if (up.nation == Diplomacy.active.playerNation)
                                {
                                    GUI.DrawTexture(new Rect(screenPos.x - 0.5f * scale, screenHeight - (screenPos.y + 0.5f * scale), scale, scale), texture[index]);
                                }
                                else
                                {
                                    if (Diplomacy.active.relations[Diplomacy.active.playerNation][up.nation] == 1)
                                    {
                                        GUI.DrawTexture(new Rect(screenPos.x - 0.5f * scale, screenHeight - (screenPos.y + 0.5f * scale), scale, scale), texture3[index]);
                                    }
                                    else
                                    {
                                        GUI.DrawTexture(new Rect(screenPos.x - 0.5f * scale, screenHeight - (screenPos.y + 0.5f * scale), scale, scale), texture2[index]);
                                    }
                                }
                            }
                            if (useHealthBars)
                            {
                                GUI.DrawTexture(new Rect(screenPos.x - 0.5f * scale, screenHeight - (screenPos.y + 0.7f * scale), scale, scale), healthBar[indexHb]);
                            }
                        }
                    }
                }
            }
        }
    }
}
