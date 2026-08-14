using System;

using UnityEngine;

namespace BonfireSilksong
{
    public static class ArtificeLogic
    {
        public static int ArtificeMilestone_1 => 1;
        public static int ArtificeMilestone_2 => 2;
        public static int ArtificeMilestone_3 => 3;
        public static bool CheckArtificeMilestone_1 => Bonfire.Instance.Status.ArtificeStat >= ArtificeMilestone_1;
        public static bool CheckArtificeMilestone_2 => Bonfire.Instance.Status.ArtificeStat >= ArtificeMilestone_2;
        public static bool CheckArtificeMilestone_3 => Bonfire.Instance.Status.ArtificeStat >= ArtificeMilestone_3;

        // Stat scaling: tool damage
        public static int ToolDamage(int baseDamage, int totalArt)
        {
            if (totalArt <= 0) return baseDamage;

            return (int)Math.Round(baseDamage * (1.0 + 2.0 / (1.0 + 15.0 * Math.Exp(-0.1 * totalArt))));
        }

        // Stat scaling: amount of time it takes to passively replenish a single tool charge (without costing any 
        // shards). Starts from 60 seconds timer at lvl 5: this prevents player from putting just a single point for 
        // such an impactful upgrade
        public static int ToolRegenInterval(int totalArt)
        {
            if (totalArt < 5) return -1;

            return (int)(2.0 + 58.0 * Math.Exp(-0.06 * (totalArt - 5)));
        }

        /// <summary>
        /// Apply tool damage bonus.
        /// </summary>
        public static int ApplyToolDamageBonuses(int baseDamage, ToolItem tool)
        {
            Bonfire.Log.LogDebug($"[ArtificeLogic]");
            Bonfire.Log.LogDebug($"<Vanilla> Tool: {tool}. Damage: {baseDamage}");

            int updatedDamage = ToolDamage(baseDamage, Bonfire.Instance.Status.ArtificeStat);

            Bonfire.Log.LogDebug($"<Modded> Tool: {tool}. Damage: {updatedDamage}");

            return updatedDamage;
        }

        /// <summary>
        /// HeroController.Update (Postfix): Passive tool regen, replenishes charges for ALL red tools 
        /// currently equipped
        /// </summary>
        public static void CheckToolRegen(float dt)
        {
            if (_toolRegenTime > 0)
            {
                _toolRegenTime -= dt;
            }
            else
            {
                Bonfire.Log.LogDebug("[ArtificeLogic] Checking tool regen conditions");
                int toolRegenInterval = ToolRegenInterval(Bonfire.Instance.Status.ArtificeStat);
                if (toolRegenInterval <= 0)
                {
                    _toolRegenTime = 1f;
                    return;
                }

                _toolRegenTime = toolRegenInterval;
                foreach (var tool in Resources.FindObjectsOfTypeAll<ToolItem>())
                {
                    if (tool.IsEquipped && tool.IsAutoReplenished() && tool.CanReload())
                        tool.CollectFree(1);
                    // tool.ReloadSingle() would do the same, but costs shards
                }

                Bonfire.Log.LogDebug("Replenished 1 charge for all red tools");
            }
        }


        private static float _toolRegenTime = 0f;
    }
}