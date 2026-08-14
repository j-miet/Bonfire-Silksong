using System;

using HutongGames.PlayMaker;

namespace BonfireSilksong
{
    public static class IntelligenceLogic
    {
        public static bool BindCostInit = false;

        public static int IntelligenceMilestone_1 => 1;
        public static int IntelligenceMilestone_2 => 2;
        public static int IntelligenceMilestone_3 => 3;
        public static bool CheckIntelligenceMilestone_1 =>
            Bonfire.Instance.Status.IntelligenceStat >= IntelligenceMilestone_1;
        public static bool CheckIntelligenceMilestone_2 =>
            Bonfire.Instance.Status.IntelligenceStat >= IntelligenceMilestone_2;
        public static bool CheckIntelligenceMilestone_3 =>
            Bonfire.Instance.Status.IntelligenceStat >= IntelligenceMilestone_3;

        // Stat scaling: silk skill damage
        public static int SpellDamage(int baseDamage, int totalInt)
        {
            return (int)Math.Round(baseDamage * Math.Pow(1.25, Math.Log(totalInt + 1, 2.0)));
        }

        // Stat scaling: binding (healing) cost.
        // In HK, soul container has range 0-99, focus costs 33 soul.
        // In HK:SS, silk container has range 0-9, with all upgrades reaching 0-18, binding costs 3 silk.
        // In Hk, range is then divided by 33, in Silksong by 9.
        // => Silksong scaling is not as smooth, but ramps a bit harder on high intelligence.
        public static int BindCost(int totalInt)
        {
            return (int)Math.Round(9.1 * Math.Exp(-0.01 * (totalInt + 2.0)));
        }

        /// <summary>
        /// Apply silk skill damage bonus.
        /// </summary>
        public static int ApplySpellDamageBonuses(int baseDamage, ToolItem spell)
        {
            Bonfire.Log.LogDebug($"[IntelligenceLogic]");
            Bonfire.Log.LogDebug($"<Vanilla> Silk Skill: {spell}. Damage: {baseDamage}");

            int updatedDamage = SpellDamage(baseDamage, Bonfire.Instance.Status.IntelligenceStat);

            Bonfire.Log.LogDebug($"<Modded> Silk Skill: {spell}. Damage: {updatedDamage}");

            return updatedDamage;
        }

        /// <summary>
        /// Override bind cost gameplay value in FSM.
        /// UpdateBindCostVisual should be called inside the patcher that handles silk spool visuals.
        /// </summary>
        public static void UpdateBindCostFSM()
        {
            // BindCostInit is just to prevent calling Fsm event search loop continuously: _bindCostInit starts 
            // false, which forces loop to run once and initialize "Silk Cost" and "Silk Cost Witch" int values when 
            // loading into a save. Afterwards it should only repeat this if player is currently binding. And finally: 
            // going back to main menu should reset init value so that init may be repeated after each "save & exit 
            // into load into game" sequence. 
            // Is this absolutely necessary? No, probably doesn't even affect game performance to be honest, but 
            // just a good habit to prevent a pointless execution loop because UpdateBindCostFSM is called on each
            // frame.

            if (!GameManager.instance.hero_ctrl.cState.isBinding && BindCostInit) return;

            if (HeroController.instance == null) return;

            FsmInt silkCost;
            FsmInt silkCostWitch;
            int cost = BindCost(Bonfire.Instance.Status.IntelligenceStat);

            foreach (var fsm in HeroController.instance.GetComponents<PlayMakerFSM>())
            {
                if (fsm.FsmName != "Bind") continue;

                silkCost = fsm.FsmVariables.GetFsmInt("Silk Cost");
                silkCostWitch = fsm.FsmVariables.GetFsmInt("Silk Cost Witch");

                if (silkCost != null)
                    silkCost.Value = cost;

                if (silkCostWitch != null)
                    silkCostWitch.Value = cost;

                BindCostInit = true;
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
    }
}