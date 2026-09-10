using UnityEngine;

namespace BonfireSilksong.GUIComponents
{
    /// <summary>
    /// Displays info overlay.
    /// </summary>
    public class InfoWindow
    {
        public static bool PendingInfoWindow = false;

        public static void Display()
        {
            if (!PendingInfoWindow) return;

            GUI.color = new Color(0, 0, 0, 0.8f);
            GUI.DrawTexture(new Rect(0, 0, BonfireGUI.Width, BonfireGUI.Height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float w = InfoWindowWidth;
            float h = InfoWindowHeight;
            float x = BonfireGUI.Width / 2f - w / 2f;
            float y = BonfireGUI.Height / 2f - h / 2f;

            // main texture
            Rect borderRect = new Rect(x - 4, y - 4, w + 8, h + 8);
            GUI.color = Color.gray;
            GUI.DrawTexture(borderRect, Texture2D.whiteTexture);

            Rect rect = new Rect(x, y, w, h);
            GUI.color = new Color(0.2f, 0.2f, 0.2f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            // footer texture
            float bottomH = 60f;
            float bottomY = y + h - bottomH;

            GUI.color = new Color(0.3f, 0.3f, 0.3f);
            Rect bottomRect = new Rect(x, bottomY, w, bottomH);
            GUI.DrawTexture(bottomRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            // rendering
            CustomCursor.BeginGroup(rect);

            CustomCursor.BeginArea(new Rect(
                InfoInnerOffset,
                InfoInnerOffset,
                rect.width - InfoOuterOffset,
                rect.height - bottomH - InfoOuterOffset));

            DrawInfoContent();
            CustomCursor.EndArea();

            CustomCursor.BeginArea(new Rect(0, rect.height - bottomH, rect.width, bottomH));
            DrawInfoFooter();
            CustomCursor.EndArea();

            CustomCursor.EndGroup();
        }


        private const float InfoWindowWidth = 650f;
        private const float InfoWindowHeight = 700f;
        private const float InfoInnerOffset = 10f;
        private const float InfoOuterOffset = 2 * InfoInnerOffset;

        private static Vector2 _infoWindowScrollPos;

        private static void DrawInfoContent()
        {
            _infoWindowScrollPos = CustomCursor.BeginScrollView(_infoWindowScrollPos);

            GUILayout.BeginVertical("box");
            Color previous = GUI.color;
            GUI.color = new Color(1f, 0.7f, 0.2f);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Bonfire-Silksong Mod Info", GUIStyles.BenchListAreaLabelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUI.color = previous;

            GUILayout.Label(
                "- The contents here are revised occasionally, but it's still possible some segments have outdated " +
                "info in them. And of course expect to find typos here and there.\n" +
                "- Silksong is going to get multiple content update DLCs. Each of them will affect general gameplay " +
                "and progression possibilities in one way or another. Therefore it's very likely this mod also sees " +
                "substantial changes over time.\n" +
                "\n" +
                "[Hotkeys]\n" +
                "-- General --\n" +
                "\"Spell/Tool\" = close topmost GUI overlay, these includes: this window, settings and bench fast " +
                "travel\n" +
                "\n" +
                "-- Bench Fast Travel --\n" +
                "\"Map\"/\"QuickMap\"/\"Inventory\"/menu close (Escape on keyboard) = close map overlay\n" +
                "\"Needolin\" = travel to selected bench location");

            GUILayout.Space(25);

            GUILayout.Label("Leveling system", GUIStyles.SettingsHeader);

            GUILayout.Space(10);

            GUILayout.Label("General", GUIStyles.SettingsHeader2);
            GUILayout.Label(
                "- Leveling costs rosaries. Cost scaling should be reasonable so you can still afford to buy " +
                "normal stuff like vendor items, benches, maps etc.\n" +
                "\n" +
                "- All stats cap at 99 points. This value serves just as an upper boundary, you won't be getting " +
                "anywhere close to it with normal gameplay: cost of next level uses formula f(n) = n^2 + 10*n + 50 " +
                "so going from 1 -> 99 on a single stat costs a total of Sum(f, k=1, n=99) = 382800 rosaries.\n" +
                "\n" +
                "- You can get free level-ups from each tier 3-5 relic returned to Relic Seeker Scrounge. These " +
                "include: Choral Commandment, Rune Harp, Arcane Egg.\n" +
                "\n" +
                "- You can respec your stat levels. You keep all free levels and can immediately use them afterwards" +
                "+ all your rosaries spent on level ups are also refunded. Respeccing costs Silkeaters: first one " +
                "is free, and each one after increases the cost by 1.\n" +
                "\n" +
                "- <GUI> If you scroll the stat info panel, you might see occasional small jumps to opposite " +
                "scroll direction. These happen because GUI actually renders only the nearest info panels based on " +
                "current scrollbar position and replaces the rest with filler content to keep total scroll length " +
                "very close to a constant value. The process of deciding what to hide exactly at given position is " +
                "handled manually and will therefore include some minor inconsistencies. However the performance " +
                "increase this gives, especially on lower-end GPUs, is quite significant. Besides, I doubt most " +
                "users would even notice this without reading this paragraph.");

            GUILayout.Space(10);

            GUILayout.Label("Stats", GUIStyles.SettingsHeader2);

            GUILayout.Label("Global Buffs", GUIStyles.SettingsHeader3);
            GUILayout.Label(
                "These require certain level and/or bonus condition e.g. \"Defeat boss X\", to become active.\n" +
                "Current ones provide QoL. They are not very interesting gameplay-wise, you can except more " +
                "interesting bonuses in the future.\n" +
                "\n" +
                "The flat currency bonus ensures all enemies now drop rosaries: this extra cash should help a bit " +
                "in early/midgame economy.");

            GUILayout.Label("Attributes", GUIStyles.SettingsHeader3);
            GUILayout.Label(
                "These form the core of leveling system. You increase the total player level by allocating " +
                "points into any of them. Stat have primary attributes + possible milestones.");

            GUILayout.Label("Primary attributes", GUIStyles.SettingsHeader4);
            GUILayout.Label(
                "Each stat has 1-2 primary attributes which scale with stat level. These give small, but " +
                "consistent power-ups. Scaling itself can vary:\n" +
                "\n" +
                "- Percentage bonuses tend to increase every level, but are obviously less useful if base damage is" +
                "low.\n" +
                "\n" +
                "- Flat bonuses give more benefit early. Their scaling is often stat-specfic: Resilience gives " +
                "extra blue masks occasionally and mask count caps at 8, Artifice tool replenishment & Wisdom silk " +
                "regen require a minimum of 5 points to become useful, Wisdom's extra silk on needle hits increases " +
                "every 20 levels (20 -> +1, 40 -> +2 etc) and so forth.\n" +
                "\n" +
                "- Strength's needle damage scaling is also a bit different: it scales from current needle upgrades" +
                "so you will get more damage per point for higher upgraded needles. Also be aware that GUI displays " +
                "the total value, not bonus: with 0 points you will always see vanilla needle damage.");

            GUILayout.Label("Wisdom: gain silk after N hits", GUIStyles.SettingsHeader4);
            GUILayout.Label(
                "A shared counter tracks your hits on enemies. After Nth hit, you get 1 silk and counter resets to " +
                "0. Importantly: this works for all damage types and gives a nice global source of silk generation.");

            GUILayout.Label("Luck: critical hits", GUIStyles.SettingsHeader4);
            GUILayout.Label(
                "If milestone mentions crit e.g. \"can apply critical hit\", this only applies to crits generated " +
                "through Luck stat. Game's own crits e.g. from Wanderer's Crest or Magnetite Dice, don't count! " +
                "This also applies to Dexterity stat Crit Damage Bonus.");

            GUILayout.Label("Luck: increase in rosary bead and shell shard drops", GUIStyles.SettingsHeader4);
            GUILayout.Label(
                "*This value increases the total drop count of each currency type (shell shards and " +
                "small/medium/large rosary beads), rounded down: if bonus is 50% & enemy drops 3 small & 1 large " +
                "rosary bead + 5 shell shards, the updated drops are\n" +
                "\n" +
                "- 1.5 * 3 -> 4.5 => 4 small rosary beads\n" +
                "- 1.5 * 0 => 0 medium rosary beads\n" +
                "- 1.5 * 1 -> 1.5 => 1 large rosary bead\n" +
                "- 1.5 * 5 -> 7.5 => 7 shell shards");

            GUILayout.Label("Silk skill needle scaling", GUIStyles.SettingsHeader4);
            GUILayout.Label(
                "In vanilla game, silk skills scale from needle damage. This still applies to damage from base " +
                "needle upgrades, but any bonus damage from leveling Strength will not buff them further. Idea is " +
                "that Intelligence stat specifically targets silk skills damage whereas Strength is for needle-based " +
                "attacks. Keeping bonuses from both would give skills too much power in early/mid game.");

            GUILayout.Label("Resilience: blue masks", GUIStyles.SettingsHeader4);
            GUILayout.Label(
                "In vanilla game, blue masks reset to 0 every time you visit a bench. This mod uses a custom system " +
                "to override default behavior for any masks gained from leveling Resilience stat. The system will " +
                "perform this update after following actions\n" +
                " - closing inventory or menu screens\n" +
                " - applying levels\n" +
                " - level respecs\n" +
                " - after using bench fast travel\n" +
                "\n" +
                " To emphasize: this only happens when you're at any bench. You can except normal behavior otherwise." +
                "\n" +
                "This system is not perfect: it has been tested quite thoroughly, but sometimes the blue hp updates " +
                "don't refresh properly and your health var visuals can have 1. missing blue masks and 2. gap between" +
                "normal and blue masks. If these happen, know that the actual blue hp value should still get updated " +
                "even if visuals did not. The easiest fix is to sit down on a bench again, or if you're already " +
                "sitting, just open then close any inventory or map screen to force a HUD refresh.\n" +
                "\n" +
                "-> One simple example of failed hp update is when you load into save file, leave bench as soon as " +
                "possible and open inventory: game still renders the hp HUD, but is interrupted because of inventory " +
                "overlay. If you wait a bit then close invetory, you will get partial or missing blue mask visuals.");

            GUILayout.Space(10);

            GUILayout.Label("Milestones", GUIStyles.SettingsHeader3);
            GUILayout.Label(
                "All stats give you additional bonuses after certain point thresholds are reached. " +
                "These milestone bonuses can vary from simple buffs to gameplay-altering mechanics.\n" +
                "\n" +
                "- Unlike global unlocks: these are solely tied to certain stat level and have no other " +
                "gameplay/progression requirements.\n" +
                "- Many milestones unlock optional features which can be toggled on/off using their respective " +
                "GUI buttons. This means you can change them only during bench visits. Some of them might " +
                "prevent normal gameplay functions like using a certain tool or even specific button inputs.\n" +
                "\n" +
                "- Some milestones can definitely be stronger than others, although this also depends on how well " +
                "you utilize them. Futhermore some of these can make the gameplay significantly more relaxing/less " +
                "riskier, while others lean to more active, possibly high-risk/high-reward approach.\n" +
                "- Milestones for a stat are generally tailored around a few specific playstyles/damage " +
                "sources. Now this doesn't mean you cannot find useful utility and/or synergies under stats: even " +
                "the first milestones which tend to have very low stat requirements can still give you significant " +
                "benefits!");

            GUILayout.Label("\nStat milestones are centered around certain playstyles/damage sources. But even if " +
            "you're focusing on just one specific source, you can still find some useful utility and synergies im " +
            "other stat paths. Feel free to experiment!\n" +
            "Anyway here's the simple overview of each stat:\n");
            GUILayout.Label("Strength", GUIStyles.SettingsHeader4);
            GUILayout.Label(
                "- Needle\n" +
                "- Needle Strike");
            GUILayout.Label("Dexterity", GUIStyles.SettingsHeader4);
            GUILayout.Label(
                "- Clawline");
            GUILayout.Label("Intelligence", GUIStyles.SettingsHeader4);
            GUILayout.Label(
                "- Silk Skills\n" +
                "- Claw Mirror\n" +
                "- Weaver's Light (modded damage source)");
            GUILayout.Label("Artifice", GUIStyles.SettingsHeader4);
            GUILayout.Label(
                "- Mainly red tools\n" +
                "- Damage-inflicting blue tools also get some benefits");
            GUILayout.Label("Resilience", GUIStyles.SettingsHeader4);
            GUILayout.Label(
                "- Normal hp replaced with blue masks: you can either play a tank build with limited offensive " +
                "capabilities, or a high damage glass cannon where taking damage must be avoided at all cost. Each " +
                "will therefore change the normal gameplay quite a bit. Furthermore, reaching the final milestone " +
                "and entering Plasmified state adds even more offense at the cost of defense.\n" +
                "- Damage types: Reflect (modded damage source) and universal damage buffs");
            GUILayout.Label("Wisdom", GUIStyles.SettingsHeader4);
            GUILayout.Label(
                "- Very relaxed summon/minion gameplay: let them do all the damage, or engage in combat normally and " +
                "let them provide support.\n" +
                "- Damage sources: Silk Nails & Cogfly Warrior (modded variants of Pale Nails & Cogfly) + Wispfire " +
                "Lantern tool.");
            GUILayout.Label("Luck", GUIStyles.SettingsHeader4);
            GUILayout.Label(
                "- Focuses on the 3 pin tools: Straight Pin, Threefold Pin and Longpin.\n" +
                "- Is also the only source of custom crit chance stat which is required for mod's own critical hit " +
                "system.\n" +
                "=> Luck was originally meant to be a support stat with no milestones at all. But it was more " +
                "interesting to introduce at least something, and the idea of a ranged-only build fit into this " +
                "nicely. So current milestones were quickly hacked together: while not as polished as milestones of " +
                "other stat paths, it should at least give more agency to these 3 pin tools in the later stages of " +
                "the game");

            GUILayout.EndVertical();

            GUILayout.Space(15);

            GUILayout.BeginVertical("box");
            GUILayout.Label("Bench fast travel", GUIStyles.SettingsHeader);
            GUILayout.Label(
                "This mod introduces a built-in fast travel system where you can teleport between unlocked bench " +
                "locations. Clicking the \"Fast Travel\" button in bench menu opens the game map with a custom " +
                "overlay which lists all unlocked benches, each categorized under a map zone.\n" +
                "\n" +
                "- The concept of map zone may vary. Example of this is \"The Citadel\" which combines other related " +
                "subareas under it e.g. Choral Chambers, High Halls, Whispering Vaults, Whiteward and Cogwork Core, " +
                "where each could be separated into their own zones. But this is a clear exception, others tend to " +
                "be more isolated.\n" +
                "\n" +
                "- If bench location data is not found, it's placed under \"Other\" category.");

            GUILayout.Space(10);

            GUILayout.Label("Unlock conditions & How to Use", GUIStyles.SettingsHeader2);
            GUILayout.Label(
                "Following conditions are required for bench fast travel:\n" +
                "1. Defeat Bell Beast in Act 1 (primary condition)\n" +
                "2. Obtain map and Bench Pins items from Shakra (map menu access).");

            GUILayout.Label("Usage", GUIStyles.SettingsHeader3);
            GUILayout.Label(
                "- Sit on a bench and click \"Fast Travel\" button.\n" +
                "- Select location, click the the \"Travel\" button at bottom-middle.\n" +
                "- Map menu closes and after a short black screen you should wake at your desired bench location\n" +
                "- To close the map menu, you either\n" +
                "  - Press any of the following keybinds: \"Inventory\", \"Map\", \"Quick Map\" or cancel (Escape on " +
                "keyboard)\n" +
                "  - Click the 'Close bench menu' button.");

            GUILayout.Label("In-game notification", GUIStyles.SettingsHeader3);
            GUILayout.Label(
                "After you defeat Bell Beast and visit a bench, a one-time pop-up appears and notifies you of " +
                "fast travel unlock. It's meant to serve as a brief reminder whereas this info section contains the " +
                "full details.");

            GUILayout.Space(10);

            GUILayout.Label("Additional details", GUIStyles.SettingsHeader2);

            GUILayout.Label("Map screen overlay", GUIStyles.SettingsHeader3);
            GUILayout.Label(
                "- Map screen is the default one except a custom overlay is opened on top of it. Most inputs " +
                "are disabled here in order to prevent inventory navigation bugs.\n" +
                "\n" +
                "- When you click at any bench location, map camera pans over the corresponding bench pin if " +
                "possible: if bench pin is closer to the edge of map and map camera can't move on top of it, the " +
                "camera will instead move as close as possible.\n" +
                "\n" +
                "- There are also some benches that have no bench pin available. Such locations are marked with a " +
                "star symbol after their name e.g. \"Bellhome ★\" in Bellhart. And because these benches don't " +
                "have the pin location data available, clicking at them will not move camera at all.");

            GUILayout.Label("Styles", GUIStyles.SettingsHeader3);
            GUILayout.Label(
                "Bench menu has 4 simple style variants:\n" +
                "- Basic + Default Names -> no zone colors, uses game's internal bench scene names\n" +
                "- Basic + Custom Names -> no zone colors, uses custom bench names\n" +
                "- Colored + Default Names -> zone colors, internal scene names\n" +
                "- Colored + Custom Names -> zone colors and custom bench names\n" +
                "You can rotate through these by clicking the style button above the bench list.");

            GUILayout.Label("Bench unlocking", GUIStyles.SettingsHeader3);
            GUILayout.Label(
                "You can only fast travel to already unlocked benches.\n" +
                "- for bench pins this means the map pin must have normal color. Disabled benches have this darker, " +
                "grayed-out coloring instead.\n" +
                "\n" +
                "- for non-pin locations, there are specific checks which are tied to game progression. You will " +
                "unlock them naturally. A simple example is again \"Bellhome ★\": you need to have unlocked the home" +
                "and visited it once.");

            GUILayout.Label("Bench availability", GUIStyles.SettingsHeader3);
            GUILayout.Label(
                "- Certain benches can be available normally up to a certain progression point, then get disabled." +
                "For example: the bench in Bonebottom is destroyed permanently in Act 3 and cannot be accessed " +
                "afterwards. Another example, but for pin-free locations: Widow boss bench is also destroyed in " +
                "Act 3\n" +
                "\n" +
                "- Abyss in Act 3 requires some extra checks: if player is allowed to teleport out of Abyss on 1. " +
                "visit, this can possibly break quest progression and cause a softlock. Thus following rules are " +
                "enforced:\n" +
                "\n" +
                "  - 1. Visit: Can only teleport between Abyss benches, complete ascent normally to leave.\n" +
                "  - After Ascent: Can't teleport back to Abyss before using Diving Bell again for 2. visit.\n" +
                "  - 2. Visit: 'Last' Dive' quest can no longer break, can freely teleport in and out of Abyss " +
                "from now.");

            GUILayout.EndVertical();

            GUILayout.Space(15);

            GUILayout.BeginVertical("box");
            GUILayout.Label("Extra Settings Panel", GUIStyles.SettingsHeader);
            GUILayout.Label(
                "Clicking the 'Extra Settings' button in bench menu opens a new window. In there, you can change " +
                "the mod's settings, including some gameplay adjustments. Labels should describe their " +
                "functionality well enough so no further details are given here, except for the following section " +
                "on custom Gamepad cursor.");

            GUILayout.Space(10);

            GUILayout.Label("Custom cursor", GUIStyles.SettingsHeader2);
            GUILayout.Label(
                "All of the GUI menus are normally navigated with mouse cursor. Notably, this cursor actually uses " +
                "mouse position and there seems to be no simple way to sync it with Gamepad controls. " +
                "To support Gamepad users, this mod introduces a custom cursor object: it is not 100% accurate " +
                "compared to mouse cursor and doesn't cause the visual indicator when you hover over a button, but " +
                "as long as you click clearly inside the button edges, it should register inputs just fine.\n" +
                "\n" +
                "Cursor controls are simple:\n" +
                "- Right stick to move cursor.\n" +
                "- Attack/Jump input to click buttons.\n" +
                "- Left/Right Bumper to move scrollbar up/down.");

            GUILayout.EndVertical();

            CustomCursor.EndScrollView();
        }

        private static void DrawInfoFooter()
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (CustomCursor.Button(
                    "Close window",
                    GUIStyles.CrestUnlockButtonStyle,
                    GUILayout.Width(160f),
                    GUILayout.Height(35f)))
            {
                PendingInfoWindow = false;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }
    }
}