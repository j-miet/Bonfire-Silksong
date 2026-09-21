using System;
using System.Collections;

using GlobalEnums;
using UnityEngine;

using BonfireSilksong.GUIComponents;

namespace BonfireSilksong.Leveling
{
    public enum BlueState
    {
        DISABLED,
        REFLECT,
        GLASSCANNON,
    }

    public static class ResilienceStat
    {
        public static bool BlockNextHit = false;

        public static bool BlueMaskStateUnlocked => CheckResilienceMilestone(1);
        public static bool GlassCannonButNotPlasmified =>
            Bonfire.Instance.Status.BlueMaskState == (int)BlueState.GLASSCANNON &&
            !IsInBlueStateAndPlasmified(BlueState.GLASSCANNON);

        public static int ResilienceMilestone(int m)
        {
            switch (m)
            {
                case 1: return 1;
                case 2: return 20;
                case 3: return 35;
                default: return LevelingSystem.MaxStatLevel;
            }
        }

        public static bool CheckResilienceMilestone(int m)
        {
            var s = Bonfire.Instance.Status.ResilienceStat;
            int b = LevelingSystem.MilestoneBonus;
            return s + b >= ResilienceMilestone(m) && s >= 1;
        }

        /// <summary>
        /// Extra lifeblood masks. Capped at 8 because at 9 the game transitions into Plasmified state.
        /// </summary>
        public static int BlueMasks(int totalRes)
        {
            if (totalRes <= 0) return 0;
            else if (totalRes < 3) return 1;
            else if (totalRes < 5) return 2;
            else if (totalRes < 9) return 3;
            else if (totalRes < 14) return 4;
            else if (totalRes < 20) return 5;
            else if (totalRes < 27) return 6;
            else if (totalRes < 35) return 7;
            else return 8;
        }

        /// <summary>
        /// Chance to block an enemy hit.
        /// </summary>
        public static float BlockChance(int totalRes)
        {
            if (totalRes <= 0) return 0f;

            return (float)(0.72 / (1.0 + 12.0 * Math.Exp(-0.07 * totalRes)));
        }

        /// <summary>
        /// LevelingGUI helper: Returns average damage blocking chance from current Resilience stat value.
        /// </summary>
        public static float AverageBlockChance(int totalRes)
        {
            float f = BlockChance(totalRes);
            float[] multipliers = { 10f, 20f, 30f, 50f, 70f, 80f, 90f };
            float sum = 0f;
            foreach (var m in multipliers)
                sum += m * f;
            return sum / multipliers.Length;
        }

        /// <summary>
        /// Reflect damage.
        /// </summary>
        public static int ReflectDamage(int totalRes, bool isGUI = false, bool GUIPlasmified = false)
        {
            BlueState state = BlueState.REFLECT;
            if (totalRes <= 0 || !IsInBlueState(state))
                return 0;

            int damage = 5 + 3 * BlueMasks(totalRes) + 5 * (PlayerData.instance.maxHealthBase - 5);

            if (IsInBlueStateAndPlasmified(state) || (isGUI && GUIPlasmified))
            {
                damage += 10 * PlayerData.instance.health;
            }

            // skip log printing when GUI calls this
            if (!isGUI)
                Bonfire.Log.LogDebug($"[ResilienceStat] Reflect damage: {damage}");

            return damage;
        }

        /// <summary>
        /// Reflect effect area.
        /// </summary>
        public static float ReflectArea(bool isGUI = false, bool GUIPlasmified = false)
        {
            BlueState state = BlueState.REFLECT;
            if (!IsInBlueState(state))
                return 0;

            float range = ReflectDamageArea;

            if (IsInBlueState(state) && CheckResilienceMilestone(2))
                range += 1f;

            if (IsInBlueStateAndPlasmified(state) || (isGUI && GUIPlasmified))
                range += 3f;

            if (!isGUI)
                Bonfire.Log.LogDebug($"[ResilienceStat] Reflect effect area: {range}");

            return range;
        }

        /// <summary>
        /// Handle reflect damage output while in reflect blue state.
        /// </summary>
        public static void HandleReflectDamageHitLogic(HazardType hazard)
        {
            // reflect damage logic, triggered only outside iframes
            if (IsInBlueState(BlueState.REFLECT) && HeroController.instance.CanTakeDamage())
            {
                bool hitEnemyInSecondaryRange = DealReflectDamage();
                float primaryRange = ReflectArea(isGUI: true);
                float secondaryRange = primaryRange * 2;

                if (Bonfire.Instance.Status.ShowCustomRangeIndicator)
                {
                    if (hitEnemyInSecondaryRange)
                    {
                        Color secondaryColor = new Color(0.6f, 0.6f, 0.8f);
                        RangeIndicator.Display("reflectSecondary",
                            secondaryRange,
                            secondaryColor,
                            secondaryColor);
                    }

                    Color primaryColor = new Color(0.6f, 0.8f, 0.6f);
                    RangeIndicator.Display("reflect",
                        primaryRange,
                        primaryColor,
                        primaryColor);
                }

                if (hazard == HazardType.ENEMY)
                {
                    if (hitEnemyInSecondaryRange)
                        HeroController.instance.AddSilk(2, false);
                    else
                        HeroController.instance.AddSilk(1, false);
                }
            }
        }

        /// <summary>
        /// Amount of blue hp healed with bind while in damage reflect state and not plasmified.
        /// </summary>
        public static bool ReflectStateHealedBlueMasks()
        {
            if (IsInBlueState(BlueState.REFLECT) && !HeroController.instance.IsInLifebloodState)
            {
                int masks = 2;

                if (CheckResilienceMilestone(2))
                    masks += 1;

                int updated = PlayerData.instance.healthBlue + masks;
                int res = BlueMasks(Bonfire.Instance.Status.ResilienceStat);

                if (updated > res)
                    masks -= updated - res;

                Bonfire.Log.LogDebug("[ResilienceStat] Blue hp healed in Reflect state: " + masks);

                GameManager.instance.QueuedBlueHealth = masks;
                EventRegister.SendEvent(EventRegisterEvents.AddQueuedBlueHealth);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Heal custom amount of blue masks with a small delay. Useful if blue hp needs to be added repeatedly, this
        /// avoids FSM event overriding.
        /// </summary>
        public static IEnumerator BlueStateHealBlueMasks(int amount)
        {
            yield return new WaitForSecondsRealtime(0.25f);

            int masks = amount;
            int updated = PlayerData.instance.healthBlue + amount;
            int res = BlueMasks(Bonfire.Instance.Status.ResilienceStat);

            if (updated > res)
                masks -= updated - res;

            Bonfire.Log.LogDebug("[ResilienceStat] Healed blue masks on crit: " + masks);

            GameManager.instance.QueuedBlueHealth = masks;
            EventRegister.SendEvent(EventRegisterEvents.AddQueuedBlueHealth);
        }

        /// <summary>
        /// Apply global damage bonuses while in glass cannon state.
        /// </summary>
        public static int ApplyGlassCannonDamageBonus(int baseDamage)
        {
            int damage = (int)Math.Round(baseDamage * (1.5f + GlassCannonDamageMaskBonus()));

            if (CheckResilienceMilestone(2) || IsInBlueState(BlueState.GLASSCANNON))
            {
                damage += 10;
                Bonfire.Log.LogDebug($"[ResilienceStat] Applied glass cannon flat bonus");
            }

            return damage;
        }

        /// <summary>
        /// Get hp + blue mask damage bonuses in glass cannon state (separated from ApplyGlassCannonDamageBonus for 
        /// GUI utilization).
        /// </summary>
        public static float GlassCannonDamageMaskBonus(bool isGUI = false, bool GUIPlasmified = false)
        {
            BlueState state = BlueState.GLASSCANNON;
            if (!IsInBlueState(state))
                return 0;

            float bonus = 0;

            if (IsInBlueStateAndPlasmified(state) || (isGUI && GUIPlasmified))
            {
                bonus += PlayerData.instance.health * 0.2f + (PlayerData.instance.maxHealthBase - 5) * 0.08f;
            }
            else
            {
                bonus += PlayerData.instance.healthBlue * 0.07f + (PlayerData.instance.maxHealthBase - 5) * 0.08f;
            }

            if (!isGUI)
                Bonfire.Log.LogDebug($"[ResilienceStat] Glass cannon mask bonus: {bonus}");

            return bonus;
        }

        /// <summary>
        /// Get current milestone reduction bonus when in glass cannon state.
        /// </summary>
        public static int GetGlassCannonMilestoneBonus(int totalRes, bool isGUI = false, bool GUIPlasmified = false)
        {
            BlueState s = BlueState.GLASSCANNON;

            if (totalRes <= 0 || !IsInBlueState(s)) return 0;

            int m;

            if (IsInBlueStateAndPlasmified(s) || (isGUI && GUIPlasmified))
            {
                m = 10;
            }
            else if (IsInBlueState(s) && CheckResilienceMilestone(2))
            {
                m = 5;
            }
            else if (IsInBlueState(s))
            {
                m = 3;
            }
            else
            {
                m = 0;
            }

            if (!isGUI)
                Bonfire.Log.LogDebug($"[ResilienceStat] Glass cannon milestone bonus: {m}");

            return m;
        }

        public static bool IsInBlueState(BlueState state)
        {
            return CheckResilienceMilestone(1) && Bonfire.Instance.Status.BlueMaskState == (int)state;
        }

        public static bool IsInAnyBlueState()
        {
            return IsInBlueState(BlueState.REFLECT) || IsInBlueState(BlueState.GLASSCANNON);
        }

        public static bool IsInBlueStateAndPlasmified(BlueState state)
        {
            return HeroController.instance.IsInLifebloodState &&
                CheckResilienceMilestone(3) &&
                Bonfire.Instance.Status.BlueMaskState == (int)state;
        }

        public static bool IsInAnyBlueStateAndPlasmified()
        {
            return HeroController.instance.IsInLifebloodState &&
                CheckResilienceMilestone(3) && IsInAnyBlueState();
        }

        /// <summary>
        /// Update damage taken based on current blue state.
        /// </summary>
        public static int UpdateBlueStateDamageTaken(int amount)
        {
            int damageTaken = amount;
            bool inAnyBlueState = IsInAnyBlueState();

            if (IsInBlueStateAndPlasmified(BlueState.GLASSCANNON))
            {
                damageTaken += 2;
            }
            else if (IsInBlueStateAndPlasmified(BlueState.REFLECT))
            {
                // revert the -1 damage taken below: IsInBlueStateAndPlasmified must be checked first before 
                // IsInBlueState as the latter will always get accepted if the former is true. 
                damageTaken = amount;
            }
            else if (inAnyBlueState && PlayerData.instance.healthBlue <= 0)
            {
                // if player has no blue masks left while in blue state, damage taken is 10x.
                damageTaken *= 10;
            }
            else if (inAnyBlueState)
            {
                damageTaken = Math.Max(1, amount - 1);
            }

            if (inAnyBlueState)
                Bonfire.Log.LogDebug($"[ResilienceStat] Damage taken in blue state: {damageTaken}");

            return damageTaken;
        }

        /// <summary>
        /// HeroController.TakeDamage (Prefix): Check if damage should get blocked via Resilience block functionality 
        /// when an enemy hits the player, and if so, set StatsShared.BlockNextHit = true.
        /// </summary>
        public static void HandleBlockCheck(HeroController instance, HazardType hazardType)
        {
            if (!instance.CanTakeDamage()) return;

            Bonfire.Log.LogDebug("[ResilienceStat] Took damage from hazard type: " + hazardType);

            if (hazardType != HazardType.ENEMY) return; // only block enemy attacks

            var s = Bonfire.Instance.Status;
            if (s.ResilienceStat <= 0) return;

            int hitsSinceShielded = _hitsSinceShielded;
            if (hitsSinceShielded > 7)
                hitsSinceShielded = 7;

            float num = UnityEngine.Random.Range(1, 100);
            float blockChance = BlockChance(s.ResilienceStat);
            float multiplier = 0f;

            switch (hitsSinceShielded)
            {
                case 1: multiplier = 10f; break;
                case 2: multiplier = 20f; break;
                case 3: multiplier = 30f; break;
                case 4: multiplier = 50f; break;
                case 5: multiplier = 70f; break;
                case 6: multiplier = 80f; break;
                case 7: multiplier = 90f; break;
            }

            Bonfire.Log.LogDebug($"Range value {num} <= {multiplier} * {blockChance}");
            if (multiplier > 0 && num <= multiplier * blockChance)
            {
                _hitsSinceShielded = 0;
                BlockNextHit = true;

                SpawnPrefab.SpawnHero(HeroController.instance.luckyDiceShieldEffectPrefab);

                Bonfire.Log.LogDebug("Blocked a hit!");
            }
            else
            {
                _hitsSinceShielded++;
            }
        }

        /// <summary>
        /// PlayerData.TakeHealth (Prefix): Prevent damage taken by setting incoming damage equal to 0 during 
        /// block state.
        /// </summary>
        public static int PreventHPLossOnBlock(int amount)
        {
            if (Bonfire.Instance.Status.ResilienceStat <= 0) return amount;
            if (BlockNextHit)
            {
                BlockNextHit = false;
                return 0;
            }

            return amount;
        }


        private const float ReflectDamageArea = 2f;

        private static int _hitsSinceShielded = 0;

        private static HitInstance CreateReflectHitInstance(HeroController instance, bool inPrimaryRange)
        {
            instance.gameObject.name = "Bonfire-Reflect";
            int reflectDamage = ReflectDamage(Bonfire.Instance.Status.ResilienceStat);
            if (!inPrimaryRange)
            {
                reflectDamage = (int)Math.Ceiling(0.5 * reflectDamage);
            }

            return new HitInstance
            {
                DamageDealt = reflectDamage,
                Source = instance.gameObject,
                AttackType = AttackTypes.Generic,
                IsFirstHit = false,
                HitEffectsType = EnemyHitEffectsProfile.EffectsTypes.Full,
                Direction = 0f,
                MagnitudeMultiplier = 0f,
                Multiplier = 1f,
                IsHeroDamage = true,
                SilkGeneration = HitSilkGeneration.Full
            };
        }

        // Create a reflect damage HitInstance and apply it on enemies in range.
        private static bool DealReflectDamage()
        {
            Bonfire.Log.LogDebug("[ResilienceStat] Reflecting damage to enemies.");

            var hc = HeroController.instance;

            float primaryRange = ReflectArea();
            float secondaryRange = 2 * primaryRange;
            float closestEnemyDistance = float.MaxValue;
            HealthManager closestEnemy = null;

            foreach (var e in EnemyHealth.EnemyMaxHp.Keys)
            {
                if (e == null)
                    continue;

                float dist = Vector2.Distance(hc.transform.position, e.transform.position);

                // apply reflect damage to enemy outside base area: HealthManagers object references must match
                if (dist > primaryRange)
                {
                    // if enemy outside primary range, check secondary range and hit the first
                    if (dist <= secondaryRange && CheckResilienceMilestone(2) && dist < closestEnemyDistance)
                    {
                        closestEnemy = e;
                        closestEnemyDistance = dist;
                        Bonfire.Log.LogDebug($"Secondary range: closest enemy updatet to {closestEnemy.name}");
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    HitInstance hit = CreateReflectHitInstance(hc, inPrimaryRange: true);
                    e.Hit(hit);
                }
            }

            if (closestEnemy != null)
            {
                // apply a separate damage instance to enemy outside primary range, but inside secondary range
                Bonfire.Log.LogDebug($"Secondary range: dealt damage to enemy {closestEnemy.name}");
                HitInstance hit = CreateReflectHitInstance(hc, inPrimaryRange: false);
                closestEnemy.Hit(hit);
            }

            return closestEnemy != null;
        }
    }
}