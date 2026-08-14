using System;
using System.Reflection;

namespace BonfireSilksong
{
    public static class DexterityLogic
    {
        public static int DexterityMilestone_1 => 1;
        public static int DexterityMilestone_2 => 2;
        public static int DexterityMilestone_3 => 3;
        public static bool CheckDexterityMilestone_1 => Bonfire.Instance.Status.DexterityStat >= DexterityMilestone_1;
        public static bool CheckDexterityMilestone_2 => Bonfire.Instance.Status.DexterityStat >= DexterityMilestone_2;
        public static bool CheckDexterityMilestone_3 => Bonfire.Instance.Status.DexterityStat >= DexterityMilestone_3;

        // Stat scaling: needle attack speed
        public static float AttackSpeed(int totalDex)
        {
            return (float)Math.Round(2.7 / (1.0 + 1.82 * Math.Exp(-0.08 * (totalDex + 1))) - 0.01, 2);
        }

        // Stat scaling: needle crit damage
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

            Bonfire.Log.LogDebug($"[DexterityLogic] Crit Damage = {updatedDamage}");

            return updatedDamage;
        }

        /// <summary>
        /// Apply Clawline damage bonuses.
        /// </summary>
        public static int ApplyHarpoonDamageBonuses(int baseDamage)
        {
            _harpoonHitCounter += 1;

            int updatedDamage = baseDamage;

            if (Bonfire.Instance.Status.DexterityStat < 1) return baseDamage;

            if (CheckDexterityMilestone_2 && _harpoonHitCounter >= 5)
            {
                updatedDamage *= 2;
                _harpoonHitCounter = 0;

                Bonfire.Log.LogDebug($"[DexterityLogic] 4th harpoon hit, updated damage={updatedDamage}");
            }

            return updatedDamage;
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
            bool refunded = _usedHarpoon && cost == 1 && CheckDexterityMilestone_1;

            _usedHarpoon = false;

            return refunded;
        }

        private static int _harpoonHitCounter = 0;
        private static bool _usedHarpoon = false;

        private static readonly FieldInfo AttackCooldownField = typeof(HeroController)
            .GetField("attack_cooldown", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo AttackDurationField = typeof(HeroController)
            .GetField("attackDuration", BindingFlags.Instance | BindingFlags.NonPublic);
    }
}