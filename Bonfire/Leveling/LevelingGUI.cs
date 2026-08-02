using UnityEngine;

namespace BonfireSilksong
{
    /// <summary>
    /// Leveling system GUI.
    /// </summary>
    public class LevelingGUI
    {
        public void DrawLevelingUI()
        {
            var s = Bonfire.Instance.Status;
            var ls = LevelingSystem.Instance;

            ls.CheckStatLevels();

            int availableRelicLevels = ls.GetAvailableRelicLevels();
            bool gotFreeLevel = availableRelicLevels > 0;

            int rosaryLevel = LevelingSystem.DisplayPendingRosaryLevel();
            int rosariesToLvlUp = LevelingSystem.GetLevelUpRosaryRequirement(rosaryLevel);

            int currentLvl = LevelingSystem.GetCurrentLevel();

            // preview strings
            string totalStr = (s.StrengthStat + ls.StrengthIncrease).ToString();
            string totalDex = (s.DexterityStat + ls.DexterityIncrease).ToString();
            string totalInt = (s.IntelligenceStat + ls.IntelligenceIncrease).ToString();
            string totalArt = (s.ArtificeStat + ls.ArtificeIncrease).ToString();
            string totalRes = (s.ResilienceStat + ls.ResilienceIncrease).ToString();
            string totalWsdm = (s.WisdomStat + ls.WisdomIncrease).ToString();
            string totalLck = (s.LuckStat + ls.LuckIncrease).ToString();

            // stat calculations
            string needleDamage = LevelingSystem.NeedleDamage(s.StrengthStat + ls.StrengthIncrease).ToString();

            string attackSpeed = LevelingSystem.AttackSpeed(s.DexterityStat + ls.DexterityIncrease).ToString();
            string critDamage = LevelingSystem.CritDamage(s.DexterityStat + ls.DexterityIncrease, 100).ToString();

            string spellDamage = LevelingSystem.SpellDamage(
                100, s.IntelligenceStat + ls.IntelligenceIncrease).ToString();
            string bindCost = LevelingSystem.BindCost(s.IntelligenceStat + ls.IntelligenceIncrease).ToString();

            string toolDamage = LevelingSystem.ToolDamage(100, s.ArtificeStat + ls.ArtificeIncrease).ToString();
            string toolRegenInterval = "-";
            if (s.ArtificeStat + ls.ArtificeIncrease > 0)
            {
                int toolRegen = LevelingSystem.ToolRegenInterval(s.ArtificeStat + ls.ArtificeIncrease);
                toolRegenInterval = toolRegen > 0 ? toolRegen.ToString() + " s" : "-";
            }

            string extraMasks = LevelingSystem.ExtraMasks(s.ResilienceStat + ls.ResilienceIncrease).ToString();
            string blockChance = s.ResilienceStat + ls.ResilienceIncrease > 0
                ? LevelingSystem.AverageBlockChance(s.ResilienceStat + ls.ResilienceIncrease).ToString("F0")
                : "0";

            string extraSilk = LevelingSystem.ExtraSilk(s.WisdomStat + ls.WisdomIncrease, 1).ToString();
            string silkRegenInterval = "-";
            if (s.WisdomStat + ls.WisdomIncrease > 0)
            {
                int silkRegen = LevelingSystem.SilkRegenInterval(s.WisdomStat + ls.WisdomIncrease);
                silkRegenInterval = silkRegen > 0 ? silkRegen.ToString() + " s" : "-";
            }

            string critChance = LevelingSystem.CritChance(s.LuckStat + ls.LuckIncrease).ToString();
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
            GUILayout.Label("Leveling", GUIStyles.LevelHeaderStyle);

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

            GUILayout.Label("Current Level: " + currentLvl, GUIStyles.LabelStyle);
            GUILayout.Label("Rosaries to Level Up: " + rosariesToLvlUp, GUIStyles.LabelStyle);

            // apply and cancel
            GUILayout.BeginHorizontal("box");

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button(applyText, GUIStyles.ButtonStyle, GUILayout.Height(40f), GUILayout.Width(258f))
                && PlayerData.instance.atBench)
            {
                ls.ApplyLevel();
            }

            GUILayout.FlexibleSpace();

            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("Cancel", GUIStyles.ButtonStyle, GUILayout.Height(40f), GUILayout.Width(258f))
                && PlayerData.instance.atBench)
            {
                ls.ResetPendingLevelUp();
            }

            GUILayout.EndHorizontal();

            // respec
            GUILayout.BeginHorizontal("box");
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button(
                    $"Respec ({s.RespecCost} " + $"{(s.RespecCost > 1 ? "Silkeaters" : "Silkeater")})",
                    GUIStyles.ButtonStyle,
                    GUILayout.Height(40f),
                    GUILayout.Width(522f))
                && PlayerData.instance.atBench)
            {
                ls.Respec();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }


        private void DrawStatButton(string statName, string displayValue, bool gotFreeLevel, PlayerStatus s,
                                   int rosariesToLvlUp)
        {
            if (GUILayout.Button(
                    $"{statName}: {displayValue}",
                    GUIStyles.ButtonStyle,
                    GUILayout.Height(40f),
                    GUILayout.Width(160f))
                && LevelingSystem.CanLevelUp(gotFreeLevel, rosariesToLvlUp)
                && PlayerData.instance.atBench)
            {
                LevelingSystem.Instance.IncreaseStat(statName, s, rosariesToLvlUp);
            }
        }

        private void DrawStatLabel(string text)
        {
            GUILayout.Label(text, GUIStyles.LabelStyle, GUILayout.Height(40f), GUILayout.Width(160f));
        }
    }
}