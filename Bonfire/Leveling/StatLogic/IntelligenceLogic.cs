using System;

using GlobalSettings;
using HutongGames.PlayMaker;
using UnityEngine;

namespace BonfireSilksong
{
    public static class IntelligenceLogic
    {
        public const int TauntBaseDamage = 5;

        public static bool BindCostInit = false;

        public static int IntelligenceMilestone(int m)
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

        public static bool CheckIntelligenceMilestone(int m)
        {
            var s = Bonfire.Instance.Status.IntelligenceStat + LevelingSystem.MilestoneBonus;
            return s >= IntelligenceMilestone(m);
        }

        /// <summary>
        /// Silk skill damage.
        /// </summary>
        public static int SpellDamage(int baseDamage, int totalInt)
        {
            return (int)Math.Round(baseDamage * Math.Pow(1.25, Math.Log(totalInt + 1, 2.0)));
        }

        /// <summary>
        /// Binding (healing) cost.
        /// In HK, soul container has range 0-99, focus costs 33 soul.
        /// In HK:SS, silk container has range 0-9, with all upgrades reaching 0-18, binding costs 3 silk.
        /// In HK, range is then divided by 33, in Silksong by 9.
        /// => Silksong scaling is not as smooth, but ramps a bit harder on high intelligence.
        /// </summary>
        public static int BindCost(int totalInt)
        {
            return (int)Math.Round(9.1 * Math.Exp(-0.01 * (totalInt + 2.0)));
        }

        // -- secondary --
        // Taunt is a custom HitInstance that will always apply damage similarly:  
        // - always same base damage before applying custom stat buffs. Game's own damage sources can depend on
        // upgrades and additional buffs which should game should be let to handle internally.
        // - it doesn't have subtypes like Needle's Up/Down/normal Slash, Silk Skill's different skill, Tool's type etc.
        // which could add separate damage checks.
        // => For this reason, there's no reason for an extra ApplyTauntDamage method. Same goes for TauntRange.
        // This also allows the secondary info to directly display the final damage.

        /// <summary>
        /// Challenge Taunt damage.
        /// </summary>
        public static int TauntDamage(int totalInt, bool isGUI = false)
        {
            int damage = TauntBaseDamage;

            if (totalInt < 0) return 0;
            if (totalInt == 1) return damage;

            if (totalInt <= 10)
            {
                damage += (int)Math.Floor(totalInt * 0.5f);
            }
            else
            {
                damage += 5 + totalInt - 10;
            }

            if (CheckIntelligenceMilestone(4))
            {
                // game has already deducted one silk at this point, apply the damage bonus with initial silk value.
                // however GUI must access the actual silk amount
                int currentSilk = !isGUI ? PlayerData.instance.silk + 1 : PlayerData.instance.silk;
                damage += currentSilk * 5;
            }

            if (!isGUI)
                Bonfire.Log.LogDebug($"[IntelligenceLogic] Challenge taunt final damage: {damage}");

            return damage;
        }

        /// <summary>
        /// Challenge Taunt range.
        /// </summary>
        public static float TauntRange(int totalInt, bool isGUI = false)
        {
            float range = 5f + (int)(0.15 * totalInt);

            if (CheckWeakBindRequirement())
                range *= 1.5f;

            if (!isGUI)
                Bonfire.Log.LogDebug($"[IntelligenceLogic] Challenge taunt range: {range}");

            return range;
        }

        /// <summary>
        /// Apply silk skill damage bonus.
        /// </summary>
        public static int ApplySpellDamageBonuses(int baseDamage, ToolItem spell)
        {
            Bonfire.Log.LogDebug($"[IntelligenceLogic]");
            Bonfire.Log.LogDebug($"<Vanilla> Silk Skill: {spell}. Damage: {baseDamage}");

            int updatedDamage = SpellDamage(baseDamage, Bonfire.Instance.Status.IntelligenceStat);

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

            Bonfire.Log.LogDebug($"[IntelligenceLogic]");
            Bonfire.Log.LogDebug($"<Vanilla> Claw Mirror damage: {baseDamage}");

            int updatedDamage = baseDamage;

            if (CheckIntelligenceMilestone(4))
            {
                updatedDamage = SpellDamage(baseDamage, Bonfire.Instance.Status.IntelligenceStat);
                Bonfire.Log.LogDebug("Applied int scaling");
            }

            if (CheckWeakBindRequirement())
            {
                updatedDamage = (int)Math.Round(updatedDamage * 1.5f);
                Bonfire.Log.LogDebug("Applied weak binding bonus");
            }

            Bonfire.Log.LogDebug($"<Modded> Claw Mirror damage: {baseDamage}");

            return updatedDamage;
        }

        /// <summary>
        /// Deal damage to enemies using Challenge Taunt.
        /// </summary>
        public static void DealTauntDamage()
        {
            Bonfire.Log.LogDebug("[IntelligenceLogic] Used Challenge Taunt ability");

            if (!CheckIntelligenceMilestone(1))
                return;

            var hc = HeroController.instance;

            float range = TauntRange(Bonfire.Instance.Status.IntelligenceStat);

            foreach (var e in EnemyHealth.EnemyMaxHp.Keys)
            {
                if (e == null)
                    continue;

                float dist = Vector2.Distance(hc.transform.position, e.transform.position);
                if (dist > range)
                    continue;

                HealthManager hm = e.GetComponent<HealthManager>();

                if (hm == null)
                    continue;

                HitInstance hit = CreateTauntHitInstance(hc);

                hm.Hit(hit);
            }
        }

        /// <summary>
        /// Checks if conditions for weak bind are true. Weak bind reduces bind healing to 1, but also makes 
        /// it cost 1/3 of original, making it similar to Hollow Knight's focus except the duration uses the base value 
        /// (which of course can be reduced).
        /// </summary>
        public static bool CheckWeakBindRequirement()
        {
            // current condition: equip Dead Bug's Purse or Shell Satchel if in Steel Soul mode.
            if ((Gameplay.DeadPurseTool.IsEquipped || Gameplay.ShellSatchelTool.IsEquipped) &&
                CheckIntelligenceMilestone(2))
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

            if (!GameManager.instance.hero_ctrl.cState.isBinding && BindCostInit)
            {
                _startedBinding = false;

                return;
            }

            BindCostInit = true;

            if (_startedBinding)
                return;

            _startedBinding = true;

            if (HeroController.instance == null)
                return;

            FsmInt silkCost;
            FsmInt silkCostWitch;
            FsmInt healAmount;
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

                Bonfire.Log.LogDebug("[IntelligenceLogic] Update bind FSM values");

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
                    if (CheckWeakBindRequirement())
                    {
                        bindAmount.Value += 1;

                        Bonfire.Log.LogDebug($"Updated bind amount to {bindAmount.Value}");
                    }
                }

                // heal amount
                healAmount = fsm.FsmVariables.GetFsmInt("Heal Amount");
                if (healAmount != null)
                {
                    if (CheckWeakBindRequirement())
                    {
                        healAmount = (int)Math.Round(cost * 0.33f);

                        Bonfire.Log.LogDebug($"Heal amount reduced, amount={healAmount.Value}");
                    }
                }

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
        /// SilkSpool.BindCost (Postfix): Update silk spool with modified binding cost. To change the internal cost
        /// value, be sure to also use UpdateBindCostFSM.
        /// </summary>
        public static float UpdateBindCostVisual(float result)
        {
            if (PlayerData.instance.IsAnyCursed) return result;

            int cost = BindCost(Bonfire.Instance.Status.IntelligenceStat);

            return cost;
        }


        private static bool _startedBinding = false;

        private static HitInstance CreateTauntHitInstance(HeroController instance)
        {
            instance.gameObject.name = "Bonfire-Taunt";

            return new HitInstance
            {
                DamageDealt = TauntBaseDamage,
                Source = instance.gameObject,
                AttackType = AttackTypes.Generic,
                IsFirstHit = false,
                HitEffectsType = EnemyHitEffectsProfile.EffectsTypes.Full,
                Direction = 0f,
                MagnitudeMultiplier = 1f,
                Multiplier = 1f,
                IsHeroDamage = true
            };
        }
    }
}