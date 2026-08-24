using System;
using System.Reflection;

using TeamCherry.SharedUtils;

namespace BonfireSilksong
{
    public static class WisdomLogic
    {
        public static int WisdomMilestone(int m)
        {
            switch (m)
            {
                case 1: return 1;
                case 2: return 2;
                case 3: return 3;
                case 4: return 3;
                default: return 999;
            }
        }

        public static bool CheckWisdomMilestone(int m)
        {
            var s = Bonfire.Instance.Status.WisdomStat;
            int b = LevelingSystem.MilestoneBonus;
            return s + b >= WisdomMilestone(m) && s >= 1;
        }

        // Stat scaling: extra silk gained when striking an enemy with needle (+1 silk for every 20 levels)
        public static int ExtraSilk(int totalWsdm, int baseSilk)
        {
            return (int)(baseSilk + Math.Floor(totalWsdm / 20f));
        }

        // Stat scaling: amount of time it takes to passively produce a single silk.
        // starts from 40 seconds timer at lvl 5 to prevents players from putting just a single point and
        // reaping the benefits
        public static int SilkRegenInterval(int totalWsdm)
        {
            if (totalWsdm < 5) return -1;

            return (int)(4.0 + 36.0 * Math.Exp(-0.06 * (totalWsdm - 5)));
        }

        /// <summary>
        /// HeroController.SilkGain (Postfix): Add extra silk on needle hits.
        /// </summary>
        public static void AddExtraSilkOnNeedleHit(HitInstance hitInstance)
        {
            if (hitInstance.SilkGeneration == HitSilkGeneration.None) return;
            if (hitInstance.SilkGeneration == HitSilkGeneration.FirstHit && !hitInstance.IsFirstHit) return;
            if (PlayerData.instance.atBench) return;

            int baseSilk = 1;
            int extraSilk = ExtraSilk(Bonfire.Instance.Status.WisdomStat, baseSilk) - baseSilk;
            if (extraSilk > 0)
                HeroController.instance.AddSilk(extraSilk, heroEffect: false);
        }

        /// <summary>
        /// HeroController.Update (Postfix): Passive silk regen.
        /// </summary>
        public static void CheckSilkRegen(float dt)
        {
            if (_silkRegenTime > 0)
            {
                _silkRegenTime -= dt;
            }
            else
            {
                Bonfire.Log.LogDebug("[WisdomLogic] Checking silk regen conditions");
                int silkRegenInterval = SilkRegenInterval(Bonfire.Instance.Status.WisdomStat);
                if (silkRegenInterval <= 0)
                {
                    _silkRegenTime = 1f;
                    return;
                }

                _silkRegenTime = silkRegenInterval;
                HeroController.instance.AddSilk(1, heroEffect: false);

                Bonfire.Log.LogDebug("Regenerated 1 silk");
            }
        }

        /// <summary>
        /// HeroWispLantern.SpawnWispsRoutine (Prefix): Checks if player has 0 silk and milestone requirement: if yes,
        /// add one silk to spawn a wisp.
        /// </summary>
        public static void WispLanternSilkOnSpawn()
        {
            var hc = HeroController.instance;

            if (CheckWisdomMilestone(1) && hc.playerData.silk <= 0)
                hc.AddSilk(1, heroEffect: false);
        }

        /// <summary>
        /// HeroWispLantern.OnSceneLoaded (Prefix): Upgrade Wispfire Lantern by lowering attack delay and cooldown.
        /// </summary>
        public static void WispLanternBuffSpeed(HeroWispLantern instance)
        {
            MinMaxFloat mmfDelay = (MinMaxFloat)WispSpawnStartDelay?.GetValue(instance);
            MinMaxFloat mmfCd = (MinMaxFloat)WispSpawnCooldown?.GetValue(instance);

            Bonfire.Log.LogDebug("[WisdomLogic] Wispfire Lantern stats:");
            Bonfire.Log.LogDebug("- Delay start: " + mmfDelay.Start);
            Bonfire.Log.LogDebug("- Delay End " + mmfDelay.End);
            Bonfire.Log.LogDebug("- Cd start: " + mmfCd.Start);
            Bonfire.Log.LogDebug("- Cd End: " + mmfCd.End);

            // start & end use same value: delay default is 0.3f, cooldown default is 4f (4 seconds)
            float delayNew = 0.3f;
            float cdNew = 4f;

            if (CheckWisdomMilestone(2))
            {
                delayNew = 0.1f;
                cdNew = 1f;
            }
            else if (CheckWisdomMilestone(1))
            {
                delayNew = 0.15f;
                cdNew = 2f;
            }

            mmfDelay.Start = mmfDelay.End = delayNew;
            mmfCd.Start = mmfCd.End = cdNew;

            WispSpawnStartDelay.SetValue(instance, mmfDelay);
            WispSpawnCooldown.SetValue(instance, mmfCd);
        }

        /// <summary>
        /// HeroController.TakeSilk (Prefix): If Wispfire Lantern silk cost should be ignored.
        /// </summary>
        public static bool CheckWispLanternCostRefund(SilkSpool.SilkTakeSource source)
        {
            if (source == SilkSpool.SilkTakeSource.Wisp && CheckWisdomMilestone(1))
                return true;

            return false;
        }


        private static float _silkRegenTime = 0f;

        private static readonly FieldInfo WispSpawnStartDelay = typeof(HeroWispLantern)
            .GetField("wispSpawnStartDelay", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo WispSpawnCooldown = typeof(HeroWispLantern)
            .GetField("wispSpawnCooldown", BindingFlags.Instance | BindingFlags.NonPublic);
    }
}