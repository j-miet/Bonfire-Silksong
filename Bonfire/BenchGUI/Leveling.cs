using System;

using UnityEngine;

namespace BonfireSilksong
{
    /// <summary>
    /// Leveling system.
    /// </summary>
    public class Leveling
    {
        public void ResetPendingLevelUp()
        {
            var s = Bonfire.Instance.Status;
            var ls = LevelingSystem.Instance;

            ls.StrengthIncrease = 0;
            ls.DexterityIncrease = 0;
            ls.IntelligenceIncrease = 0;
            ls.ArtificeIncrease = 0;
            ls.WisdomIncrease = 0;
            ls.ResilienceIncrease = 0;
            ls.LuckIncrease = 0;

            s.PendingRosaries = 0;
            s.PendingRosaryLevels = 0;
            s.PendingRelicLevels = 0;

            Bonfire.Instance.UpdateBindCostFSM();
        }

        public void DrawLevelingUI()
        {
            var s = Bonfire.Instance.Status;
            var ls = LevelingSystem.Instance;

            int availableRelicLevels = ls.CachedRelicsObtained - s.ConsumedRelicLevels
                                     + s.RespecRelicLevels - s.PendingRelicLevels;
            bool gotFreeLevel = availableRelicLevels > 0;

            int rosaryLevels = s.TotalRosaryLevels + s.PendingRosaryLevels;
            int rosariesToLvlUp = (int)(Math.Pow(rosaryLevels, 2.0) + (10 * rosaryLevels) + 50.0);

            int currentLvl = s.StrengthStat + s.DexterityStat + s.IntelligenceStat
                        + s.ArtificeStat + s.ResilienceStat + s.WisdomStat + s.LuckStat
                        + s.PendingRelicLevels
                        + s.PendingRosaryLevels
                        + 1; // base level is 1

            // preview strings
            string totalStr = (s.StrengthStat + ls.StrengthIncrease).ToString();
            string totalDex = (s.DexterityStat + ls.DexterityIncrease).ToString();
            string totalInt = (s.IntelligenceStat + ls.IntelligenceIncrease).ToString();
            string totalArt = (s.ArtificeStat + ls.ArtificeIncrease).ToString();
            string totalRes = (s.ResilienceStat + ls.ResilienceIncrease).ToString();
            string totalWsdm = (s.WisdomStat + ls.WisdomIncrease).ToString();
            string totalLck = (s.LuckStat + ls.LuckIncrease).ToString();

            // stat calculations
            string needleDamage = ls.NeedleDamage(s.StrengthStat + ls.StrengthIncrease).ToString();

            string attackSpeed = ls.AttackSpeed(s.DexterityStat + ls.DexterityIncrease).ToString();
            string critDamage = ls.CritDamage(s.DexterityStat + ls.DexterityIncrease, 100).ToString();

            string spellDamage = ls.SpellDamage(100, s.IntelligenceStat + ls.IntelligenceIncrease).ToString();
            string bindCost = ls.BindCost(s.IntelligenceStat + ls.IntelligenceIncrease).ToString();

            string toolDamage = ls.ToolDamage(100, s.ArtificeStat + ls.ArtificeIncrease).ToString();
            string toolRegenInterval = "-";
            if (s.ArtificeStat + ls.ArtificeIncrease > 0)
            {
                int toolRegen = ls.ToolRegenInterval(s.ArtificeStat + ls.ArtificeIncrease);
                toolRegenInterval = toolRegen > 0 ? toolRegen.ToString() + " s" : "-";
            }

            string extraMasks = ls.ExtraMasks(s.ResilienceStat + ls.ResilienceIncrease).ToString();
            string blockChance = s.ResilienceStat + ls.ResilienceIncrease > 0
                ? ls.AverageBlockChance(s.ResilienceStat + ls.ResilienceIncrease).ToString("F0")
                : "0";

            string extraSilk = ls.ExtraSilk(s.WisdomStat + ls.WisdomIncrease, 1).ToString();
            string silkRegenInterval = "-";
            if (s.WisdomStat + ls.WisdomIncrease > 0)
            {
                int silkRegen = ls.SilkRegenInterval(s.WisdomStat + ls.WisdomIncrease);
                silkRegenInterval = silkRegen > 0 ? silkRegen.ToString() + " s" : "-";
            }

            string critChance = ls.CritChance(s.LuckStat + ls.LuckIncrease).ToString();
            string rosaryIncrease = (5 * (s.LuckStat + ls.LuckIncrease)).ToString();

            // apply button label
            string applyText;
            if (s.PendingRelicLevels > 0 && s.PendingRosaries > 0)
            {
                applyText = $"Apply ({s.PendingRosaries} rosaries and {s.PendingRelicLevels} relics)";
            }
            else if (s.PendingRelicLevels > 0)
            {
                applyText = $"Apply ({s.PendingRelicLevels} relic levels)";
            }
            else if (gotFreeLevel)
            {
                applyText = $"{availableRelicLevels} Free Levels available!";
            }
            else
            {
                applyText = $"Apply ({s.PendingRosaries} rosaries)";
            }

            // layout
            GUILayout.BeginArea(new Rect(20f, Screen.height / 4.25f, 530f, 750f));
            GUILayout.Label("Leveling", Styles._levelHeaderStyle);

            // stats
            GUILayout.BeginHorizontal("box");

            GUILayout.BeginVertical("box");
            DrawStatButton("Strength", totalStr, gotFreeLevel, s, rosariesToLvlUp);
            DrawStatButton("Dexterity", totalDex, gotFreeLevel, s, rosariesToLvlUp);
            DrawStatButton("Intelligence", totalInt, gotFreeLevel, s, rosariesToLvlUp);
            DrawStatButton("Artifice", totalArt, gotFreeLevel, s, rosariesToLvlUp);
            DrawStatButton("Resilience", totalRes, gotFreeLevel, s, rosariesToLvlUp);
            DrawStatButton("Wisdom", totalWsdm, gotFreeLevel, s, rosariesToLvlUp);
            DrawStatButton("Luck", totalLck, gotFreeLevel, s, rosariesToLvlUp);
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            DrawStatLabel("Needle Damage: " + needleDamage);
            DrawStatLabel("Slash Speed: " + attackSpeed);
            DrawStatLabel("Silk Skill Damage: " + spellDamage + "%");
            DrawStatLabel("Tool Damage: " + toolDamage + "%");
            DrawStatLabel("Extra Lifeblood Masks: " + extraMasks);
            DrawStatLabel("Silk Gained On Hit: " + extraSilk);
            DrawStatLabel("Crit Chance: " + critChance + "%");
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            DrawStatLabel("");   // spacer to align with column 1
            DrawStatLabel("Crit Damage: " + critDamage + "%");
            DrawStatLabel("Binding Cost: " + bindCost);
            DrawStatLabel("Tool Regen interval: " + toolRegenInterval);
            DrawStatLabel("Block Chance: " + blockChance + "%");
            DrawStatLabel("Silk Regen interval: " + silkRegenInterval);
            DrawStatLabel("Rosary + Shard Increase: " + rosaryIncrease + "%");
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.Label("Current Level: " + currentLvl, Styles._labelStyle);
            GUILayout.Label("Rosaries to Level Up: " + rosariesToLvlUp, Styles._labelStyle);

            // apply and cancel
            GUILayout.BeginHorizontal("box");

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button(applyText, Styles._buttonStyle, GUILayout.Height(40f), GUILayout.Width(258f))
                && PlayerData.instance.atBench)
            {
                LevelingSystem.Instance.ApplyLevel();
            }

            GUILayout.FlexibleSpace();

            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("Cancel", Styles._buttonStyle, GUILayout.Height(40f), GUILayout.Width(258f))
                && PlayerData.instance.atBench)
            {
                ResetPendingLevelUp();
            }

            GUILayout.EndHorizontal();

            // respec
            GUILayout.BeginHorizontal("box");
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button(
                    $"Respec ({s.RespecCost} " + $"{(s.RespecCost > 1 ? "Silkeaters" : "Silkeater")})",
                    Styles._buttonStyle,
                    GUILayout.Height(40f),
                    GUILayout.Width(522f))
                && PlayerData.instance.atBench)
            {
                LevelingSystem.Instance.Respec();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }


        private bool CanLevelUp(bool gotFreeLevel, int rosariesToLvlUp)
        {
            var s = Bonfire.Instance.Status;
            return PlayerData.instance.atBench &&
                   (gotFreeLevel || (rosariesToLvlUp + s.PendingRosaries <= PlayerData.instance.geo));
        }

        private void DrawStatButton(string statName, string displayValue, bool gotFreeLevel, PlayerStatus s,
                                   int rosariesToLvlUp)
        {
            if (GUILayout.Button(
                    $"{statName}: {displayValue}",
                    Styles._buttonStyle,
                    GUILayout.Height(40f),
                    GUILayout.Width(160f))
                && CanLevelUp(gotFreeLevel, rosariesToLvlUp)
                && PlayerData.instance.atBench)
            {
                LevelingSystem.Instance.IncreaseStat(statName, s, rosariesToLvlUp);
            }
        }

        private void DrawStatLabel(string text)
        {
            GUILayout.Label(text, Styles._labelStyle, GUILayout.Height(40f), GUILayout.Width(160f));
        }
    }
}