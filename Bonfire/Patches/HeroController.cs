using System;

using GlobalEnums;
using GlobalSettings;
using HarmonyLib;
using UnityEngine;

using BonfireSilksong.Leveling;
using BonfireSilksong.Settings;
using BonfireSilksong.Teleport;

namespace BonfireSilksong.Patches
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
                TeleportPatch.HandleArrival(__instance);

                if (BenchTeleport.IsBenchTeleporting)
                {
                    // teleporting will update blue hp too early so call update again after a short delay to 
                    // refresh state
                    Bonfire.Instance.StartCoroutine(BlueHealthUpdate.CallDelayedUpdateBlueHP());
                }
            }
        }

        [HarmonyPatch(typeof(HeroController), "TickDeliveryItems")]
        class HeroController_TickDeliveryItems
        {
            static bool Prefix()
            {
                return SettingsPatch.DeliveryQuestTimerCheck();
            }
        }

        [HarmonyPatch(typeof(HeroController), "Update")]
        class HeroController_Update
        {
            static void Prefix(HeroController __instance)
            {
                StrengthStat.RefreshFleaBrewBuffDuration(__instance);
            }

            static void Postfix()
            {
                var gm = GameManager.instance;
                var pd = PlayerData.instance;
                var hc = HeroController.instance;
                if (gm == null || pd == null) return;

                float dt = Time.deltaTime;

                EnemyHealth.RemoveUnused(dt);

                // these should prevent checks during non-gameplay events like dialogue
                if (gm.GameState != GameState.PLAYING ||
                    gm.IsNonGameplayScene() ||
                    pd.disablePause)
                {
                    // Bonfire.Log.LogInfo("[HeroController.Update] Gameplay state:\n" +
                    //     $"Gamestate: {gm.GameState}\n" +
                    //     $"isNonGamePlayScene: {gm.IsNonGameplayScene()}\n" +
                    //     $"disablePause: {PlayerData.instance.disablePause}");
                    return;
                }

                LevelingPatch.CheckNeedolinState(dt);
                GlobalStat.CheckSilkRegen(dt);
                ArtificeStat.CheckToolRegen(dt);
                WisdomStat.CheckSilkNails(dt);
                IntelligenceStat.CheckWeaverAura(dt);


                // acceptingInput is very versatile for checking when inputs can be accepted, but it's also weirdly
                // restrictive e.g. sprinting already sets it to false. It's good for cogfly respawning: fast travel
                // despawn them, but also has these weird transition states where most other blocking states are
                // temporarily switched off. But acceptingInput stays 'false' for the entire duration. Then simply
                // exluding it during bench visits allows cogflies to properly appear when 
                // - game is loaded
                // - bench fast travel is used (which must also manually despawn cogflies to prevent inactive cogflies
                // from appearing in target scene)
                if (hc.acceptingInput || LevelingPatch.PlayingdNeedolin || pd.atBench)
                {
                    WisdomStat.CheckCogflyWarriorState();
                }

                // needolin also blocks inputs: if inputs are again available & needolin playing state is true, reset it
                if (LevelingPatch.PlayingdNeedolin && hc.acceptingInput)
                    LevelingPatch.PlayingdNeedolin = false;
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
                __result = StrengthStat.UpdateNeedleChargeBeginTime(__result);
            }
        }

        [HarmonyPatch(typeof(HeroController), "CurrentNailChargeTime", MethodType.Getter)]
        class HeroController_CurrentNailChargeTime
        {
            static void Postfix(ref float __result)
            {
                __result = StrengthStat.UpdateNeedleChargeTime(__result);
            }
        }

        [HarmonyPatch(typeof(HeroController), "DidAttack")]
        class HeroController_DidAttack
        {
            static void Postfix(HeroController __instance)
            {
                DexterityStat.UpdateNeedleAttackCooldown(__instance);
            }
        }

        // private void Attack(AttackDirection attackDir)
        [HarmonyPatch(typeof(HeroController), "Attack")]
        class HeroController_Attack
        {
            static void Postfix(HeroController __instance)
            {
                DexterityStat.UpdateNeedleAttackDuration(__instance);
            }
        }

        // public void TakeDamage(GameObject go, CollisionSide damageSide, int damageAmount, HazardType hazardType, 
        //     DamagePropertyFlags damagePropertyFlags)
        [HarmonyPatch(typeof(HeroController), "TakeDamage")]
        class HealthManager_TakeDamage
        {
            static void Prefix(HeroController __instance, HazardType hazardType)
            {
                ResilienceStat.HandleBlockCheck(__instance, hazardType);

                ResilienceStat.HandleReflectDamageHitLogic(hazardType);
            }

            static void Postfix()
            {
                // safety feature to ensure that block flag is set to 'false' after TakeDamage call. 
                // Otherwise blocking flag could get stuck on 'true', making player invulnerable.
                ResilienceStat.BlockNextHit = false;
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
                //Bonfire.Log.LogDebug($"[HeroController] SilkTakeSource {source}");

                if (IntelligenceStat.WeaverAuraTakeSilk)
                    return true;

                if (WisdomStat.CheckWispLanternCostRefund(source))
                    return false;

                // this should be enough to identify Needolin silk cost: only Needolin & Silkspeed Anklets seem to use 
                // 'ActiveUse' as a source.
                if (LevelingPatch.PlayingdNeedolin && source == SilkSpool.SilkTakeSource.ActiveUse)
                    return false;

                if (LevelingPatch.CheckTauntCostRefund(amount))
                    return false;

                if (DexterityStat.CheckHarpoonCostRefund(amount))
                    return false;

                if (ArtificeStat.CheckSilkshotSilkCostRefund(amount))
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
                if (ResilienceStat.IsInBlueStateAndPlasmified(BlueState.REFLECT))
                    return true;

                if (ResilienceStat.ReflectStateHealedBlueMasks())
                    return false;

                if (IntelligenceStat.CheckWeakBindRequirement())
                {
                    amount = (int)Math.Round(amount * 0.33f);

                    // don't allow healing from the additional Claw Mirror bind. With this, first bind does nothing
                    // but any following ones heal normally.
                    if (IntelligenceStat.ExtraBind)
                    {
                        IntelligenceStat.ExtraBind = false;
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
                    DexterityStat.UsedHarpoon();
            }
        }

        // game seems to call this method only if taunt successfully consumed 1 silk. Currently weaver aura on/off 
        // toggle use this functionality. This shouldn't cost silk nor give the global 50% damage buff for next attack.
        // SilkTauntEffectConsume removes the damage buff immediately, LevelingPatch.Taunted is called upon button 
        // press in ListenFor patches which is then later consumed by LevelingPatch.CheckTauntCostRefund.
        // However the initial check for silk cost of 1 remains, this could be removed quite easily but with Silk 
        // Hearts upgrades its very easy to always have that 1 silk available anyway.
        [HarmonyPatch(typeof(HeroController), "SilkTaunted")]
        class HeroController_SilkTaunted
        {
            static void Postfix()
            {
                if (IntelligenceStat.AuraOrBurstEnabled)
                {
                    IntelligenceStat.WeaverAuraTaunt = !IntelligenceStat.WeaverAuraTaunt;

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
                if (IntelligenceStat.AuraOrBurstEnabled)
                    IntelligenceStat.WeaverAuraRingTaunt = !IntelligenceStat.WeaverAuraRingTaunt;
            }
        }

        // public bool CanDoubleJump(bool checkControlState)
        [HarmonyPatch(typeof(HeroController), "CanDoubleJump")]
        class HeroController_CanDoubleJump
        {
            static void Postfix(ref bool __result)
            {
                if (Gameplay.BrollySpikeTool.IsEquipped &&
                    ArtificeStat.CheckArtificeMilestone(2) &&
                    __result)
                {
                    if (!ArtificeStat.HitEnemyWithSawtoothCirclet)
                        ArtificeStat.HitEnemyWithSawtoothCirclet = true;
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
                    ArtificeStat.CheckArtificeMilestone(2) &&
                    __result)
                {
                    if (!ArtificeStat.HitEnemyWithSawtoothCirclet)
                        ArtificeStat.HitEnemyWithSawtoothCirclet = true;
                }
            }
        }

        [HarmonyPatch(typeof(HeroController), "CanPlayNeedolin")]
        class HeroController_CanPlayNeedolin
        {
            static void Postfix(ref bool __result)
            {
                if (__result)
                {
                    WisdomStat.ToggleSummonsActivity();
                    LevelingPatch.PlayingdNeedolin = true;
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
                if (ArtificeStat.CanUseTool(tool))
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
                __state = ArtificeStat.GetThrownToolItem();

                // can't use Thread Storm while Luck's Pinwielder is enabled
                if (LuckStat.IsPinwielderEnabled && __state.name == "Thread Sphere")
                    return false;

                // can't use Voltvessels tool while in Intelligence's Weak Bind is enabled
                if (IntelligenceStat.CheckWeakBindRequirement() && __state.name == "Lightning Rod")
                    return false;

                // can't use Pale Nails when Wisdom's Silk Nails are enabled.
                if (WisdomStat.IsSilkNailsEnabled && __state.name == "Silk Boss Needle")
                    return false;

                // can't use Cogfly tool when Wisdom's Cogfly Warrior is enabled
                if (WisdomStat.IsCogflyWarriorEnabled && __state.name == "Cogwork Flier")
                    return false;

                // if in any blue state, allow use of Plasmium Phial normally before final milestone.
                // However, when final milestone is reached the behavior changes:
                // - always get Plasmified instantly when using Phial, no matter how much hp you have
                // - during Plasmified state, can't use Phial anymore
                if (__state.name == "Lifeblood Syringe" && ResilienceStat.IsInAnyBlueState())
                {
                    if (!HeroController.instance.IsInLifebloodState)
                    {
                        if (ResilienceStat.CheckResilienceMilestone(3))
                            BlueHealthUpdate.CallUpdateBlueHP(9);

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                if (!isAutoThrow && __state != null)
                    _extraToolThrows = ArtificeStat.GetToolCopyCount(__state);

                return true;
            }

            static void Postfix(ToolItem __state)
            {
                if (__state == null) return;

                LuckStat.CheckStraightPinRefund(__state);

                if (_extraToolThrows <= 0)
                {
                    ArtificeStat.CheckToolChargeRefund(__state);
                    return;
                }

                ArtificeStat.SetThrowFieldValues(__state, setWillThrowTool: true, setQueuedAutoThrow: true);
                _extraToolThrows--;
            }
        }

        [HarmonyPatch(typeof(HeroController), "HitMaxBlueHealth")]
        class HeroController_HitMaxBlueHealth
        {
            static void Postfix()
            {
                // When in glass cannon state, block entering the Plasmified state without using Plasmium Phial.
                if (ResilienceStat.IsInAnyBlueState() &&
                    !ResilienceStat.CheckResilienceMilestone(3))
                {
                    HeroController.instance.ResetLifebloodState();

                    // when game attempts to enter Plasmified state, but it gets immediately cancelled, the blue hp
                    // value seems to get incremented by 1 and ending up to 9 health. This forces is back.
                    PlayerData.instance.healthBlue = 8;
                }
            }
        }

        [HarmonyPatch(typeof(HeroController), "LeaveScene")]
        class HeroController_LeaveScene
        {
            static void Prefix()
            {
                EnemyHealth.ResetEnemyHpData();
            }
        }

        // public void AddToMaggotCharmTimer(float delta)
        [HarmonyPatch(typeof(HeroController), "AddToMaggotCharmTimer")]
        class HeroController_AddToMaggotCharmTimer
        {
            static bool Prefix()
            {
                if (GlobalStat.CheckGlobalMilestone(GlobalMilestone.MAGGOTREPEL))
                    return false;

                return true;
            }
        }

        [HarmonyPatch(typeof(HeroController), "DidMaggotCharmHit")]
        class HeroController_DidMaggotCharmHit
        {
            static bool Prefix()
            {
                if (GlobalStat.CheckGlobalMilestone(GlobalMilestone.MAGGOTREPEL))
                    return false;

                return true;
            }
        }
    }
}