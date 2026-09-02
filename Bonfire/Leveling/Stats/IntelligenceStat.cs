using System;

using HutongGames.PlayMaker;
using UnityEngine;

namespace BonfireSilksong
{
    public enum WeaverAura
    {
        DISABLED,
        AURA,
        BURST
    }

    public static class IntelligenceStat
    {
        public const int WeaverAuraBaseDamage = 5;
        public const float WeaverAuraBaseRange = 5f; // actual aura range, used in visuals + hit detection

        public static bool BindCostInit = false;
        public static bool ExtraBind = false;
        public static bool WeaverAuraTaunt = false;
        public static bool WeaverAuraRingTaunt = false;

        public static bool IsWeaverAuraEnabled
        {
            get
            {
                return Bonfire.Instance.Status.WeaverAuraMode == (int)WeaverAura.AURA &&
                    CheckIntelligenceMilestone(1);
            }
        }

        public static bool IsVoidBurstEnabled
        {
            get
            {
                return Bonfire.Instance.Status.WeaverAuraMode == (int)WeaverAura.BURST &&
                    CheckIntelligenceMilestone(4);
            }
        }

        public static bool AuraOrBurstEnabled => IsWeaverAuraEnabled || IsVoidBurstEnabled;

        public static int IntelligenceMilestone(int m)
        {
            switch (m)
            {
                case 1: return 1;
                case 2: return 10;
                case 3: return 25;
                case 4: return 35;
                default: return 99;
            }
        }

        public static bool CheckIntelligenceMilestone(int m)
        {
            var s = Bonfire.Instance.Status.IntelligenceStat;
            int b = LevelingSystem.MilestoneBonus;
            return s + b >= IntelligenceMilestone(m) && s >= 1;
        }

        /// <summary>
        /// Silk skill damage.
        /// </summary>
        public static int SpellDamage(int baseDamage, int totalInt)
        {
            if (totalInt <= 0) return baseDamage;

            return (int)Math.Round(baseDamage * Math.Pow(1.25, Math.Log(totalInt + 1, 2.0)));
        }

        /// <summary>
        /// Binding (healing) cost.
        /// In HK, soul container has range 0-99, focus costs 33 soul.
        /// In HK:SS, silk container has range 0-9, with all upgrades reaching 0-18, bind costs 3 silk.
        /// In HK, range is then divided by 33, in Silksong by 9.
        /// => Silksong scaling is not as smooth, but ramps a bit harder on high intelligence.
        /// </summary>
        public static int BindCost(int totalInt)
        {
            if (totalInt <= 0) return 9;

            return (int)Math.Round(9.1 * Math.Exp(-0.01 * (totalInt + 2.0)));
        }

        // -- secondary --
        // WeaverAura is a custom HitInstance that will always apply damage similarly:  
        // - always same base damage before applying custom stat buffs. Game's own damage sources can depend on
        // upgrades and additional buffs which should game should be let to handle internally.
        // - it doesn't have subtypes like Needle's Up/Down/normal Slash, Silk Skill's different skill, Tool's type etc.
        // which could add separate damage checks.
        // => For this reason, there's no reason for an extra ApplyTauntDamage method. Same goes for TauntRange.
        // This also allows the secondary info to directly display the final damage.

        /// <summary>
        /// Taunt aura damage.
        /// </summary>
        public static int WeaverAuraDamage(int totalInt, bool isGUI = false, bool GUISilkBonus = false)
        {
            int damage = WeaverAuraBaseDamage;

            if (totalInt <= 0) return 0;
            if (totalInt == 1) return damage;

            damage += (int)Math.Floor(totalInt * 0.5f);

            // game has already deducted one silk at this point, apply the damage bonus with initial silk value.
            // however GUI must access the actual silk amount
            int currentSilk = !isGUI ? PlayerData.instance.silk + 1 : PlayerData.instance.silk;

            if (IsVoidBurstEnabled)
            {
                if (isGUI)
                {
                    damage += currentSilk * BurstSilkDamageMultiplier;
                }
                else
                {
                    // this has to use the total silk before Pale Burst wiped all of it, PlayerData.silk is already
                    // using the current amount.
                    damage += LevelingPatch.TotalSilkCache * BurstSilkDamageMultiplier;
                    LevelingPatch.TotalSilkCache = 0;
                }
            }
            else if (WeaverAuraRingTaunt || (isGUI && GUISilkBonus))
            {
                damage += currentSilk * 3;
            }

            if (!isGUI)
                Bonfire.Log.LogDebug($"[IntelligenceStat] Challenge taunt final damage: {damage}");

            return damage;
        }

        /// <summary>
        /// Taunt aura range.
        /// </summary>
        public static float WeaverAuraRange(int totalInt, bool isGUI = false)
        {
            if (totalInt <= 0) return 0;

            float range = WeaverAuraBaseRange + (int)(0.15 * totalInt);

            if (IsVoidBurstEnabled)
            {
                range += BurstRangeIncrease;
            }
            else if (WeaverAuraRingTaunt)
            {
                range *= WeaverAuraRingTauntRangeMultiplier;
            }

            if (!isGUI)
                Bonfire.Log.LogDebug($"[IntelligenceStat] Challenge taunt range: {range}");

            return range;
        }

        /// <summary>
        /// Apply silk skill damage bonus.
        /// </summary>
        public static int ApplySpellDamageBonuses(int baseDamage, ToolItem spell)
        {
            Bonfire.Log.LogDebug($"[IntelligenceStat]");
            Bonfire.Log.LogDebug($"<Vanilla> Silk Skill: {spell}. Damage: {baseDamage}");

            int updatedDamage = baseDamage;

            if (!ResilienceStat.IsInBlueState(BlueState.GLASSCANNON))
                updatedDamage = SpellDamage(baseDamage, Bonfire.Instance.Status.IntelligenceStat);

            if (CheckWeakBindRequirement())
            {
                updatedDamage = (int)Math.Round(updatedDamage * 1.5f);
            }

            Bonfire.Log.LogDebug($"<Modded> Silk Skill: {spell}. Damage: {updatedDamage}");

            return updatedDamage;
        }

        /// <summary>
        /// Apply Claw Mirror binding effect damage bonuses.
        /// </summary>
        public static int ApplyClawMirrorDamageBonuses(int baseDamage, ToolItem tool)
        {
            if (tool.name != "Dazzle Bind" && tool.name != "Dazzle Bind Upgraded") return baseDamage;

            Bonfire.Log.LogDebug($"[IntelligenceStat]");
            Bonfire.Log.LogDebug($"<Vanilla> Claw Mirror damage: {baseDamage}");

            int updatedDamage = baseDamage;

            if (CheckIntelligenceMilestone(4))
            {
                updatedDamage += (int)Math.Floor(Bonfire.Instance.Status.IntelligenceStat * 0.5f);
                Bonfire.Log.LogDebug("Applied int scaling");
            }

            if (CheckWeakBindRequirement())
            {
                updatedDamage = (int)Math.Round(updatedDamage * 1.5f);
                Bonfire.Log.LogDebug("Applied weak binding bonus");
            }

            Bonfire.Log.LogDebug($"<Modded> Claw Mirror damage: {updatedDamage}");

            return updatedDamage;
        }

        /// <summary>
        /// Deal damage to enemies using Challenge Taunt aura effect.
        /// </summary>
        public static bool DealWeaverAuraDamage()
        {
            Bonfire.Log.LogDebug("[IntelligenceStat] Weaver Aura Hit Detection");

            var hc = HeroController.instance;
            bool didHit = false;

            float range = WeaverAuraRange(Bonfire.Instance.Status.IntelligenceStat);

            foreach (var e in EnemyHealth.EnemyMaxHp.Keys)
            {
                if (e == null)
                    continue;

                float dist = Vector2.Distance(hc.transform.position, e.transform.position);
                if (dist - WeaverAuraRangeOffset > range)
                    continue;

                HealthManager hm = e.GetComponent<HealthManager>();

                if (hm == null)
                    continue;

                HitInstance hit = CreateWeaverAuraHitInstance(hc);

                hm.Hit(hit);

                didHit = true;
            }

            return didHit;
        }

        /// <summary>
        /// HeroController.Update (Postfix): Procs the weaver aura effect when enabled, handles removal and adding
        /// of silk, and displays the range visual.
        /// </summary>
        public static void CheckWeaverAura(float dt)
        {
            if (!WeaverAuraRingTaunt && !WeaverAuraTaunt) return;

            if (UseVoidBurst()) return;

            if (Bonfire.Instance.Status.IntelligenceStat < IntelligenceMilestone(1)) return;

            if (_weaverAuraCooldown > 0)
            {
                _weaverAuraCooldown -= dt;
            }
            else
            {
                int silk = PlayerData.instance.silk;
                var hc = HeroController.instance;

                if (silk <= 0)
                {
                    _weaverAuraCooldown = 0;
                    WeaverAuraTaunt = false;
                    WeaverAuraRingTaunt = false;
                }
                else if (WeaverAuraTaunt && silk >= 1)
                {
                    // cost of 3 means conditions for Egg of Flealia are valid, otherwise take silk normally
                    if (LevelingPatch.CustomSilkSkillCostGetter() != 3)
                        hc.TakeSilk(1);

                    if (DealWeaverAuraDamage() && CheckWeakBindRequirement())
                        hc.AddSilk(1, false);

                    if (IsWeaverAuraEnabled && Bonfire.Instance.Status.ShowCustomRangeIndicator)
                    {
                        RangeIndicator.Display(
                            WeaverAuraRange(Bonfire.Instance.Status.IntelligenceStat),
                            null,
                            null);
                    }

                    _weaverAuraCooldown = 1f;

                    Bonfire.Log.LogDebug("[IntelligenceStat] Weaver Aura: Taunted");
                }
                else if (WeaverAuraRingTaunt && silk >= 2)
                {
                    if (LevelingPatch.CustomSilkSkillCostGetter() != 3)
                        hc.TakeSilk(2);
                    else
                        hc.TakeSilk(1);

                    if (DealWeaverAuraDamage() && CheckWeakBindRequirement())
                        hc.AddSilk(1, false);

                    if (IsWeaverAuraEnabled && Bonfire.Instance.Status.ShowCustomRangeIndicator)
                    {
                        RangeIndicator.Display(
                            WeaverAuraRange(Bonfire.Instance.Status.IntelligenceStat),
                            null,
                            null);
                    }

                    _weaverAuraCooldown = 1f;

                    Bonfire.Log.LogDebug("[IntelligenceStat] Weaver Aura: RingTaunted");
                }
                else
                {
                    _weaverAuraCooldown = 0;
                }
            }
        }

        public static void ResetWeaverAuraTimer() => _weaverAuraCooldown = 0;

        /// <summary>
        /// Checks if conditions for weak bind are true. Weak bind reduces bind healing to 1, but also makes 
        /// it cost 1/3 of original, making it similar to Hollow Knight's focus except the duration uses the base value 
        /// (which of course can be reduced).
        /// </summary>
        public static bool CheckWeakBindRequirement()
        {
            if (CheckIntelligenceMilestone(2) && Bonfire.Instance.Status.WeakBindEnabled)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Override binding FSM values. These includes:
        /// - bind cost gameplay value in FSM, UpdateBindCostVisual should be called inside the patcher that 
        /// handles silk spool visuals.
        /// - bind duration.
        /// </summary>
        public static void UpdateBindFSMValues()
        {
            // BindCostInit is just to prevent calling Fsm event search loop continuously: _bindCostInit starts 
            // false, which forces loop to run once and initialize "Silk Cost" and "Silk Cost Witch" int values when 
            // loading into a save. Afterwards it should only repeat this if player is currently binding. And finally: 
            // going back to main menu should reset init value so that init may be repeated after each "save & exit 
            // into load into game" sequence. 
            // Is this absolutely necessary? No, probably doesn't even affect game performance to be honest, but 
            // just a good habit to prevent a pointless execution loop because UpdateBindCostFSM is called on each
            // frame.

            if (!HeroController.instance.cState.isBinding && BindCostInit)
            {
                _startedBinding = false;

                return;
            }

            BindCostInit = true;

            if (_startedBinding)
                return;

            _startedBinding = true;
            ExtraBind = false;

            if (HeroController.instance == null)
                return;

            FsmInt silkCost;
            FsmInt silkCostWitch;
            FsmFloat bindTime;
            FsmInt bindAmount;

            int cost = BindCost(Bonfire.Instance.Status.IntelligenceStat);
            if (cost < 1)
            {
                BindCostInit = true;
                return;
            }

            foreach (var fsm in HeroController.instance.GetComponents<PlayMakerFSM>())
            {
                if (fsm.FsmName != "Bind")
                    continue;

                Bonfire.Log.LogDebug("[IntelligenceStat] Update bind FSM values");

                // cost
                silkCost = fsm.FsmVariables.GetFsmInt("Silk Cost");
                silkCostWitch = fsm.FsmVariables.GetFsmInt("Silk Cost Witch");

                if (silkCost != null)
                {
                    if (CheckWeakBindRequirement())
                    {
                        silkCost.Value = (int)Math.Round(cost * 0.33f);
                    }
                    else
                    {
                        silkCost.Value = cost;
                    }
                }

                if (silkCostWitch != null)
                {
                    if (CheckWeakBindRequirement())
                    {
                        silkCostWitch.Value = (int)Math.Round(cost * 0.33f);
                    }
                    else
                    {
                        silkCostWitch.Value = cost;
                    }
                }

                Bonfire.Log.LogDebug("Applied cost reduction:");
                Bonfire.Log.LogDebug($"- Normal: {silkCost}");
                Bonfire.Log.LogDebug($"- Witch:{silkCostWitch}");

                // amount of binds in succession (e.g. default 1, Multibinder sets this to 2)
                bindAmount = fsm.FsmVariables.GetFsmInt("Bind Amount");
                if (bindAmount != null)
                {
                    if (CheckWeakBindRequirement() &&
                        (ToolItemManager.IsToolEquipped("Dazzle Bind") ||
                        ToolItemManager.IsToolEquipped("Dazzle Bind Upgraded"))
                        )
                    {
                        bindAmount.Value += 1;
                        ExtraBind = true;

                        Bonfire.Log.LogDebug($"Updated bind amount to {bindAmount.Value}");
                    }
                }

                // - OBSOLETE, better to directly patch HeroController.AddHealth as this doesn't work Multibinder tool's
                // heal amount.
                /*
                healAmount = fsm.FsmVariables.GetFsmInt("Heal Amount");
                if (healAmount != null)
                {
                    if (CheckWeakBindRequirement())
                    {
                        healAmount = (int)Math.Round(cost * 0.33f);

                        Bonfire.Log.LogDebug($"Heal amount reduced, amount={healAmount.Value}");
                    }
                }
                */

                // duration
                bindTime = fsm.FsmVariables.GetFsmFloat("Bind Time");

                if (bindTime != null)
                {
                    if (CheckIntelligenceMilestone(3) &&
                    (ToolItemManager.IsToolEquipped("Dazzle Bind") ||
                    ToolItemManager.IsToolEquipped("Dazzle Bind Upgraded")))
                    {
                        bindTime.Value *= 0.67f;
                    }

                    if (CheckWeakBindRequirement())
                    {
                        bindTime.Value *= 0.67f;
                    }

                    Bonfire.Log.LogDebug($"Applied duration reduction, duration={bindTime.Value}");
                }
            }
        }

        /// <summary>
        /// SilkSpool.BindCost (Postfix): Update silk spool with modified bind cost. To change the internal cost
        /// value, be sure to also use UpdateBindCostFSM.
        /// </summary>
        public static float UpdateBindCostVisual(float result)
        {
            if (PlayerData.instance.IsAnyCursed) return result;

            int cost = BindCost(Bonfire.Instance.Status.IntelligenceStat);

            if (CheckWeakBindRequirement())
                cost = (int)Math.Round(cost * 0.33f);

            return cost;
        }


        // offset: this is to make visual range better match with enemy hit position. In other words, the damage range 
        // is slightly longer than visual indicator.
        private const float WeaverAuraRangeOffset = 0.2f;
        private const float WeaverAuraRingTauntRangeMultiplier = 0.5f;
        private const int BurstSilkDamageMultiplier = 6;
        private const float BurstRangeIncrease = 5f;

        private static bool _startedBinding = false;
        private static float _weaverAuraCooldown = 0f;

        private static HitInstance CreateWeaverAuraHitInstance(HeroController instance)
        {
            instance.gameObject.name = "Bonfire-WeaverAura";

            return new HitInstance
            {
                DamageDealt = WeaverAuraBaseDamage,
                Source = instance.gameObject,
                AttackType = AttackTypes.Generic,
                IsFirstHit = false,
                HitEffectsType = EnemyHitEffectsProfile.EffectsTypes.Full,
                Direction = 0f, // hit angle, doesn't matter because no knockback or directional damage bonuses needed
                MagnitudeMultiplier = 0f, // has to be 0 for knockback removal
                Multiplier = 1f,
                IsHeroDamage = true
            };
        }

        private static bool UseVoidBurst()
        {
            if (!IsVoidBurstEnabled) return false;

            int silk = PlayerData.instance.silk;
            var hc = HeroController.instance;

            hc.TakeSilk(silk);

            if (DealWeaverAuraDamage() && CheckWeakBindRequirement())
                hc.AddSilk(1, false);

            if (Bonfire.Instance.Status.ShowCustomRangeIndicator)
            {
                RangeIndicator.Display(
                    WeaverAuraRange(Bonfire.Instance.Status.IntelligenceStat),
                    null,
                    new Color(0.2f, 0.2f, 0.4f));
            }

            SpawnPrefab.SpawnHero(HeroController.instance.takeHitDoubleBlackThreadEffectPrefab);
            HeroController.instance.GetComponent<SpriteFlash>().FlashShadowRecharge();

            _weaverAuraCooldown = 0;
            WeaverAuraTaunt = false;
            WeaverAuraRingTaunt = false;

            Bonfire.Log.LogDebug("[IntelligenceStat] Pale Burst used");

            return true;
        }
    }
}