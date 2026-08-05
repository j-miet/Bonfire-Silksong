using System.Collections.Generic;

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
            _levelingAreaRectTopY = (BonfireGUI.Height / 4.25f) + 20;

            var s = Bonfire.Instance.Status;
            GUILayout.BeginArea(new Rect(
                _levelingAreaRectTopX,
                _levelingAreaRectTopY - 55,
                _levelingAreaDefaultRectWidth,
                50f));

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            string lvlStyleName = _levelingMenuStyle[Bonfire.Instance.Status.LevelingInfoStyle];

            GUI.backgroundColor = Color.white;
            if (GUILayout.Button(
                $"Style: {lvlStyleName}",
                GUIStyles.ButtonStyle,
                GUILayout.Width(250f),
                GUILayout.Height(40f))
                && PlayerData.instance.atBench)
            {
                ChangeLevelingMenuStyle();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndArea();

            DrawUIPanels();
        }

        /// <summary>
        /// BeginArea GUI component for other buttons under leveling UI. Requires a manual EndArea() call for closing!
        /// </summary>
        public void DrawOtherUIBeginArea()
        {
            GUILayout.BeginArea(new Rect(20f,
                _levelingAreaRectTopY + _levelingAreaDefaultRectHeight + _levelingAreaTextAndButtonsHeight,
                _levelingAreaDefaultRectWidth,
                200f));
        }

        // user interface measures
        private const float _levelingAreaRectTopX = 20f;
        private float _levelingAreaRectTopY;
        private const float _levelingAreaDefaultRectWidth = 530f;
        private const float _levelingAreaDefaultRectHeight = 350f;
        private const float _levelingAreaTextAndButtonsHeight = 155f;

        private const float _infoAreaRectTopX = 800f;
        private float _infoAreaRectTopY;
        private const float _infoAreaRectWidth = 999f;
        private const float _infoAreaRectHeight = 900f;
        // offset of 16f seems to prevent the horizontal scroll while fitting the 3. column's contents.
        private const float _colWidth = _infoAreaRectWidth / 3f - 16f;
        private Vector2 _infoScrollPos;

        // shared variables
        private int _availableRelicLevels;
        private bool _gotFreeLevel;
        private int _rosaryLevel;
        private int _rosariesToLvlUp;
        private int _currentLvl;

        private string _totalStr;
        private string _totalDex;
        private string _totalInt;
        private string _totalArt;
        private string _totalRes;
        private string _totalWsdm;
        private string _totalLuck;

        private string _needleDamage;
        private string _attackSpeed;
        private string _critDamage;
        private string _spellDamage;
        private string _bindCost;
        private string _toolDamage;
        private string _toolRegenInterval;
        private string _extraMasks;
        private string _blockChance;
        private string _extraSilk;
        private string _silkRegenInterval;
        private string _critChance;
        private string _rosaryIncrease;

        private string _applyText;

        // labels for leveling menu styles
        private readonly Dictionary<int, string> _levelingMenuStyle = new Dictionary<int, string>
        {
            {0, "Default"},
            {1, "Default + Info"},
            {2, "Simple"},
            {3, "Simple + Info"}
        };

        private void DrawDefaultUI()
        {
            var s = Bonfire.Instance.Status;
            var ls = LevelingSystem.Instance;

            ls.CheckStatLevels();
            UpdateGUIFields();

            // layout
            GUILayout.BeginArea(new Rect(
                _levelingAreaRectTopX,
                _levelingAreaRectTopY,
                _levelingAreaDefaultRectWidth,
                _levelingAreaDefaultRectHeight));
            GUILayout.Label("Leveling", GUIStyles.LevelHeaderStyle);

            // stats
            GUILayout.BeginHorizontal("box");

            GUILayout.BeginVertical("box");
            DrawStatButton("Strength", _totalStr, _gotFreeLevel, s, _rosariesToLvlUp);
            DrawStatButton("Dexterity", _totalDex, _gotFreeLevel, s, _rosariesToLvlUp);
            DrawStatButton("Intelligence", _totalInt, _gotFreeLevel, s, _rosariesToLvlUp);
            DrawStatButton("Artifice", _totalArt, _gotFreeLevel, s, _rosariesToLvlUp);
            DrawStatButton("Resilience", _totalRes, _gotFreeLevel, s, _rosariesToLvlUp);
            DrawStatButton("Wisdom", _totalWsdm, _gotFreeLevel, s, _rosariesToLvlUp);
            DrawStatButton("Luck", _totalLuck, _gotFreeLevel, s, _rosariesToLvlUp);
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            DrawStatLabel("Needle Damage: " + _needleDamage);
            DrawStatLabel("Slash Speed: " + _attackSpeed);
            DrawStatLabel("Silk Skill Damage: " + _spellDamage + "%");
            DrawStatLabel("Tool Damage: " + _toolDamage + "%");
            DrawStatLabel("Extra Lifeblood Masks: " + _extraMasks);
            DrawStatLabel("Silk Gained On Enemy Hit: " + _extraSilk);
            DrawStatLabel("Crit Chance: " + _critChance + "%");
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            DrawStatLabel("");   // spacer to align with column 1, because Str doesn't have a second modifier
            DrawStatLabel("Crit Damage: " + _critDamage + "%");
            DrawStatLabel("Binding Cost: " + _bindCost);
            DrawStatLabel("Tool Regen Interval: " + _toolRegenInterval);
            DrawStatLabel("Block Chance: " + _blockChance + "%");
            DrawStatLabel("Silk Regen Interval: " + _silkRegenInterval);
            DrawStatLabel("Rosary & Shard Increase: " + _rosaryIncrease + "%");
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // level and cost text + apply/cancel/respec buttons
            GUILayout.BeginArea(new Rect(
                _levelingAreaRectTopX,
                _levelingAreaRectTopY + _levelingAreaDefaultRectHeight,
                _levelingAreaDefaultRectWidth,
                _levelingAreaTextAndButtonsHeight));

            GUILayout.Label("Current Level: " + _currentLvl, GUIStyles.LabelStyle);
            GUILayout.Label("Rosaries to Level Up: " + _rosariesToLvlUp, GUIStyles.LabelStyle);

            GUILayout.BeginHorizontal("box");

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button(_applyText, GUIStyles.ButtonStyle, GUILayout.Width(258f), GUILayout.Height(40f))
                && PlayerData.instance.atBench)
            {
                ls.ApplyLevel();
            }

            GUILayout.FlexibleSpace();

            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("Cancel", GUIStyles.ButtonStyle, GUILayout.Width(258f), GUILayout.Height(40f))
                && PlayerData.instance.atBench)
            {
                ls.ResetPendingLevelUp();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal("box");
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button(
                    $"Respec ({s.RespecCost} " + $"{(s.RespecCost > 1 ? "Silkeaters" : "Silkeater")})",
                    GUIStyles.ButtonStyle,
                    GUILayout.Width(522f),
                    GUILayout.Height(40f))
                && PlayerData.instance.atBench)
            {
                ls.Respec();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void DrawSimpleUI()
        {
            var s = Bonfire.Instance.Status;
            var ls = LevelingSystem.Instance;

            ls.CheckStatLevels();
            UpdateGUIFields();

            // layout
            GUILayout.BeginArea(new Rect(
                _levelingAreaRectTopX,
                _levelingAreaRectTopY,
                _levelingAreaDefaultRectWidth,
                _levelingAreaDefaultRectHeight));
            GUILayout.Label("Leveling", GUIStyles.LevelHeaderStyle);

            // stats
            GUILayout.BeginHorizontal("box");

            GUILayout.BeginVertical("box");
            DrawStatButtonExtended("Strength", _gotFreeLevel, s, _rosariesToLvlUp);
            DrawStatButtonExtended("Dexterity", _gotFreeLevel, s, _rosariesToLvlUp);
            DrawStatButtonExtended("Intelligence", _gotFreeLevel, s, _rosariesToLvlUp);
            DrawStatButtonExtended("Artifice", _gotFreeLevel, s, _rosariesToLvlUp);
            DrawStatButtonExtended("Resilience", _gotFreeLevel, s, _rosariesToLvlUp);
            DrawStatButtonExtended("Wisdom", _gotFreeLevel, s, _rosariesToLvlUp);
            DrawStatButtonExtended("Luck", _gotFreeLevel, s, _rosariesToLvlUp);
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            DrawStatLabelExtended($"{s.StrengthStat}");
            DrawStatLabelExtended($"{s.DexterityStat}");
            DrawStatLabelExtended($"{s.IntelligenceStat}");
            DrawStatLabelExtended($"{s.ArtificeStat}");
            DrawStatLabelExtended($"{s.ResilienceStat}");
            DrawStatLabelExtended($"{s.WisdomStat}");
            DrawStatLabelExtended($"{s.LuckStat}");
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            DrawStatLabelExtended("→");
            DrawStatLabelExtended("→");
            DrawStatLabelExtended("→");
            DrawStatLabelExtended("→");
            DrawStatLabelExtended("→");
            DrawStatLabelExtended("→");
            DrawStatLabelExtended("→");
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            DrawStatLabelExtended($"{(s.StrengthStat + ls.StrengthIncrease).ToString()}");
            DrawStatLabelExtended($"{(s.DexterityStat + ls.DexterityIncrease).ToString()}");
            DrawStatLabelExtended($"{(s.IntelligenceStat + ls.IntelligenceIncrease).ToString()}");
            DrawStatLabelExtended($"{(s.ArtificeStat + ls.ArtificeIncrease).ToString()}");
            DrawStatLabelExtended($"{(s.ResilienceStat + ls.ResilienceIncrease).ToString()}");
            DrawStatLabelExtended($"{(s.WisdomStat + ls.WisdomIncrease).ToString()}");
            DrawStatLabelExtended($"{(s.LuckStat + ls.LuckIncrease).ToString()}");
            GUILayout.EndVertical();

            // secondary stat milestone unlocks
            int labelS = GUIStyles.LabelStyle.fontSize;
            GUIStyles.LabelStyle.fontSize = 24;
            GUILayout.BeginVertical("box");
            DrawStatLabelExtended(GetStatSecondaryStatusVisuals("strength"));
            DrawStatLabelExtended(GetStatSecondaryStatusVisuals("dexterity"));
            DrawStatLabelExtended(GetStatSecondaryStatusVisuals("intelligence"));
            DrawStatLabelExtended(GetStatSecondaryStatusVisuals("artifice"));
            DrawStatLabelExtended(GetStatSecondaryStatusVisuals("resilience"));
            DrawStatLabelExtended(GetStatSecondaryStatusVisuals("wisdom"));
            DrawStatLabelExtended(GetStatSecondaryStatusVisuals("luck"));
            GUILayout.EndVertical();
            GUIStyles.LabelStyle.fontSize = labelS;

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // level and cost text + apply/cancel/respec buttons
            GUILayout.BeginArea(new Rect(
                _levelingAreaRectTopX,
                _levelingAreaRectTopY + _levelingAreaDefaultRectHeight,
                _levelingAreaDefaultRectWidth,
                150f));

            GUILayout.Label("Current Level: " + _currentLvl, GUIStyles.LabelStyle);
            GUILayout.Label("Rosaries to Level Up: " + _rosariesToLvlUp, GUIStyles.LabelStyle);

            GUILayout.BeginHorizontal("box");

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button(_applyText, GUIStyles.ButtonStyle, GUILayout.Width(258f), GUILayout.Height(40f))
                && PlayerData.instance.atBench)
            {
                ls.ApplyLevel();
            }

            GUILayout.FlexibleSpace();

            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("Cancel", GUIStyles.ButtonStyle, GUILayout.Width(258f), GUILayout.Height(40f))
                && PlayerData.instance.atBench)
            {
                ls.ResetPendingLevelUp();
            }

            GUILayout.EndHorizontal();

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

            GUILayout.EndArea();
        }

        private void DrawExtendedStatPanel()
        {
            _infoAreaRectTopY = (BonfireGUI.Height / 8f) - 40f;

            GUI.color = new Color(0.8f, 0.7f, 0.3f, 0.9f);
            GUI.DrawTexture(
                new Rect(
                    _infoAreaRectTopX - 4, _infoAreaRectTopY - 4,
                    _infoAreaRectWidth + 8, _infoAreaRectHeight + 12),
                    Texture2D.whiteTexture);

            GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            GUI.DrawTexture(
                new Rect(
                    _infoAreaRectTopX, _infoAreaRectTopY,
                    _infoAreaRectWidth, _infoAreaRectHeight),
                    Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(
                _infoAreaRectTopX,
                _infoAreaRectTopY,
                _infoAreaRectWidth,
                _infoAreaRectHeight));

            GUI.color = Color.white;
            _infoScrollPos = GUILayout.BeginScrollView(_infoScrollPos);

            GUILayout.BeginHorizontal("box");
            DrawStrengthInfo();
            DrawDexterityInfo();
            DrawIntelligenceInfo();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal("box");
            DrawArtificeInfo();
            DrawResilienceInfo();
            DrawWisdomInfo();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal("box");
            DrawLuckInfo();
            DrawInfoFillerBox();  // draw 2 filler boxes to fill empty grid cells on 3rd row
            DrawInfoFillerBox();
            GUILayout.EndHorizontal();

            GUI.color = Color.white;
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawStatButton(string statName, string displayValue, bool gotFreeLevel, PlayerStatus s,
                                   int rosariesToLvlUp)
        {
            if (GUILayout.Button(
                    $"{statName}: {displayValue}",
                    GUIStyles.ButtonStyle,
                    GUILayout.Width(160f),
                    GUILayout.Height(40f))
                && LevelingSystem.CanLevelUp(gotFreeLevel, rosariesToLvlUp)
                && PlayerData.instance.atBench)
            {
                LevelingSystem.Instance.IncreaseStat(statName, s, rosariesToLvlUp);
            }
        }

        private void DrawStatLabel(string text)
        {
            GUILayout.Label(text, GUIStyles.LabelStyle, GUILayout.Width(160f), GUILayout.Height(40f));
        }

        private void DrawStatButtonExtended(string statName, bool gotFreeLevel, PlayerStatus s,
                                   int rosariesToLvlUp)
        {
            if (GUILayout.Button(
                    $"{statName}",
                    GUIStyles.ButtonStyle,
                    GUILayout.Width(160f),
                    GUILayout.Height(40f))
                && LevelingSystem.CanLevelUp(gotFreeLevel, rosariesToLvlUp)
                && PlayerData.instance.atBench)
            {
                LevelingSystem.Instance.IncreaseStat(statName, s, rosariesToLvlUp);
            }
        }

        private void DrawStatLabelExtended(string text)
        {
            GUILayout.Label(text, GUIStyles.LabelStyle, GUILayout.Width(75f), GUILayout.Height(40f));
        }

        private void DrawInfoStatLabel(string text)
        {
            GUILayout.Label(text, GUIStyles.LvlInfoLabelStyle, GUILayout.Width(_colWidth), GUILayout.Height(40f));
        }

        // info boxes for all stats
        private void DrawStrengthInfo()
        {
            GUILayout.BeginVertical("box", GUILayout.Width(_colWidth), GUILayout.ExpandHeight(true));
            GUILayout.Label("Strength", GUIStyles.LabelStyle);

            GUILayout.Label("Primary", GUIStyles.LvlInfoHeaderPrimaryStyle);
            DrawInfoStatLabel("Needle Damage: " + _needleDamage);

            GUILayout.Label("Secondary", GUIStyles.LvlInfoHeaderSecondaryStyle);

            GUILayout.EndVertical();
        }

        private void DrawDexterityInfo()
        {
            Bonfire.Log.LogInfo($"colWidth: {_colWidth}");
            GUILayout.BeginVertical("box", GUILayout.Width(_colWidth), GUILayout.ExpandHeight(true));
            GUILayout.Label("Dexterity", GUIStyles.LabelStyle);

            GUILayout.Label("Primary", GUIStyles.LvlInfoHeaderPrimaryStyle);
            DrawInfoStatLabel("Slash Speed: " + _attackSpeed);
            DrawInfoStatLabel("Crit Damage: " + _critDamage + "%");

            GUILayout.Label("Secondary", GUIStyles.LvlInfoHeaderSecondaryStyle);

            GUILayout.EndVertical();
        }

        private void DrawIntelligenceInfo()
        {
            GUILayout.BeginVertical("box", GUILayout.Width(_colWidth), GUILayout.ExpandHeight(true));
            GUILayout.Label("Intelligence", GUIStyles.LabelStyle);

            GUILayout.Label("Primary", GUIStyles.LvlInfoHeaderPrimaryStyle);
            DrawInfoStatLabel("Silk Skill Damage: " + _spellDamage + "%");
            DrawInfoStatLabel("Binding Cost: " + _bindCost);

            GUILayout.Label("Secondary", GUIStyles.LvlInfoHeaderSecondaryStyle);

            GUILayout.EndVertical();
        }

        private void DrawArtificeInfo()
        {
            GUILayout.BeginVertical("box", GUILayout.Width(_colWidth), GUILayout.ExpandHeight(true));
            GUILayout.Label("Artifice", GUIStyles.LabelStyle);

            GUILayout.Label("Primary", GUIStyles.LvlInfoHeaderPrimaryStyle);
            DrawInfoStatLabel("Tool Damage: " + _toolDamage + "%");
            DrawInfoStatLabel("Tool Regen Interval: " + _toolRegenInterval);

            GUILayout.Label("Secondary", GUIStyles.LvlInfoHeaderSecondaryStyle);

            GUILayout.EndVertical();
        }

        private void DrawResilienceInfo()
        {
            GUILayout.BeginVertical("box", GUILayout.Width(_colWidth), GUILayout.ExpandHeight(true));
            GUILayout.Label("Resilience", GUIStyles.LabelStyle);

            GUILayout.Label("Primary", GUIStyles.LvlInfoHeaderPrimaryStyle);
            DrawInfoStatLabel("Extra Lifeblood Masks " + _extraMasks);
            DrawInfoStatLabel("Block Chance: " + _blockChance + "%");

            GUILayout.Label("Secondary", GUIStyles.LvlInfoHeaderSecondaryStyle);

            GUILayout.EndVertical();
        }

        private void DrawWisdomInfo()
        {
            GUILayout.BeginVertical("box", GUILayout.Width(_colWidth), GUILayout.ExpandHeight(true));
            GUILayout.Label("Wisdom", GUIStyles.LabelStyle);

            GUILayout.Label("Primary", GUIStyles.LvlInfoHeaderPrimaryStyle);
            DrawInfoStatLabel("Silk Gained On Enemy Hit: " + _extraSilk);
            DrawInfoStatLabel("Silk Regen Interval: " + _silkRegenInterval);

            GUILayout.Label("Secondary", GUIStyles.LvlInfoHeaderSecondaryStyle);
            GUILayout.Label(
                $"[{LevelingSystem.WisdomMilestone_1}] " +
                "Wispfire Lantern silk cost removed, attack delay halved and spawn cooldown is down to 2 seconds.",
                LevelingSystem.CheckWisdomMilestone_1 ?
                    GUIStyles.LvlInfoLabelStyle : GUIStyles.LvlInfoLabelDisabledStyle);
            GUILayout.Label(
                $"[{LevelingSystem.WisdomMilestone_2}] " +
                "Wispfire Lantern attack cooldown down to 1/3 of original, spawns a wisp every second.",
                LevelingSystem.CheckWisdomMilestone_2 ?
                    GUIStyles.LvlInfoLabelStyle : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.EndVertical();
        }

        private void DrawLuckInfo()
        {
            GUILayout.BeginVertical("box", GUILayout.Width(_colWidth), GUILayout.ExpandHeight(true));
            GUILayout.Label("Luck", GUIStyles.LabelStyle);

            GUILayout.Label("Primary", GUIStyles.LvlInfoHeaderPrimaryStyle);
            DrawInfoStatLabel("Crit Chance: " + _critChance + "%");
            DrawInfoStatLabel("Rosary & Shard Increase: " + _rosaryIncrease + "%");

            GUILayout.Label("Secondary", GUIStyles.LvlInfoHeaderSecondaryStyle);

            GUILayout.EndVertical();
        }

        // filler component for empty cell spaces
        private void DrawInfoFillerBox()
        {
            GUILayout.BeginVertical("box", GUILayout.Width(_colWidth), GUILayout.ExpandHeight(true));

            DrawInfoStatLabel("");

            GUILayout.EndVertical();
        }

        private void ChangeLevelingMenuStyle()
        {
            int s = Bonfire.Instance.Status.LevelingInfoStyle + 1;
            if (s >= 4 || s < 0)
                s = 0;

            Bonfire.Instance.Status.LevelingInfoStyle = s;
        }

        private void DrawUIPanels()
        {
            var s = Bonfire.Instance.Status.LevelingInfoStyle;

            switch (s)
            {
                case 0:
                    DrawDefaultUI();
                    return;
                case 1:
                    DrawDefaultUI();
                    DrawExtendedStatPanel();
                    return;
                case 2:
                    DrawSimpleUI();
                    return;
                case 3:
                    DrawSimpleUI();
                    DrawExtendedStatPanel();
                    return;
            }
        }

        private void UpdateGUIFields()
        {
            var s = Bonfire.Instance.Status;
            var ls = LevelingSystem.Instance;

            _availableRelicLevels = ls.GetAvailableRelicLevels();
            _gotFreeLevel = _availableRelicLevels > 0;

            _rosaryLevel = LevelingSystem.DisplayPendingRosaryLevel();
            _rosariesToLvlUp = LevelingSystem.GetLevelUpRosaryRequirement(_rosaryLevel);

            _currentLvl = LevelingSystem.GetCurrentLevel();

            // preview strings
            _totalStr = (s.StrengthStat + ls.StrengthIncrease).ToString();
            _totalDex = (s.DexterityStat + ls.DexterityIncrease).ToString();
            _totalInt = (s.IntelligenceStat + ls.IntelligenceIncrease).ToString();
            _totalArt = (s.ArtificeStat + ls.ArtificeIncrease).ToString();
            _totalRes = (s.ResilienceStat + ls.ResilienceIncrease).ToString();
            _totalWsdm = (s.WisdomStat + ls.WisdomIncrease).ToString();
            _totalLuck = (s.LuckStat + ls.LuckIncrease).ToString();

            // stat calculations
            _needleDamage = LevelingSystem.NeedleDamage(s.StrengthStat + ls.StrengthIncrease).ToString();

            _attackSpeed = LevelingSystem.AttackSpeed(s.DexterityStat + ls.DexterityIncrease).ToString();
            _critDamage = LevelingSystem.CritDamage(s.DexterityStat + ls.DexterityIncrease, 100).ToString();

            _spellDamage = LevelingSystem.SpellDamage(100, s.IntelligenceStat + ls.IntelligenceIncrease).ToString();
            _bindCost = LevelingSystem.BindCost(s.IntelligenceStat + ls.IntelligenceIncrease).ToString();

            _toolDamage = LevelingSystem.ToolDamage(100, s.ArtificeStat + ls.ArtificeIncrease).ToString();
            _toolRegenInterval = "-";
            if (s.ArtificeStat + ls.ArtificeIncrease > 0)
            {
                int toolRegen = LevelingSystem.ToolRegenInterval(s.ArtificeStat + ls.ArtificeIncrease);
                _toolRegenInterval = toolRegen > 0 ? toolRegen.ToString() + " s" : "-";
            }

            _extraMasks = LevelingSystem.ExtraMasks(s.ResilienceStat + ls.ResilienceIncrease).ToString();
            _blockChance = s.ResilienceStat + ls.ResilienceIncrease > 0
                ? LevelingSystem.AverageBlockChance(s.ResilienceStat + ls.ResilienceIncrease).ToString("F0")
                : "0";

            _extraSilk = LevelingSystem.ExtraSilk(s.WisdomStat + ls.WisdomIncrease, 1).ToString();
            _silkRegenInterval = "-";
            if (s.WisdomStat + ls.WisdomIncrease > 0)
            {
                int silkRegen = LevelingSystem.SilkRegenInterval(s.WisdomStat + ls.WisdomIncrease);
                _silkRegenInterval = silkRegen > 0 ? silkRegen.ToString() + " s" : "-";
            }

            _critChance = LevelingSystem.CritChance(s.LuckStat + ls.LuckIncrease).ToString();
            _rosaryIncrease = (5 * (s.LuckStat + ls.LuckIncrease)).ToString();

            // "Apply" button label
            if (s.PendingRelicLevels > 0 && s.PendingRosaries > 0)
            {
                _applyText = $"Apply ({s.PendingRosaries} rosaries and {s.PendingRelicLevels} relics)";
            }
            else if (s.PendingRelicLevels > 0)
            {
                _applyText = $"Apply ({s.PendingRelicLevels} relic levels)";
            }
            else if (_gotFreeLevel)
            {
                _applyText = $"{_availableRelicLevels} Free Levels available!";
            }
            else
            {
                _applyText = $"Apply ({s.PendingRosaries} rosaries)";
            }
        }

        private string GetStatSecondaryStatusVisuals(string statName)
        {
            (int current, int total) = LevelingSystem.Instance.GetStatSecondaryStatus(statName);
            if (total == 0) return "";

            return new string('●', current) + new string('○', total - current);
        }
    }
}