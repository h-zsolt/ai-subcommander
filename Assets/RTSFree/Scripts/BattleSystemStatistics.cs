using UnityEngine;

namespace RTSToolkitFree
{
    public static class BattleSystemStatistics
    {
        static float[] timesStatistics = new float[10];
        static float[] lowFrequencyTimeStatistics = new float[10];
        static int[] countStatistics = new int[9];

        static float tUpdateWithStatistics;

        public static void UpdateWithStatistics(BattleSystem battleSystem, float deltaTime)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            battleSystem.SearchPhase();
            float tSearchPhase = (float)sw.Elapsed.TotalMilliseconds;

            battleSystem.RetargetPhase();
            float tRetargetPhase = (float)sw.Elapsed.TotalMilliseconds - tSearchPhase;

            battleSystem.ApproachPhase();
            float tApproachPhase = (float)sw.Elapsed.TotalMilliseconds - tRetargetPhase;

            battleSystem.AttackPhase();
            float tAttackPhase = (float)sw.Elapsed.TotalMilliseconds - tApproachPhase;

            battleSystem.SelfHealingPhase(deltaTime);
            float tSelfHealingPhase = (float)sw.Elapsed.TotalMilliseconds - tAttackPhase;

            battleSystem.DeathPhase();
            float tDeathPhase = (float)sw.Elapsed.TotalMilliseconds - tSelfHealingPhase;

            battleSystem.SinkPhase(deltaTime);
            float tSinkPhase = (float)sw.Elapsed.TotalMilliseconds - tDeathPhase;

            battleSystem.ManualMover();
            float tManualMover = (float)sw.Elapsed.TotalMilliseconds - tSinkPhase;

            float tTotal = tSearchPhase + tRetargetPhase + tApproachPhase + tAttackPhase + tSelfHealingPhase + tDeathPhase + tSinkPhase + tManualMover;

            timesStatistics[0] = 0.9f * timesStatistics[0] + 0.1f * tTotal;
            timesStatistics[1] = 0.9f * timesStatistics[1] + 0.1f * tSearchPhase;
            timesStatistics[2] = 0.9f * timesStatistics[2] + 0.1f * tRetargetPhase;
            timesStatistics[3] = 0.9f * timesStatistics[3] + 0.1f * tApproachPhase;
            timesStatistics[4] = 0.9f * timesStatistics[4] + 0.1f * tAttackPhase;
            timesStatistics[5] = 0.9f * timesStatistics[5] + 0.1f * tSelfHealingPhase;
            timesStatistics[6] = 0.9f * timesStatistics[6] + 0.1f * tDeathPhase;
            timesStatistics[7] = 0.9f * timesStatistics[7] + 0.1f * tSinkPhase;
            timesStatistics[8] = 0.9f * timesStatistics[8] + 0.1f * tManualMover;

            timesStatistics[9] = 0.9f * timesStatistics[9] + 0.1f * deltaTime * 1000f;

            tUpdateWithStatistics -= deltaTime;

            if (tUpdateWithStatistics < 0f)
            {
                tUpdateWithStatistics = 1f;

                for (int i = 0; i < timesStatistics.Length; i++)
                {
                    lowFrequencyTimeStatistics[i] = timesStatistics[i];
                }

                CalculateCountStatistics(battleSystem);
            }
        }

        static void CalculateCountStatistics(BattleSystem battleSystem)
        {
            for (int i = 0; i < countStatistics.Length; i++)
            {
                countStatistics[i] = 0;
            }

            for (int i = 0; i < battleSystem.allUnits.Count; i++)
            {
                UnitPars up = battleSystem.allUnits[i];

                countStatistics[0]++;

                if (up.isReady)
                {
                    countStatistics[1]++;
                }

                if (up.isApproaching)
                {
                    countStatistics[2]++;
                    countStatistics[3]++;
                }

                if (up.isAttacking)
                {
                    countStatistics[4]++;
                }

                if (up.health < up.maxHealth && up.health > 0f)
                {
                    countStatistics[5]++;
                }

                if (up.isDying)
                {
                    countStatistics[6]++;
                }

                ManualControl manualControl = up.GetComponent<ManualControl>();

                if (manualControl.isMoving || manualControl.prepareMoving)
                {
                    countStatistics[8]++;
                }
            }

            countStatistics[7] = battleSystem.sinks.Count;
        }

        // Display performance UI
        public static void OnGUI()
        {
            GUI.Label(GUIRect(0.05f),
                $"BattleSystem: n={countStatistics[0]} t={lowFrequencyTimeStatistics[0]}ms dt={lowFrequencyTimeStatistics[9]} f={lowFrequencyTimeStatistics[0] * 100f / lowFrequencyTimeStatistics[9]}%"
            );

            GUI.Label(GUIRect(0.15f), $"Search: n={countStatistics[1]} t={lowFrequencyTimeStatistics[1]}ms f={lowFrequencyTimeStatistics[1] * 100f / lowFrequencyTimeStatistics[0]}%");
            GUI.Label(GUIRect(0.2f), $"Retarget: n={countStatistics[2]} t={lowFrequencyTimeStatistics[2]}ms f={lowFrequencyTimeStatistics[2] * 100f / lowFrequencyTimeStatistics[0]}%");
            GUI.Label(GUIRect(0.25f), $"Approach: n={countStatistics[3]} t={lowFrequencyTimeStatistics[3]}ms f={lowFrequencyTimeStatistics[3] * 100f / lowFrequencyTimeStatistics[0]}%");
            GUI.Label(GUIRect(0.3f), $"Attack: n={countStatistics[4]} t={lowFrequencyTimeStatistics[4]}ms f={lowFrequencyTimeStatistics[4] * 100f / lowFrequencyTimeStatistics[0]}%");
            GUI.Label(GUIRect(0.35f), $"SelfHealing: n={countStatistics[5]} t={lowFrequencyTimeStatistics[5]}ms f={lowFrequencyTimeStatistics[5] * 100f / lowFrequencyTimeStatistics[0]}%");
            GUI.Label(GUIRect(0.4f), $"Death: n={countStatistics[6]} t={lowFrequencyTimeStatistics[6]}ms f={lowFrequencyTimeStatistics[6] * 100f / lowFrequencyTimeStatistics[0]}%");
            GUI.Label(GUIRect(0.45f), $"Sink: n={countStatistics[7]} t={lowFrequencyTimeStatistics[7]}ms f={lowFrequencyTimeStatistics[7] * 100f / lowFrequencyTimeStatistics[0]}%");
            GUI.Label(GUIRect(0.5f), $"ManualMover: n={countStatistics[8]} t={lowFrequencyTimeStatistics[8]}ms f={lowFrequencyTimeStatistics[8] * 100f / lowFrequencyTimeStatistics[0]}%");
        }

        static Rect GUIRect(float height)
        {
            return new Rect(Screen.width * 0.05f, Screen.height * height, 500f, 20f);
        }
    }
}
