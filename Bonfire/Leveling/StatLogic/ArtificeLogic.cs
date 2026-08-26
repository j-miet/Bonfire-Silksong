using System;
using System.Linq;
using System.Reflection;

using GlobalSettings;
using UnityEngine;

namespace BonfireSilksong
{
    public static class ArtificeLogic
    {
        public static bool HitEnemyWithSawtoothCirclet = false;

        public static int ArtificeMilestone(int m)
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

        public static bool CheckArtificeMilestone(int m)
        {
            var s = Bonfire.Instance.Status.ArtificeStat;
            int b = LevelingSystem.MilestoneBonus;
            return s + b >= ArtificeMilestone(m) && s >= 1;
        }

        /// <summary>
        /// Tool damage.
        /// </summary>
        public static int ToolDamage(int baseDamage, int totalArt)
        {
            if (totalArt <= 0) return baseDamage;

            return (int)Math.Round(baseDamage * (1.0 + 2.0 / (1.0 + 15.0 * Math.Exp(-0.1 * totalArt))));
        }

        /// <summary>
        /// Amount of time it takes to passively replenish a single tool charge (without costing any shards). Starts 
        /// from 60 seconds timer at lvl 5: this prevents player from putting just a single point for such an impactful 
        /// upgrade.
        /// </summary>
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

            int updatedDamage = baseDamage;

            if (!ResilienceLogic.IsInBlueState(BlueState.GLASSCANNON))
                updatedDamage = ToolDamage(baseDamage, Bonfire.Instance.Status.ArtificeStat);

            Bonfire.Log.LogDebug($"<Modded> Tool: {tool}. Damage: {updatedDamage}");

            return updatedDamage;
        }

        /// <summary>
        /// Checks if tool is any of the Silkshot variants.
        /// </summary>
        public static bool IsSilkshotTool(ToolItem tool)
        {
            return tool != null && _silkshotNames.Contains(tool.name);
        }

        /// <summary>
        /// HeroController.Update (Postfix): Passive tool regen, replenishes 1 charge for all currently equipped red 
        /// tools.
        /// </summary>
        public static void CheckToolRegen(float dt)
        {
            if (Bonfire.Instance.Status.ArtificeStat < 1) return;

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

        /// <summary>
        /// HeroController.CanThrowTool (Prefix): Check if a tool requiring specific checks can be used e.g. Silkshot
        /// can ignore the initial silk cost check.
        /// </summary>
        public static bool CanUseTool(ToolItem tool)
        {
            if (IsSilkshotTool(tool) && CheckArtificeMilestone(1))
            {
                _isUsingSilkshot = true;

                return true;
            }
            else if (tool.name == "Rosary Cannon" && CheckArtificeMilestone(2))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// HeroController.TakeSilk (Prefix): Ignore Silkshot tool silk cost.
        /// </summary>
        public static bool CheckSilkshotSilkCostRefund(int cost)
        {
            bool refunded = _isUsingSilkshot && cost == 1 && CheckArtificeMilestone(1);

            _isUsingSilkshot = false;

            return refunded;
        }

        /// <summary>
        /// HeroController.ThrowTool (Prefix): Checks if tool is valid for copying then calculates extra tool copies
        /// from Quick Sling tool + Artifice stat rolls. If tool is not valid for extra copies OR should only use
        /// Quick Sling base game behavior, returned count is 0.
        /// </summary>
        public static int GetToolCopyCount(ToolItem tool)
        {
            if (!CheckArtificeMilestone(4)) return 0;

            var t = tool;

            // Following tools can't be copied, Hornet will just lock into directional animation if copying is forced.
            // All of these except 'Lighting Rod', which is Voltvessels, wouldn't work in vanilla anyway. For
            // Voltvessels, the Bola works but Spear doesn't, latter has probably some additional logic involved 
            // because even with Quick Sling equipped it has much slower animation compared to other tools. Could try
            // to pinpoint the exact sequence later, however it's not a priority right now.
            if (IsSilkshotTool(t) ||
                t.name == "Rosary Cannon" ||
                t.name == "Screw Attack" ||
                t.name == "Flintstone" ||
                t.name == "Flea Brew" ||
                t.name == "Lifeblood Syringe" ||
                t.name == "Silk Snare" ||
                (t.name == "Lightning Rod" && !PlayerData.instance.LightningToolToggle) ||
                tool.Type == ToolItemType.Skill ||
                tool.Type == ToolItemType.Blue ||
                tool.Type == ToolItemType.Yellow)
            {
                return 0;
            }

            int copies = 0;
            _toolRefunds = 0;

            // roll for original tool
            if (UnityEngine.Random.Range(0, 3) == 0)
            {
                copies++;
                _toolRefunds += 2;

                Bonfire.Log.LogDebug("Copied current tool.");
            }

            if (Gameplay.QuickSlingTool.IsEquipped)
            {
                // roll for Quicksling secondary projectile
                if (UnityEngine.Random.Range(0, 3) == 0)
                {
                    copies++;
                    _toolRefunds += 2;

                    Bonfire.Log.LogDebug("Copied secondary projectile from Quick Sling tool.");
                }

                copies++;
            }

            return copies;
        }

        /// <summary>
        /// HeroController.ThrowTool (Postfix): Refills charges for specific tools.
        /// </summary>
        public static void CheckToolChargeRefund(ToolItem tool)
        {
            if (IsSilkshotTool(tool) && CheckArtificeMilestone(3))
            {
                tool.CollectFree(1);
            }
            else if (tool != null && tool.name == "Rosary Cannon" && CheckArtificeMilestone(2))
            {
                tool.CollectFree(1000);
            }
            else if (_toolRefunds > 0 && CheckArtificeMilestone(4))
            {
                tool.CollectFree(_toolRefunds);
                _toolRefunds = 0;
            }
        }

        /// <summary>
        /// HeroController.ThrowTool (Prefix): Get currently thrown tool in willThrowTool private field.
        /// </summary>
        public static ToolItem GetThrownToolItem()
        {
            return (ToolItem)_willThrowToolField.GetValue(HeroController.instance);
        }

        /// <summary>
        /// HeroController.ThrowTool (Postfix): Changes null willThrowTool field to passed tool + sets 
        /// queuedAutoThrowField = true to automatically throw another copy of current tool. This behavior is similar
        /// to how game handles Quick Sling tool copy spawning.
        /// </summary>
        public static void SetThrowFieldValues(ToolItem tool, bool setWillThrowTool, bool setQueuedAutoThrow)
        {
            var hc = HeroController.instance;

            if (setWillThrowTool)
                _willThrowToolField.SetValue(hc, tool);

            // this makes HeroController's Update to call ThrowTool again
            if (setQueuedAutoThrow)
                _queuedAutoThrowToolField.SetValue(hc, true);
        }

        /// <summary>
        /// ObjectPool.Spawn (Postfix): Applies visual and damage hitbox scaling to Sawtooth Circlet tool.
        /// </summary>
        public static void ScaleSawtoothCircletRange(GameObject instance)
        {
            if (instance == null || !CheckArtificeMilestone(3))
                return;

            Transform visual = instance.transform.Find(
                "Brolly_spike_position/brolly_slash_2D"
            );
            Transform damager = instance.transform.Find(
                "Brolly_spike_position/brolly_slash_enemy_damager"
            );

            // - base values -
            // Visual: (1.05, -0.94, 1.00)
            // Damager: (2.33, -0.47, 1.00)
            // => X ~ 2.33/1.05 = 2.22, Y ~ -0.47/-0.94 = 0.5, Z can remain unscaled
            //
            // Why not simply scale localScale.x etc. directly: The GameObject is pooled, meaning it is likely recycled
            // and using existing scale as a reference just ends up with stacking values over and over again. Instead
            // the localScale base values listed above are better here and avoid the constant unintended re-referencing.

            float rangeMultpilier = SawtoothCircletRangeMultiplier;

            if (visual != null)
            {
                visual.localScale = new Vector3(1.05f * rangeMultpilier, -0.94f, 1.0f);

                Bonfire.Log.LogDebug($"Brolly Spike visual scale: {visual.localScale}");
            }

            if (damager != null)
            {
                damager.localScale = new Vector3(2.33f * rangeMultpilier, -0.47f, 1.0f);

                Bonfire.Log.LogDebug($"Brolly Spike damager scale: {damager.localScale}");
            }
        }


        private const float SawtoothCircletRangeMultiplier = 1.5f;

        private static readonly string[] _silkshotNames = new[] {
            "WebShot Forge",
            "WebShot Architect",
            "WebShot Weaver"
        };

        private static int _toolRefunds = 0;
        private static bool _isUsingSilkshot = false;
        private static float _toolRegenTime = 0f;

        private static readonly FieldInfo _willThrowToolField = typeof(HeroController)
            .GetField("willThrowTool", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _queuedAutoThrowToolField = typeof(HeroController)
            .GetField("queuedAutoThrowTool", BindingFlags.Instance | BindingFlags.NonPublic);
    }
}