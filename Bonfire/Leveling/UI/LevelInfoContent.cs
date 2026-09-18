namespace BonfireSilksong.Leveling.UI
{
    public static class LevelInfoContent
    {
        public static string StrengthMilestone1 =>
            "Needle strike: active charge time reduced by 20%. " +
            "Can toggle on/off automatic release when fully charged.";
        public static string StrengthMilestone2 =>
            "Needle range increased by 33% and its initial charge time reduced by 20%.";
        public static string StrengthMilestone3 =>
            "- Flintslate and Flea Brew tools have infinite buff duration.\n" +
            "- Downslash damage increased by 100%.\n" +
            "- Needle strike: both initial and active charge time reduction buffed (20% -> 40%).";

        public static string DexterityMilestone1 =>
            "Clawline damage increased by 25%. " +
            $"Every {DexterityStat.ClawlineHitLimit}th hit from Clawline deals 2x damage " +
            "(default Clawline use counts as 2 hits).";
        public static string DexterityMilestone2 =>
            "Clawline silk cost removed, can now critically hit.";
        public static string DexterityMilestone3 =>
            "All critical hits now heal 1 mask.\n" +
            "\n" +
            "Clawline:\n" +
            " - damage bonus increased (25% -> 50%).\n" +
            " - 7th hit will always heal 1 mask and casts 'Silk Spear' silk skill without costing silk.\n" +
            "   Skill scales normally from silk skill damage bonuses.";

        public static string IntelligenceMilestone1 =>
            "Toggle on/off \"Weaver's Light\".\n" +
            "\n" +
            "<Weaver's Light>\n" +
            "- Taunt now enables an aura around you which flashes every second, consuming 1 silk and " +
            "dealing damage to enemies inside it. Damage and range increases with Intelligence stat.\n" +
            "- If you taunt with Throwing Rings: aura will instead cost 2 silk per second and has its range " +
            "reduced by 50%, but you get +3 extra damage from each active silk in spool.\n" +
            "- Taunt no longer grants the 50% damage bonus for next hit, however its initial cost is also" +
            "removed. Taunting again disables active aura effect, meaning you can use it tactically" +
            "without needing to wait until all your silk is depleted.\n" +
            "- Equipping Egg of Flealia reduces aura silk cost by 1 when on max hp.";
        public static string IntelligenceMilestone2 =>
            "Toggle on/off \"Weak Bind\".\n" +
            "\n" +
            "<Weak Bind>\n" +
            "- Bind heals 1 mask and costs 1/3 of current cost, but keeps its current speed,\n" +
            "- You take + 1 damage from everything, but get the following bonuses:\n" +
            "  - Silk Skills: Each hit instance spawns Voltvessels bola projectile on top of enemy. Bolas have a " +
            "custom, flat intelligence damage scaling which can only benefit from global damage buffs. " +
            "This effect also disables normal use of Voltvessels tool.\n" +
            "  - Claw Mirror: procs additional time (similar to Multibinder effect), binding speed increased " +
            "by 33%.\n" +
            "  - Weaver's Light: gives 1 silk if at least one enemy got hit by the aura.";
        public static string IntelligenceMilestone3 =>
            "Silk skills, Claw Mirror and Taunt can crit. Equipping Claw Mirror increases binding speed by 33%.";
        public static string IntelligenceMilestone4 =>
            "Give more effects to all Intelligence-focused abilities:\n" +
            "- Silk skills: silk cost reduced by 1.\n" +
            "- Claw Mirror: every 2 points in Intelligence adds +1 damage. Damage is applied after Artifice stat " +
            "scaling.\n" +
            "- Weaver's Light: Gains a new \"Void Burst\" toggle state. Void Burst makes taunt a single-use " +
            "ability, consuming all silk when used while giving following benefits: \n" +
            "  - Range increased by 5.\n" +
            "  - Each silk in spool increases damage by +6.\n" +
            "\n" +
            "Taunting with Throwing Rings has no additional effects when Void Burst is enabled.";

        public static string ArtificeMilestone1 =>
            "Pimpillo: has 2x tool capacity and can apply critical hits.\n" +
            "Sawtooth Circlet: can apply critical hits.";
        public static string ArtificeMilestone2 =>
            "Rosary Cannon: has infinite, free refills -> every time you begin the use animation, it becomes " +
            "fully loaded.\n" +
            "Silkshot: silk cost removed, can apply critical hits.\n" +
            "Sawtooth Circlet: gives 1 silk from first enemy hit.";
        public static string ArtificeMilestone3 =>
            "Rosary Cannon: +2 flat damage which is applied before Artifice stat damage bonus.\n" +
            "Silkshot: has infinite charges.\n" +
            "Sawtooth Circlet: horizontal range increased by 50%.\n" +
            "Pimpillo: casts \"Rune Rage\" silk skill on top of enemy on hit without costing silk. Skill scales " +
            "normally from any silk skill damage buffs.";
        public static string ArtificeMilestone4 =>
            "Offensive red tool has 33% chance to spawn a copy of itself when used. " +
            "When a copy is spawned, neither original nor copy cost charges.\n" +
            "Following tools cannot benefit from this: Silkshot, Delver's Drill, Rosary Cannon, " +
            "Voltvessels Spear (Bolas do work), Snare Setter.";

        public static string ResilienceMilestone1 =>
            "You can now select \"Vengeful\" or \"Hollow\" blue mask state.\n" +
            "\n" +
            "When Vengeful/Hollow is active:\n" +
            "- Blue masks effectively replace normal masks: if you have no blue hp left, the next hit will " +
            "inflict \"default damage x 10\".\n" +
            "- All damage taken is reduced by 1, but must always deal at least 1 damage.\n" +
            "- You can enter Plasmified state only after unlocking the final Resilience milestone. Plasmified " +
            "refers to blue health regen state which is normally triggered after getting 9 blue masks.\n" +
            "- Egg of Flealia gives silk cost reduction only if your active blue mask count is greater than or " +
            "equal to current mask limit from Resilience. So if Resilience gives 3 masks and you active mask " +
            "is to 2 or less, cost reduction no longer applies. Be aware this is no longer true while in " +
            "Plasmified state as max hp is again equal to normal mask count and ignores Resilience bonus masks " +
            "entirely.\n" +
            "=> Both playstyles are quite risky to play early, but Vengeful will lean more towards the tanky/" +
            "low-risk, but one-dimensional playstyle whereas with Hollow you want to minimize hits taken at all " +
            "cost to keep huge damage bonus benefits.\n" +
            "\n" +
            "<Vengeful>\n " +
            "- Reflect damage back to enemies around you when you take damage. All your non-reflect damage is " +
            "reduced by 99%, but deals always at least 1 damage. Only exception to this rule is the \"Memory " +
            "Crystal\" blue tool which deals its full damage.\n" +
            "- Reflect base damage is 5. Damage scales from 1. Resilience stat extra blue masks count (max 8) " +
            "and 2. mask upgrades (max 5).\n" +
            "- Bind no longer heals normal masks, but instead 1 blue mask.\n" +
            "- You gain 1 silk after getting hit by an enemy.\n" +
            "=> Low-risk playstyle: you can become very tanky, but are also limited to one-dimensional " +
            "offensive gameplay of ramming enemies with your body.\n" +
            "\n" +
            "<Hollow>\n" +
            "- Bind can no longer be used at all.\n" +
            "- You can no longer benefit from stat primary damage bonuses " +
            "(Strength, Dexterity, Intelligence, Artifice) or Luck crit chance, except during Plasmified state " +
            "after unlockking final milestone. Bonuses from milestones work normally.\n" +
            "- Damage from all sources is increased by 50%. Each active blue mask in hp bar gives 7% extra damage " +
            "additively while mask upgrades give passive bonus of 8% instead.\n" +
            "- All non-Resilience stat milestones require 3 less points to become active. You must still have at " +
            "least 1 point in a stat to activate any of its milestones.\n" +
            "=> High-risk playstyle as healing is pretty much non-existent, but gives a universal damage boost " +
            "and allows reaching non-Resilience milestones with fewer stat points.";
        public static string ResilienceMilestone2 =>
            "Additional buffs to blue health modes:\n" +
            "<Vengeful> Bind now heals 2 blue masks, reflect damage area increased by 1.\n" +
            "<Hollow> You get flat +10 damage bonus which is applied after mask bonuses. Milestone bonus " +
            "increased to 5.";
        public static string ResilienceMilestone3 =>
            "While in Vengful/Hollow state, you can now enter Plasmified state and Plasmium Phial will instantly " +
            "trigger Plasmified when used. While Plasmified, your use of Plasmium Phial is again blocked but " +
            "you get a more offensive variant of current state:\n" +
            "\n" +
            "<Vengeful>\n" +
            "- Each active blue mask in hp bar now also gives additional +10 reflect damage.\n" +
            "- Reflect damage can crit.\n" +
            "- Reflect area increased by 2.\n" +
            "- Penalty: you take normal amount of damage again and bind heals only the default amount of 1 during " +
            "Plasmification.\n" +
            "\n" +
            "<Hollow>\n" +
            "- Active blue mask bonus increased (7% -> 20%).\n" +
            "- Crit change and crit damage can now be applied normally.\n" +
            "- Milestone bonus increased to 10.\n" +
            "- Penalty: you take normal amount of damage + additional 2 masks (1 becomes 3, 2 becomes 4 and so on).\n" +
            "\n" +
            "Your max hp in Plasmified state is equal to your normal mask count. Each active mask is counted " +
            "into blue mask damage bonus while Vengeful/Hollow state is active.";

        public static string WisdomMilestone1 =>
            "- Toggle on/off \"Silk Nails\" which is a custom variant of Pale Nails silk skill.\n" +
            "\n" +
            "<Silk Nails>\n" +
            "- Has its own damage and spawn cooldown scaling from Wisdom.\n" +
            "- Spawns automatically when enemies are near.\n" +
            "- Doesn't have a silk cost.\n" +
            "- While Silk Nails is enabled, you can no longer use Pale Nails silk skill.\n" +
            "\n" +
            "Additionally: you can now toggle your summons on/off using Needolin. This will also disable/enable the " +
            "GUI toggle buttons for custom summons.\n" +
            "- Following are counted as summons: Wispfire Lantern, Silk Nails, Cogfly Warrior.\n" +
            "- Hornet's texture displays a brief flash when Needolin animation starts, this signals the toggle " +
            "was succesful.\n" +
            "- This setting will not be saved between sessions and starts always in \"ALLOWED\" state. This then" +
            "means if you close the game and relaunch it, the first Needolin toggle will always switch to " +
            "\"BLOCKED\".\n" +
            "=> This feature is meant to serve as a quality of life during platforming sections where bouncing " +
            "off of enemies could become very difficult with summons attacking them.";
        public static string WisdomMilestone2 =>
            "Toggle on/off \"Cogfly Warrior\" which is a custom variant of Cogfly red tool.\n" +
            "\n" +
            "<Cogfly Warrior>\n" +
            "- Has its own damage and attack cooldown scaling from Wisdom.\n" +
            "- Has no max hit limit, meaning the minion will never die + it auto-summons itself if some game " +
            "event ends up destroying it (e.g. cutscenes).\n" +
            "- Total cogfly amount limited to 1, final milestone increases this by 1.\n" +
            "- While Cogfly Warrior is enabled, you can no longer use Cogfly tool.";
        public static string WisdomMilestone3 =>
            "Wispfire Lantern silk cost removed, attack delay halved and spawn cooldown is down to 2 seconds.\n" +
            "=> Wispfire Lantern has no Wisdom stat scaling, only this and the next milestone buffs it.";
        public static string WisdomMilestone4 =>
            "All of Wisdom's offensive abilities buffed:\n" +
            "- Silk Nails: Can now critically hit.\n" +
            "- Cogfly Warrior: summons a second cogfly.\n" +
            "- Wispfire Lantern: attack cooldown down to 1/3 of original, spawns a wisp every second.";

        public static string LuckMilestone1 =>
            "Can toggle \"Pinwielder\" on/off. All of the Luck milestone bonuses require this state to " +
            "be active.\n" +
            "\n" +
            "<Pinwielder>\n" +
            "- Focuses on 3 pin tools: Straight Pin, Threefold Pin and Longpin.\n" +
            "- Has infinite ranged attack, but with very low damage output in early game. Relies a lot on " +
            "higher crit chance, milestone bonuses and buffs from other stat trees. Later on with final " +
            "milestones, high crit chance and various other synergies, damage output will be very high.\n" +
            "- Later milestones will also utilize Thread Storm silk skill:\n" +
            "\n" +
            "=> Pinwielder's Thread Storm effect:\n" +
            "- Casts the Thread Storm silk skill normally except it spawns on top of enemy. Skill scales from " +
            "silk skill damage bonuses.\n" +
            "- Only one instance of Thread Storm can be active at a time: effect cannot be re-triggered until " +
            "previous one ends.\n" +
            "- You can no longer cast Thread Storm skill normally.\n" +
            "\n" +
            "# Initial bonuses #\n" +
            "- Straight Pin: has infinite charges, but deals 1/4 of its normal damage (rounded up to the next " +
            "integer value)\n" +
            "- Threefold Pin & Longpin: can now critically hit.";
        public static string LuckMilestone2 =>
            "Straight Pin: can now critically hit + crits give 1 silk.\n" +
            "Threefold Pin: crits get +8 damage which is applied before Dexterity crit multiplier.";
        public static string LuckMilestone3 =>
            "Longpin: casts Thread Storm on crit. " +
            $"Effect lasts for {LuckStat.ThreadStormDuration} seconds.";
        public static string LuckMilestone4 =>
            "- Straight Pin: crit casts Thread Storm with effect duration of " +
            $"{LuckStat.ThreadStormDuration} seconds.\n" +
            "- Threefold Pin: crit damage bonus buffed (+8 -> +30).\n" +
            "- Longpin: now always casts Thread Storm on enemy hit (no crit required), effect lasts " +
            $"{LuckStat.ThreadStormDurationExtended} seconds.";

        public static string GetGlobalInfoContent(GlobalMilestone m)
        {
            var pd = PlayerData.instance;

            switch (m)
            {
                case GlobalMilestone.CURRENCYBASE:
                    return
                    "★ Defeat \"Moss Mother\" boss ★\n" +
                    "Increases the base amount of rosary beads and shell shards dropped by enemies.\n" +
                    $"Starts at +1 and gains another +1 every {GlobalStat.ExtraRosaryThreshold} levels.";

                case GlobalMilestone.BENCHTELEPORT:
                    return
                    "★ Defeat \"Bell Beast\" boss & Obtain a Map + Bench Pins from Shakra ★\n" +
                    "Unlocks access to bench fast travel.";

                case GlobalMilestone.QOLTOOLS:
                    return
                    $"★ Reach level {GlobalStat.QoLToolsLevelRequirement} ★\n" +
                    "The effects of the \"Compass\" and \"Magnetite Brooch\" are always active once you obtain " +
                    "the corresponding tool.";

                case GlobalMilestone.SHARDREFILL:
                    return
                    $"★ Reach level {GlobalStat.ShardRefillLevelRequirement} & Defeat \"Cogwork Dancers\" boss ★\n" +
                    "Shell Shards are fully replenished during bench visits.";

                case GlobalMilestone.CURRENCYMULTIPLIER:
                    return
                    $"★ Reach level {GlobalStat.CurrencyBuffLevelRequirement} & Obtain Threefold Melody ★\n" +
                    $"All enemies drop {GlobalStat.CurrencyMultiplier}x rosaries and shell shards.";

                case GlobalMilestone.MAGGOTREPEL:
                    return
                    $"★ Obtain 25 maps (current count: {pd.MapCount}) ‹- OR -› Defeat \"Groal the Great\" boss ★\n" +
                    "\"Wreath of Purity\" effect is always active once you obtain the tool.";

                case GlobalMilestone.SILKREGEN:
                    return
                    "★ Ascend from the Abyss ★\n" +
                    $"Passively regenerate 1 silk every {GlobalStat.SilkRegenInterval} seconds.";

                case GlobalMilestone.RESPECFREE:
                    return
                    "★ Obtain any of the old hearts in Act 3 ★\n" +
                    "Respec cost removed.";

                default: return "";
            }
        }
    }
}