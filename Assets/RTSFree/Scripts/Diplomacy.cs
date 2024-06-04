using System.Collections.Generic;
using UnityEngine;

namespace RTSToolkitFree
{
    public class Diplomacy : MonoBehaviour
    {
        public static Diplomacy active;

        [HideInInspector] public int numberNations;
        public List<List<int>> relations = new List<List<int>>();
        public int playerNation = 0;

        public int defaultRelation = 1;

        void Awake()
        {
            active = this;
        }

        void Start()
        {
            SetAllWar();
        }

        public void AddNation()
        {
            for(int i=0; i<numberNations; i++)
            {
                relations[i].Add(defaultRelation);
            }

            relations.Add(new List<int>());

            for(int i=0; i<numberNations+1; i++)
            {
                relations[numberNations].Add(defaultRelation);
            }

            numberNations++;

            BattleSystem.active.AddNation();
        }

        public void SetAllPeace()
        {
            for (int i = 0; i < numberNations; i++)
            {
                for (int j = 0; j < numberNations; j++)
                {
                    if (i != j)
                    {
                        relations[i][j] = 0;
                    }
                }
            }

            Debug.Log("peace");
        }

        public void SetAllWar()
        {
            for (int i = 0; i < numberNations; i++)
            {
                for (int j = 0; j < numberNations; j++)
                {
                    if (i != j)
                    {
                        relations[i][j] = 1;
                    }
                }
            }

            // Debug.Log("war");
        }
    }
}
