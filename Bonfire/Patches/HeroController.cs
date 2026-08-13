using GlobalEnums;
using HarmonyLib;
using UnityEngine;

namespace BonfireSilksong
{
    internal class HeroController_Patch
    {
        // force config data saving when charms are updated
        // As leveling up and respeccing needs these, it's important to explicitly call CharmUpdate inside them.
        [HarmonyPatch(typeof(HeroController), "CharmUpdate")]
        class HeroController_CharmUpdate
        {
            static void Postfix()
            {
                LevelingSystem.Instance.RefreshRelicsDeposited();
                Bonfire.Instance.SaveSettings();
            }
        }

        // private void FinishedEnteringScene(bool setHazardMarker, bool preventRunBob)
        [HarmonyPatch(typeof(HeroController), "FinishedEnteringScene")]
        class HeroController_FinishedEnteringScene
        {
            static void Postfix(HeroController __instance)
            {
                TeleportLogic.HandleArrival(__instance);
            }
        }

        [HarmonyPatch(typeof(HeroController), "TickDeliveryItems")]
        class HeroController_TickDeliveryItems
        {
            static bool Prefix()
            {
                return SettingsLogic.DeliveryQuestTimerCheck();
            }
        }

        [HarmonyPatch(typeof(HeroController), "Update")]
        class HeroController_Update
        {
            static void Prefix(HeroController __instance)
            {
                StrengthLogic.RefreshFleaBrewBuffDuration(__instance);
            }

            static void Postfix()
            {
                Bonfire.Log.LogDebug("[HeroController.Update] Update hp data, roll crit, check sil + tool regen");

                StatsShared.FlushEnemyHPData();
                StatsShared.RollCritValue();

                if (HeroController.instance == null || PlayerData.instance == null)
                    return;

                float dt = Time.deltaTime;
                try
                {
                    WisdomLogic.CheckSilkRegen(dt);
                    ArtificeLogic.CheckToolRegen(dt);
                }
                catch { }
            }
        }

        [HarmonyPatch(typeof(HeroController), "UpdateBlueHealth")]
        class HeroController_UpdateBlueHealth
        {
            static void Postfix()
            {
                ResilienceLogic.CustomUpdateBlueHealth();
            }
        }

        [HarmonyPatch(typeof(HeroController), "CurrentNailChargeBeginTime", MethodType.Getter)]
        class HeroController_CurrentNailChargeBeginTime
        {
            static void Postfix(ref float __result)
            {
                __result = StrengthLogic.UpdateNeedleChargeBeginTime(__result);
            }
        }

        [HarmonyPatch(typeof(HeroController), "CurrentNailChargeTime", MethodType.Getter)]
        class HeroController_CurrentNailChargeTime
        {
            static void Postfix(ref float __result)
            {
                __result = StrengthLogic.UpdateNeedleChargeTime(__result);
            }
        }

        [HarmonyPatch(typeof(HeroController), "DidAttack")]
        class HeroController_DidAttack
        {
            static void Postfix(HeroController __instance)
            {
                DexterityLogic.UpdateNeedleAttackCooldown(__instance);
            }
        }

        // private void Attack(AttackDirection attackDir)
        [HarmonyPatch(typeof(HeroController), "Attack")]
        class HeroController_Attack
        {
            static void Postfix(HeroController __instance)
            {
                DexterityLogic.UpdateNeedleAttackDuration(__instance);
            }
        }

        // public void TakeDamage( GameObject go, CollisionSide damageSide, int damageAmount, HazardType hazardType, 
        //     DamagePropertyFlags damagePropertyFlags)
        [HarmonyPatch(typeof(HeroController), "TakeDamage")]
        class HealthManager_TakeDamage
        {
            static void Prefix(HeroController __instance, HazardType hazardType)
            {
                ResilienceLogic.HandleBlockCheck(__instance, hazardType);
            }

            static void Postfix()
            {
                // safety feature to ensure that block flag is set to 'false' after TakeDamage call. 
                // Otherwise blocking flag could get stuck on 'true', making player invulnerable.
                StatsShared.BlockNextHit = false;
            }
        }

        // public void SilkGain(HitInstance hitInstance)
        [HarmonyPatch(typeof(HeroController), "SilkGain", new[] { typeof(HitInstance) })]
        class HeroController_SilkGain
        {
            static void Postfix(HitInstance hitInstance)
            {
                WisdomLogic.AddExtraSilkOnNeedleHit(hitInstance);
            }
        }

        [HarmonyPatch(typeof(HeroController), "TakeSilk", new[] { typeof(int), typeof(SilkSpool.SilkTakeSource) })]
        class HeroController_TakeSilk
        {
            static void Postfix(HeroController __instance, int amount, SilkSpool.SilkTakeSource source)
            {
                WisdomLogic.WispLanternRefundCost(__instance, amount, source);
            }
        }
    }
}