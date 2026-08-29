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
                    IntelligenceLogic.CheckWeaverAura(dt);
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
                if (PatchHelpers.CheckTauntCostRefund(amount))
                    return false;

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

        // game seems to call this method only if taunt successfully consumed 1 silk. Currently weaver aura on/off 
        // toggle use this functionality. This shouldn't cost silk nor give the global 50% damage buff for next attack.
        // SilkTauntEffectConsume removes the damage buff immediately, PatchHelpers.Taunted is called upon button 
        // press in ListenFor patches which is then later consumed by PatchHelpers.CheckTauntCostRefund.
        // However the initial check for silk cost of 1 remains, this could be removed quite easily but with Silk 
        // Hearts upgrades its very easy to always have that 1 silk available anyway.
        [HarmonyPatch(typeof(HeroController), "SilkTaunted")]
        class HeroController_SilkTaunted
        {
            static void Postfix()
            {
                if (IntelligenceLogic.AuraOrBurstEnabled)
                {
                    IntelligenceLogic.WeaverAuraTaunt = !IntelligenceLogic.WeaverAuraTaunt;

                    // this removed the normal damage buff
                    HeroController.instance.SilkTauntEffectConsume();
                }
            }
        }

        // this is always called when Throwing Ring tool is equipped as taunting no longer costs silk, and instead of
        // 50% global damage it now gives only the 50% Throwing Ring tool damage. So no need to call 
        // RingTauntEffectConsume here.
        [HarmonyPatch(typeof(HeroController), "RingTaunted")]
        class HeroController_RingTaunted
        {
            static void Postfix()
            {
                if (IntelligenceLogic.AuraOrBurstEnabled)
                    IntelligenceLogic.WeaverAuraRingTaunt = !IntelligenceLogic.WeaverAuraRingTaunt;
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

        [HarmonyPatch(typeof(HeroController), "CanPlayNeedolin")]
        class HeroController_CanFPlayNeedolin
        {
            static void Postfix(ref bool __result)
            {
                if (__result)
                {
                    WisdomLogic.ToggleSummonsActivity();
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
                    return false;

                // can't use Cogfly tool when Wisdom's Cogfly Warrior is enabled
                if (WisdomLogic.IsCogflyWarriorEnabled && __state.name == "Cogwork Flier")
                    return false;

                // if in any blue state, allow use of Plasmium Phial normally before final milestone.
                // However, when final milestone is reached the behavior changes:
                // - always get Plasmified instantly when using Phial, no matter how much hp you have
                // - during Plasmified state, can't use Phial anymore
                if (__state.name == "Lifeblood Syringe" && ResilienceLogic.IsInAnyBlueState())
                {
                    if (!HeroController.instance.IsInLifebloodState)
                    {
                        if (ResilienceLogic.CheckResilienceMilestone(3))
                            BlueHealthUpdate.CallUpdateBlueHP(9);

                        return true;
                    }
                    else
                    {
                        return false;
                    }
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