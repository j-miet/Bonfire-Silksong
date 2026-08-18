using System;
using System.Reflection;

using UnityEngine;

namespace BonfireSilksong
{
    public static class StrengthLogic
    {
        public static int StrengthMilestone_1 => 1;
        public static int StrengthMilestone_2 => 2;
        public static int StrengthMilestone_3 => 3;
        public static bool CheckStrengthMilestone_1 => Bonfire.Instance.Status.StrengthStat >= StrengthMilestone_1;
        public static bool CheckStrengthMilestone_2 => Bonfire.Instance.Status.StrengthStat >= StrengthMilestone_2;
        public static bool CheckStrengthMilestone_3 => Bonfire.Instance.Status.StrengthStat >= StrengthMilestone_3;

        // Stat scaling: needle damage
        public static int NeedleDamage(int baseDamage, int totalStr)
        {
            return (int)Math.Round(
                baseDamage * Math.Pow(1.2, Math.Log(totalStr + 1, 2.0)));
        }

        /// <summary>
        /// Apply needle damage bonuses. PlayerData.nailDamage getter will handle base damage updates so don't reapply
        /// NeedleDamage bonus again!
        /// </summary>
        public static int ApplyNeedleDamageBonuses(int damage, string sourceName)
        {
            Bonfire.Log.LogDebug($"[StrengthLogic] Apply needle damage bonuses:");
            int updated = damage;

            if (CheckStrengthMilestone_3 && sourceName == "DownSlash")
            {
                updated *= 2; // 100% extra damage on pogos

                Bonfire.Log.LogDebug($"- Applied pogo damage");
            }

            Bonfire.Log.LogDebug($"Damage after bonuses for {sourceName} = {updated}");

            return updated;
        }

        /// <summary>
        /// PlayerData.nailDamage (Postfix): Update needle damage getter, this way all needle damage buffs
        /// are applied on top of custom value + this makes needle strike damage also scale properly.
        /// </summary>
        public static int UpdateNeedleBaseDamage(int baseDamage)
        {
            Bonfire.Log.LogDebug($"[PlayerData.nailDamage] <Vanilla> needle base damage: {baseDamage}");

            if (Bonfire.Instance.Status.StrengthStat < 1) return baseDamage;

            int updatedDamage = NeedleDamage(baseDamage, Bonfire.Instance.Status.StrengthStat);

            Bonfire.Log.LogDebug($"[PlayerData.nailDamage] <Modded> needle base damage: {updatedDamage}");
            return updatedDamage;
        }

        /// <summary>
        /// NailAttackBase.OnSlashStarting (Postfix): Update needle basic attack range.
        /// </summary>
        public static void UpdateNeedleRange(NailAttackBase instance)
        {
            if (!CheckStrengthMilestone_2) return;

            Vector3 scale = instance.transform.localScale;

            scale.x *= NeedleRangeMultiplier.x;
            scale.y *= NeedleRangeMultiplier.y;

            instance.transform.localScale = scale;
        }

        /// <summary>
        /// HeroNailImbuement.Update (Prefix): Update Flintslate red tool buff continuously for infinite duration.
        /// </summary>
        public static void RefreshFlintslateBuffDuration(HeroNailImbuement instance)
        {
            if (!CheckStrengthMilestone_3) return;
            if (instance.CurrentElement != NailElements.Fire) return;

            ImbuementTimeLeftField.SetValue(instance, 999f);
        }

        /// <summary>
        /// HeroController.Update (Prefix): Update Flea Brew red tool buff continuously for infinite duration.
        /// </summary>
        public static void RefreshFleaBrewBuffDuration(HeroController instance)
        {
            if (CheckStrengthMilestone_3 && instance.IsUsingQuickening)
            {
                QuickeningTimeLeftField.SetValue(instance, 999f);
            }
        }

        /// <summary>
        /// HeroController.CurrentNailChargeBeginTime (Postfix): Update initial windup time before needle strike
        /// charge begins.
        /// </summary>
        public static float UpdateNeedleChargeBeginTime(float result)
        {
            if (CheckStrengthMilestone_3)
                result *= 0.6f;
            else if (CheckStrengthMilestone_2)
                result *= 0.8f;

            return result;
        }

        /// <summary>
        /// HeroController.CurrentNailChargeTime (Postfix): Update time it takes to charge needle strike to full.
        /// </summary>
        public static float UpdateNeedleChargeTime(float result)
        {
            if (CheckStrengthMilestone_3)
                result *= 0.6f;
            else if (CheckStrengthMilestone_1)
                result *= 0.8f;

            CurrentChargeLimit = result;

            return result;
        }

        /// <summary>
        /// ListenForAttack.OnUpdate (Prefix) + ListenForAttack2.OnUpdate (Prefix): Automatically release the needle
        /// strike after its ready for use.
        /// </summary>
        public static void ReleaseNeedleChargeAutomatically()
        {
            if (!CheckStrengthMilestone_1 || !Bonfire.Instance.Status.AutoReleaseNeedleStrike) return;

            if ((float)NailChargeTimerField.GetValue(HeroController.instance) > CurrentChargeLimit)
            {
                Bonfire.Log.LogDebug("[StrengthLogic] Attempting to release needle strike automatically");
                var fsm = FindNeedleStrikeFSM();

                if (fsm == null)
                {
                    Bonfire.Log.LogWarning("Nail Arts FSM not found.");
                    return;
                }

                Bonfire.Log.LogDebug("Sending \"BUTTON UP\" event.");
                fsm.SendEvent("BUTTON UP");
            }
        }


        // find FSM responsible for needle strike
        private static PlayMakerFSM FindNeedleStrikeFSM()
        {
            foreach (var fsm in HeroController.instance.GetComponents<PlayMakerFSM>())
            {
                if (fsm.FsmName == "Nail Arts")
                    return fsm;
            }

            return null;
        }

        private static readonly Vector2 NeedleRangeMultiplier = new Vector2(1.33f, 1.33f);
        private static float CurrentChargeLimit;

        private static readonly FieldInfo ImbuementTimeLeftField = typeof(HeroNailImbuement)
            .GetField("imbuementTimeLeft", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo QuickeningTimeLeftField = typeof(HeroController)
            .GetField("quickeningTimeLeft", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo NailChargeTimerField = typeof(HeroController)
            .GetField("nailChargeTimer", BindingFlags.Instance | BindingFlags.NonPublic);
    }
}