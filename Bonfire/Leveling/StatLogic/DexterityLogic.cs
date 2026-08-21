using System;
using System.Reflection;

namespace BonfireSilksong
{
    public static class DexterityLogic
    {
        public const int ClawlineHitLimit = 7;

        public static int DexterityMilestone(int m)
        {
            switch (m)
            {
                case 1: return 1;
                case 2: return 2;
                case 3: return 3;
                default: return 999;
            }
        }

        public static bool CheckDexterityMilestone(int m)
        {
            var s = Bonfire.Instance.Status.DexterityStat + LevelingSystem.MilestoneBonus;
            return s >= DexterityMilestone(m);
        }

        /// <summary>
        /// Stat scaling: needle attack speed.
        /// </summary>
        public static float AttackSpeed(int totalDex)
        {
            return (float)Math.Round(2.7 / (1.0 + 1.82 * Math.Exp(-0.08 * (totalDex + 1))) - 0.01, 2);
        }

        /// <summary>
        /// Needle crit damage.
        /// </summary>
        public static int CritDamage(int totalDex, int needleDamage)
        {
            return (int)(needleDamage * (1.2 + Math.Log(totalDex + 1)));
        }

        /// <summary>
        /// Apply crit damage bonus to hit and display visual effect on player character.
        /// </summary>
        public static int ApplyCritDamage(int baseDamage)
        {
            int updatedDamage = CritDamage(Bonfire.Instance.Status.DexterityStat, baseDamage);

            HeroController.instance.GetComponent<SpriteFlash>().FlashGrimmflame();

            Bonfire.Log.LogDebug($"[DexterityLogic] Applied Crit Damage, damage = {updatedDamage}");

            return updatedDamage;
        }

        /// <summary>
        /// Apply Clawline damage bonuses.
        /// </summary>
        public static int ApplyClawlineDamageBonuses(int baseDamage)
        {
            _clawlineHitCounter += 1;
            Bonfire.Log.LogDebug("Clawline hits: " + _clawlineHitCounter);

            int updatedDamage = baseDamage;

            if (Bonfire.Instance.Status.DexterityStat < 1) return baseDamage;

            Bonfire.Log.LogDebug($"[DexterityLogic] Apply Clawline damage bonuses");

            if (CheckDexterityMilestone(3))
            {
                updatedDamage = (int)Math.Round(updatedDamage * 1.5f);
                Bonfire.Log.LogDebug("Applied x1.5 multiplier");
            }
            else if (CheckDexterityMilestone(1))
            {
                updatedDamage = (int)Math.Round(updatedDamage * 1.25f);
                Bonfire.Log.LogDebug("Applied x1.25 multiplier");
            }

            if (CheckDexterityMilestone(1) && _clawlineHitCounter >= ClawlineHitLimit)
            {
                updatedDamage *= 2;
                Bonfire.Log.LogDebug($"Clawline {ClawlineHitLimit}. hit: applied x2 multiplier");

                if (CheckDexterityMilestone(3))
                {
                    HeroController.instance.AddHealth(1);
                    Bonfire.Log.LogDebug($"Clawline {ClawlineHitLimit}. hit: healed 1 mask");
                }

                _clawlineHitCounter = 0;
            }

            Bonfire.Log.LogDebug($"Clawline updated damage={updatedDamage}");

            return updatedDamage;
        }

        /// <summary>
        /// Heal the player if damage was a crit.
        /// </summary>
        public static void ApplyCritHeal(bool wasCrit)
        {
            if (CheckDexterityMilestone(3) && wasCrit)
            {
                HeroController.instance.AddHealth(1);
                Bonfire.Log.LogDebug("[DexterityLogic] Crit heal");
            }

            // -- original idea was to heal 2 masks from clawline and needle strike, 1 from the rest.
            // This can stay here for now, in case crit healing needs to be adjusted for specific damage sources.
            /*
            string s = source.ToLower();

            Bonfire.Log.LogDebug("[DexterityLogic] Crit heal");

            if (CheckDexterityMilestone_3 && wasCrit)
            {
                if (s == "default")
                {
                    HeroController.instance.AddHealth(1);
                    Bonfire.Log.LogDebug("Heal type: default");
                }
                else if (s == "clawline" || (isNeedle && !s.Contains("slash")))
                {
                    HeroController.instance.AddHealth(2);
                    Bonfire.Log.LogDebug($"Heal type: {s}");
                }
                else
                {
                    Bonfire.Log.LogDebug("No healing");
                }
            }
            */
        }

        /// <summary>
        /// HeroController.DidAttack (Postfix): Update needle attack cooldown.
        /// </summary>
        public static void UpdateNeedleAttackCooldown(HeroController instance)
        {
            var config = instance.Config;
            if (config == null) return;

            if (Bonfire.Instance.Status.DexterityStat < 1) return;

            float attackSpeed = AttackSpeed(Bonfire.Instance.Status.DexterityStat);

            AttackCooldownField?.SetValue(
                instance,
                (float)AttackCooldownField.GetValue(instance) / attackSpeed);
        }

        /// <summary>
        /// HeroController.Attack (Postfix): Update needle attack duration.
        /// </summary>
        public static void UpdateNeedleAttackDuration(HeroController instance)
        {
            var config = instance.Config;
            if (config == null) return;

            if (Bonfire.Instance.Status.DexterityStat < 1) return;

            float attackSpeed = AttackSpeed(Bonfire.Instance.Status.DexterityStat);

            AttackDurationField?.SetValue(
                instance,
                (float)AttackDurationField.GetValue(instance) / attackSpeed);
        }

        /// <summary>
        /// HeroController.CanHarpoonDash (Postfix): Toggle usedHarpoon state for RefundHarpoonCost.
        /// </summary>
        public static void UsedHarpoon()
        {
            _usedHarpoon = true;
        }

        /// <summary>
        /// HeroController.TakeSilk (Prefix): If Clawline silk cost should be ignored.
        /// </summary>
        public static bool CheckHarpoonCostRefund(int cost)
        {
            bool refunded = _usedHarpoon && cost == 1 && CheckDexterityMilestone(2);

            _usedHarpoon = false;

            return refunded;
        }

        private static int _clawlineHitCounter = 0;
        private static bool _usedHarpoon = false;

        private static readonly FieldInfo AttackCooldownField = typeof(HeroController)
            .GetField("attack_cooldown", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo AttackDurationField = typeof(HeroController)
            .GetField("attackDuration", BindingFlags.Instance | BindingFlags.NonPublic);
    }
}