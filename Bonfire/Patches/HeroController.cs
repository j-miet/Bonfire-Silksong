using System;

using GlobalEnums;
using GlobalSettings;
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

                EnemyHealth.FlushEnemyHPData();

                if (HeroController.instance == null || PlayerData.instance == null)
                    return;

                float dt = Time.deltaTime;
                try
                {
                    WisdomLogic.CheckSilkRegen(dt);
                    ArtificeLogic.CheckToolRegen(dt);
                    WisdomLogic.CheckSilkNails(dt);
                }
                catch { }

                WisdomLogic.CheckCogflyWarriorState();
            }
        }

        [HarmonyPatch(typeof(HeroController), "UpdateBlueHealth")]
        class HeroController_UpdateBlueHealth
        {
            static void Postfix()
            {
                BlueHealthUpdate.CustomUpdateBlueHealth();
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

        // public void TakeDamage(GameObject go, CollisionSide damageSide, int damageAmount, HazardType hazardType, 
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
                ResilienceLogic.BlockNextHit = false;
            }
        }

        // public void SilkGain(HitInstance hitInstance)
        [HarmonyPatch(typeof(HeroController), "SilkGain", new[] {
            typeof(HitInstance)
        })]
        class HeroController_SilkGain
        {
            static void Postfix(HitInstance hitInstance)
            {
                WisdomLogic.AddExtraSilkOnNeedleHit(hitInstance);
            }
        }

        // public void TakeSilk(int amount, SilkSpool.SilkTakeSource source)
        [HarmonyPatch(typeof(HeroController), "TakeSilk", new[] {
            typeof(int),
            typeof(SilkSpool.SilkTakeSource)
        })]
        class HeroController_TakeSilk
        {
            static bool Prefix(int amount, SilkSpool.SilkTakeSource source)
            {
                if (WisdomLogic.CheckWispLanternCostRefund(source))
                    return false;

                if (DexterityLogic.CheckHarpoonCostRefund(amount))
                    return false;

                if (ArtificeLogic.CheckSilkshotSilkCostRefund(amount))
                    return false;

                return true;
            }
        }

        // public void AddHealth(int amount)
        [HarmonyPatch(typeof(HeroController), "AddHealth")]
        class HeroController_AddHealth
        {
            static bool Prefix(ref int amount)
            {
                // This wasn't originally intended, but it can stay:
                // If player has Weak Bind and is in reflect blue state, this check will override 
                // CheckWeakBindRequirement and thus allow the extra bind from Claw Mirror to heal. So you get 2 heals,
                // or even 3 with Multibinder.
                if (ResilienceLogic.IsInBlueStateAndPlasmified(BlueState.REFLECT))
                    return true;

                if (ResilienceLogic.ReflectStateHealedBlueMasks())
                    return false;

                if (IntelligenceLogic.CheckWeakBindRequirement())
                {
                    amount = (int)Math.Round(amount * 0.33f);

                    // don't allow healing from the additional Claw Mirror bind. With this, first bind does nothing
                    // but any following ones heal normally.
                    if (IntelligenceLogic.ExtraBind)
                    {
                        IntelligenceLogic.ExtraBind = false;
                        return false;
                    }

                    return true;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(HeroController), "CanHarpoonDash")]
        class HeroController_CanHarpoonDash
        {
            static void Postfix(ref bool __result)
            {
                if (__result)
                    DexterityLogic.UsedHarpoon();
            }
        }

        [HarmonyPatch(typeof(HeroController), "SilkTaunted")]
        class HeroController_SilkTaunted
        {
            static void Postfix()
            {
                WisdomLogic.ToggleSummonsActivity();

                IntelligenceLogic.DealTauntDamage();

                if (IntelligenceLogic.CheckIntelligenceMilestone(1) &&
                    Bonfire.Instance.Status.ShowCustomRangeIndicator)
                {
                    RangeIndicator.Display(
                        IntelligenceLogic.TauntRange(Bonfire.Instance.Status.IntelligenceStat, isGUI: true));
                }
            }
        }

        [HarmonyPatch(typeof(HeroController), "RingTaunted")]
        class HeroController_RingTaunted
        {
            static void Postfix()
            {
                WisdomLogic.ToggleSummonsActivity();

                IntelligenceLogic.DealTauntDamage();

                if (IntelligenceLogic.CheckIntelligenceMilestone(1) &&
                    Bonfire.Instance.Status.ShowCustomRangeIndicator)
                {
                    RangeIndicator.Display(
                        IntelligenceLogic.TauntRange(Bonfire.Instance.Status.IntelligenceStat, isGUI: true));
                }
            }
        }

        // public bool CanDoubleJump(bool checkControlState)
        [HarmonyPatch(typeof(HeroController), "CanDoubleJump")]
        class HeroController_CanDoubleJump
        {
            static void Postfix(ref bool __result)
            {
                if (Gameplay.BrollySpikeTool.IsEquipped &&
                    ArtificeLogic.CheckArtificeMilestone(2) &&
                    __result)
                {
                    if (!ArtificeLogic.HitEnemyWithSawtoothCirclet)
                        ArtificeLogic.HitEnemyWithSawtoothCirclet = true;
                }
            }
        }

        // public bool CanDoubleJump(bool checkControlState)
        [HarmonyPatch(typeof(HeroController), "CanFloat")]
        class HeroController_CanFloat
        {
            static void Postfix(ref bool __result)
            {
                if (Gameplay.BrollySpikeTool.IsEquipped &&
                    ArtificeLogic.CheckArtificeMilestone(2) &&
                    __result)
                {
                    if (!ArtificeLogic.HitEnemyWithSawtoothCirclet)
                        ArtificeLogic.HitEnemyWithSawtoothCirclet = true;
                }
            }
        }

        // private bool CanThrowTool(ToolItem tool, AttackToolBinding binding, bool reportFailure)
        [HarmonyPatch(typeof(HeroController), "CanThrowTool", new[] {
            typeof(ToolItem),
            typeof(AttackToolBinding),
            typeof(bool)
        })]
        class HeroController_CanThrowTool
        {
            static bool Prefix(ToolItem tool, ref bool __result)
            {
                if (ArtificeLogic.CanUseTool(tool))
                {
                    __result = true;
                    return false;
                }

                return true;
            }
        }

        // private bool ThrowTool(bool isAutoThrow)
        [HarmonyPatch(typeof(HeroController), "ThrowTool")]
        class HeroController_ThrowTool
        {
            private static int _extraToolThrows;

            static bool Prefix(bool isAutoThrow, out ToolItem __state)
            {
                __state = ArtificeLogic.GetThrownToolItem();

                // can't use Pale Nails when Wisdom's Silk Nails are enabled.
                if (WisdomLogic.IsSilkNailsEnabled && __state.name == "Silk Boss Needle")
                {
                    return false;
                }

                // can't use Cogfly tool when Wisdom's Cogfly Warrior is enabled
                if (WisdomLogic.IsCogflyWarriorEnabled && __state.name == "Cogwork Flier")
                {
                    return false;
                }

                // can't use Plasmium Phial when Resilience's blue state is enabled and milestone 3 not yet unlocked.
                if (__state.name == "Lifeblood Syringe" &&
                    !HeroController.instance.IsInLifebloodState &&
                    ResilienceLogic.IsInAnyBlueState() &&
                    !ResilienceLogic.CheckResilienceMilestone(3))
                {
                    return false;
                }

                if (!isAutoThrow && __state != null)
                    _extraToolThrows = ArtificeLogic.GetToolCopyCount(__state);

                return true;
            }

            static void Postfix(ToolItem __state)
            {
                if (__state == null) return;

                if (_extraToolThrows <= 0)
                {
                    ArtificeLogic.CheckToolChargeRefund(__state);
                    return;
                }

                ArtificeLogic.SetThrowFieldValues(__state, setWillThrowTool: true, setQueuedAutoThrow: true);
                _extraToolThrows--;
            }
        }

        [HarmonyPatch(typeof(HeroController), "HitMaxBlueHealth")]
        class HeroController_HitMaxBlueHealth
        {
            static void Postfix()
            {
                // When in glass cannon state, block entering the Plasmified state without using Plasmium Phial.
                if (ResilienceLogic.IsInAnyBlueState() &&
                    !ResilienceLogic.CheckResilienceMilestone(3))
                {
                    HeroController.instance.ResetLifebloodState();

                    // when game attempts to enter Plasmified state, but it gets immediately cancelled, the blue hp
                    // value seems to get incremented by 1 and ending up to 9 health. This forces is back.
                    PlayerData.instance.healthBlue = 8;
                }
            }
        }
    }
}