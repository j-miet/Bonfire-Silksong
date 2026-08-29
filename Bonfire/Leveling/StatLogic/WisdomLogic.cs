using System;
using System.Reflection;

using TeamCherry.SharedUtils;
using UnityEngine;

namespace BonfireSilksong
{
    public static class WisdomLogic
    {
        public static bool BlockSummons { get; private set; } = false;
        public static bool CanBlockSummons => CheckWisdomMilestone(1) && PlayerData.instance.hasNeedolin;
        public static bool IsSilkNailsEnabled
        {
            get
            {
                if (BlockSummons) return false;

                return Bonfire.Instance.Status.SilkNailsEnabled && CheckWisdomMilestone(1);
            }
        }
        public static bool IsCogflyWarriorEnabled
        {
            get
            {
                if (BlockSummons) return false;

                return Bonfire.Instance.Status.CogflyWarriorEnabled && CheckWisdomMilestone(2);
            }
        }

        public static int WisdomMilestone(int m)
        {
            switch (m)
            {
                case 1: return 1;
                case 2: return 2;
                case 3: return 3;
                case 4: return 4;
                default: return 999;
            }
        }

        public static bool CheckWisdomMilestone(int m)
        {
            var s = Bonfire.Instance.Status.WisdomStat;
            int b = LevelingSystem.MilestoneBonus;
            return s + b >= WisdomMilestone(m) && s >= 1;
        }

        /// <summary>
        /// Extra silk gained when striking an enemy with needle (+1 silk for every 20 levels).
        /// </summary>
        public static int ExtraSilk(int totalWsdm, int baseSilk)
        {
            if (totalWsdm < ExtraSilkLevelThreshold) return baseSilk;

            return (int)(baseSilk + Math.Floor(totalWsdm / ExtraSilkLevelThreshold));
        }

        /// <summary>
        /// Amount of time it takes to passively produce a single silk.
        /// Starts from 40 seconds timer at lvl 5 to prevents players from putting just a single point and reaping the 
        /// benefits.
        /// </summary>
        public static int SilkRegenInterval(int totalWsdm)
        {
            if (totalWsdm < SilkRegenUnlockLevel) return -1;

            return (int)(4.0 + 36.0 * Math.Exp(-0.06 * (totalWsdm - SilkRegenUnlockLevel)));
        }

        public static int SilkNailsDamage(int totalWsdm)
        {
            int damage = SilkNailsBaseDamage;

            if (totalWsdm <= 1) return damage;

            if (totalWsdm < WisdomMilestone(4))
            {
                damage += (int)Math.Floor(totalWsdm * 0.5f);
            }
            else
            {
                damage += 5 + totalWsdm - 10;
            }

            return damage;
        }

        public static float SilkNailsSpawnCooldown(int totalWsdm)
        {
            if (totalWsdm <= 1) return 5f;

            return (float)(0.75 + 4.25 * Math.Exp(-0.09 * totalWsdm));
        }

        public static int CogflyWarriorDamage(int totalWsdm)
        {
            int damage = CogflyWarriorBaseDamage;

            if (totalWsdm <= 1) return damage;

            if (totalWsdm <= WisdomMilestone(2))
            {
                damage += (int)Math.Floor(totalWsdm * 0.25f);
            }
            else
            {
                damage += 3 + (int)Math.Floor(totalWsdm * 0.5f);
            }

            return damage;
        }

        public static float CogflyWarriorAttackCooldown(int totalWsdm)
        {
            if (totalWsdm <= 1) return 1.5f;

            // game's default cooldown logic samples values from range [0.5, 1.0). 
            // This instead sets range to (0.05, 1.5], but simply decreases the value with more points in Wisdom.
            return (float)(0.05 + 1.45 * Math.Exp(-0.05 * totalWsdm));
        }

        /// <summary>
        /// Updates spawn limit of Cogfly Warriors. Should call this frequently e.g. after scene transitions, and
        /// also after leveling up/respeccing.
        /// </summary>
        public static void UpdateCogflyWarriorSpawnLimit()
        {
            if (CheckWisdomMilestone(4))
                CogflySpawner.SetCogflySpawnLimit(2);
            else
                CogflySpawner.SetCogflySpawnLimit(1);
        }

        /// <summary>
        /// Get Wispfire Lantern custom attack delay and cooldown values.
        /// </summary>
        public static (float attackCd, float spawnCd) WispLanternSpeed()
        {
            // start & end use same value: delay default is 0.3f, cooldown default is 4f (4 seconds)
            float attackNew = 0.3f;
            float spawnNew = 4f;

            if (CheckWisdomMilestone(4))
            {
                attackNew = 0.1f;
                spawnNew = 1f;
            }
            else if (CheckWisdomMilestone(3))
            {
                attackNew = 0.15f;
                spawnNew = 2f;
            }

            return (attackNew, spawnNew);
        }

        /// <summary>
        /// HeroController.SilkGain (Postfix): Add extra silk on needle hits.
        /// </summary>
        public static void AddExtraSilkOnNeedleHit(HitInstance hitInstance)
        {
            if (hitInstance.SilkGeneration == HitSilkGeneration.None) return;
            if (hitInstance.SilkGeneration == HitSilkGeneration.FirstHit && !hitInstance.IsFirstHit) return;
            if (PlayerData.instance.atBench) return;

            int extraSilk = ExtraSilk(Bonfire.Instance.Status.WisdomStat, 0);
            if (extraSilk > 0)
                HeroController.instance.AddSilk(extraSilk, heroEffect: false);
        }

        /// <summary>
        /// HeroController.Update (Postfix): Passive silk regen.
        /// </summary>
        public static void CheckSilkRegen(float dt)
        {
            if (Bonfire.Instance.Status.WisdomStat < SilkRegenUnlockLevel) return;

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
        /// HeroController.Update (Postfix): Silk Nails custom ability spawning.
        /// </summary>
        public static void CheckSilkNails(float dt)
        {
            if (!IsSilkNailsEnabled) return;

            if (_silkNailCooldown > 0)
            {
                _silkNailCooldown -= dt;
            }
            else
            {
                if (SpawnSilkNails())
                {
                    Bonfire.Log.LogInfo("[WisdomLogic] Checking Silk Nails cooldown.");

                    float silkNailsCD = SilkNailsSpawnCooldown(Bonfire.Instance.Status.WisdomStat);
                    if (silkNailsCD <= 0)
                    {
                        _silkNailCooldown = 1f;
                        return;
                    }

                    _silkNailCooldown = silkNailsCD;
                    Bonfire.Log.LogInfo("Spawned Silk Nails");

                    return;
                }

                // small pause to avoid constant SpawnSilkNails check. Probably doesn't have any practical impact
                // because the distance calculation inside SpawnSilkNails is already quite inexpensive to do.
                _silkNailCooldown = 0.05f;

                return;
            }
        }

        /// <summary>
        /// HeroController.Update (Postfix): Check if Cogfly Warrior(s) need to be resummoned or despawned.
        /// </summary>
        public static void CheckCogflyWarriorState()
        {
            if (IsCogflyWarriorEnabled)
            {
                CogflySpawner.SpawnCogflies();
            }
            else
            {
                CogflySpawner.DespawnAllCogflies();
            }
        }

        /// <summary>
        /// ClockworkHatchling.HitLanded (Prefix): Update Cogfly hp to custom amount before hit logic. This way they
        /// can be made permanent/unbreakable.
        /// </summary>
        public static void UpdateCogflyCurrentHP(ClockworkHatchling instance)
        {
            _hpCurrentField.SetValue(instance, 100);
        }

        /// <summary>
        /// ClockworkHatchling.StartAttackCooldown (Postfix): Override Cogfly attack cooldown.
        /// </summary>
        public static void UpdateCogflyAttackCooldown(ClockworkHatchling instance)
        {
            _cooldownTimer.SetValue(instance, CogflyWarriorAttackCooldown(Bonfire.Instance.Status.WisdomStat));
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
        public static void ApplyWispLanternSpeedBuff(HeroWispLantern instance)
        {
            MinMaxFloat mmfDelay = (MinMaxFloat)_wispSpawnStartDelay?.GetValue(instance);
            MinMaxFloat mmfSpawnCd = (MinMaxFloat)_wispSpawnCooldown?.GetValue(instance);

            Bonfire.Log.LogDebug("[WisdomLogic] Wispfire Lantern stats:");
            Bonfire.Log.LogDebug("- Delay start: " + mmfDelay.Start);
            Bonfire.Log.LogDebug("- Delay End " + mmfDelay.End);
            Bonfire.Log.LogDebug("- Cd start: " + mmfSpawnCd.Start);
            Bonfire.Log.LogDebug("- Cd End: " + mmfSpawnCd.End);

            var (delay, cd) = WispLanternSpeed();

            mmfDelay.Start = mmfDelay.End = delay;
            mmfSpawnCd.Start = mmfSpawnCd.End = cd;

            _wispSpawnStartDelay.SetValue(instance, mmfDelay);
            _wispSpawnCooldown.SetValue(instance, mmfSpawnCd);
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

        /// <summary>
        /// Reverses BlockSummons boolean value and plays the Magnetite Dice prefab effect as a visual indicator.
        /// </summary>
        public static bool ToggleSummonsActivity()
        {
            if (!CanBlockSummons) return false;

            if (BlockSummons)
            {
                BlockSummons = false;
                Bonfire.Log.LogDebug("[WisdomLogic] Summons are blocked.");
            }
            else
            {
                BlockSummons = true;
                Bonfire.Log.LogDebug("[WisdomLogic] Summons are active.");
            }

            HeroController.instance.GetComponent<SpriteFlash>().flashFocusHeal();

            return true;
        }


        private const int SilkRegenUnlockLevel = 5;
        private const float ExtraSilkLevelThreshold = 20f;
        private const int SilkNailsBaseDamage = 1;
        private const float SilkNailsActivationRange = 12f;
        private const int CogflyWarriorBaseDamage = 2;

        private static float _silkRegenTime = 0f;
        private static float _silkNailCooldown = 0f;

        private static readonly FieldInfo _hpCurrentField = typeof(ClockworkHatchling)
            .GetField("hpCurrent", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _cooldownTimer = typeof(ClockworkHatchling)
            .GetField("cooldownTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _wispSpawnStartDelay = typeof(HeroWispLantern)
            .GetField("wispSpawnStartDelay", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _wispSpawnCooldown = typeof(HeroWispLantern)
            .GetField("wispSpawnCooldown", BindingFlags.Instance | BindingFlags.NonPublic);

        private static bool SpawnSilkNails()
        {
            HeroController hc = HeroController.instance;

            foreach (var e in EnemyHealth.EnemyMaxHp.Keys)
            {
                if (e == null)
                    continue;

                float dist = Vector2.Distance(hc.transform.position, e.transform.position);
                if (dist > SilkNailsActivationRange)
                    continue;

                PaleNailsSpawner.SpawnNeedles();

                return true;
            }

            return false;
        }
    }
}