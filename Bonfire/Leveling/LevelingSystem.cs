using System;
using System.Collections;
using System.Linq;

using GlobalEnums;
using HutongGames.PlayMaker;
using UnityEngine;

namespace BonfireSilksong
{
    /// <summary>
    /// Leveling system logic.
    /// </summary>
    public class LevelingSystem : MonoBehaviour
    {
        public int StrengthIncrease = 0;
        public int DexterityIncrease = 0;
        public int IntelligenceIncrease = 0;
        public int ArtificeIncrease = 0;
        public int ResilienceIncrease = 0;
        public int WisdomIncrease = 0;
        public int LuckIncrease = 0;

        public static bool SatDownAtBench = false;
        public static bool BlueHpInit = false;
        public static LevelingSystem Instance
        {
            get => _instance;
            private set => _instance = value;
        }

        // stat requirements for secondary effects + their check methods. 
        // int getters are for direct values e.g. GUI display, bool getters for logic checks.
        // must be 'static' to allow Harmony patcher access.
        public static int StrengthMilestone_1 => 5;
        public static int StrengthMilestone_2 => 10;
        public static int StrengthMilestone_3 => 15;
        public static bool CheckStrengthMilestone_1 =>
            Bonfire.Instance.Status.StrengthStat + Instance.StrengthIncrease >= StrengthMilestone_1;
        public static bool CheckStrengthMilestone_2 =>
            Bonfire.Instance.Status.StrengthStat + Instance.StrengthIncrease >= StrengthMilestone_2;
        public static bool CheckStrengthMilestone_3 =>
            Bonfire.Instance.Status.StrengthStat + Instance.StrengthIncrease >= StrengthMilestone_3;

        public static int DexterityMilestone_1 => 0;
        public static int DexterityMilestone_2 => 0;
        public static int DexterityMilestone_3 => 0;
        public static bool CheckDexterityMilestone_1 =>
            Bonfire.Instance.Status.DexterityStat + Instance.DexterityIncrease >= DexterityMilestone_1;
        public static bool CheckDexterityMilestone_2 =>
            Bonfire.Instance.Status.DexterityStat + Instance.DexterityIncrease >= DexterityMilestone_2;
        public static bool CheckDexterityMilestone_3 =>
            Bonfire.Instance.Status.DexterityStat + Instance.DexterityIncrease >= DexterityMilestone_3;

        public static int IntelligenceMilestone_1 => 0;
        public static int IntelligenceMilestone_2 => 0;
        public static int IntelligenceMilestone_3 => 0;
        public static bool CheckIntelligenceMilestone_1 =>
            Bonfire.Instance.Status.IntelligenceStat + Instance.IntelligenceIncrease >= IntelligenceMilestone_1;
        public static bool CheckIntelligenceMilestone_2 =>
            Bonfire.Instance.Status.IntelligenceStat + Instance.IntelligenceIncrease >= IntelligenceMilestone_2;
        public static bool CheckIntelligenceMilestone_3 =>
            Bonfire.Instance.Status.IntelligenceStat + Instance.IntelligenceIncrease >= IntelligenceMilestone_3;

        public static int ArtificeMilestone_1 => 0;
        public static int ArtificeMilestone_2 => 0;
        public static int ArtificeMilestone_3 => 0;
        public static bool CheckArtificeMilestone_1 =>
            Bonfire.Instance.Status.ArtificeStat + Instance.ArtificeIncrease >= ArtificeMilestone_1;
        public static bool CheckArtificeMilestone_2 =>
            Bonfire.Instance.Status.ArtificeStat + Instance.ArtificeIncrease >= ArtificeMilestone_2;
        public static bool CheckArtificeMilestone_3 =>
            Bonfire.Instance.Status.ArtificeStat + Instance.ArtificeIncrease >= ArtificeMilestone_3;

        public static int ResilienceMilestone_1 => 0;
        public static int ResilienceMilestone_2 => 0;
        public static int ResilienceMilestone_3 => 0;
        public static int ResilienceMilestone_4 => 0;
        public static bool CheckResilienceMilestone_1 =>
            Bonfire.Instance.Status.ResilienceStat + Instance.ResilienceIncrease >= ResilienceMilestone_1;
        public static bool CheckResilienceMilestone_2 =>
            Bonfire.Instance.Status.ResilienceStat + Instance.ResilienceIncrease >= ResilienceMilestone_2;
        public static bool CheckResilienceMilestone_3 =>
            Bonfire.Instance.Status.ResilienceStat + Instance.ResilienceIncrease >= ResilienceMilestone_3;

        public static int WisdomMilestone_1 => 20;
        public static int WisdomMilestone_2 => 30;
        public static int WisdomMilestone_3 => 30;
        public static bool CheckWisdomMilestone_1 =>
            Bonfire.Instance.Status.WisdomStat + Instance.WisdomIncrease >= WisdomMilestone_1;
        public static bool CheckWisdomMilestone_2 =>
            Bonfire.Instance.Status.WisdomStat + Instance.WisdomIncrease >= WisdomMilestone_2;
        public static bool CheckWisdomMilestone_3 =>
            Bonfire.Instance.Status.WisdomStat + Instance.WisdomIncrease >= WisdomMilestone_3;

        public static int LuckMilestone_1 => 0;
        public static int LuckMilestone_2 => 0;
        public static int LuckMilestone_3 => 0;
        public static bool CheckLuckMilestone_1 =>
            Bonfire.Instance.Status.LuckStat + Instance.LuckIncrease >= LuckMilestone_1;
        public static bool CheckLuckMilestone_2 =>
            Bonfire.Instance.Status.LuckStat + Instance.LuckIncrease >= LuckMilestone_2;
        public static bool CheckLuckMilestone_3 =>
            Bonfire.Instance.Status.LuckStat + Instance.LuckIncrease >= LuckMilestone_3;


        /// <summary>
        /// Increments the pending allocation for a single stat.
        /// </summary>
        public void IncreaseStat(string stat, PlayerStatus s, int rosariesToLvlUp)
        {
            if (stat == "Strength") StrengthIncrease++;
            else if (stat == "Dexterity") DexterityIncrease++;
            else if (stat == "Intelligence") IntelligenceIncrease++;
            else if (stat == "Artifice") ArtificeIncrease++;
            else if (stat == "Resilience") ResilienceIncrease++;
            else if (stat == "Wisdom") WisdomIncrease++;
            else LuckIncrease++;

            if (!CheckStatLevels()) return;

            int relicsObtained = _cachedRelicsDeposited;
            if (relicsObtained < 0)
            {
                Bonfire.Log.LogError("Failed to fetch relic data");
                return;
            }

            int availableRelicLevels = GetAvailableRelicLevels();

            if (availableRelicLevels > 0)
            {
                s.PendingRelicLevels++;
            }
            else
            {
                s.PendingRosaries += rosariesToLvlUp;
                s.PendingRosaryLevels++;
            }
        }

        /// <summary>
        /// Commits the pending stat allocations, deducting rosaries and relic levels.
        /// </summary>
        public void ApplyLevel()
        {
            var s = Bonfire.Instance.Status;

            int fromRespecPool = Math.Min(s.PendingRelicLevels, s.RespecRelicLevels);
            int fromNewDeposits = s.PendingRelicLevels - fromRespecPool;

            s.RespecRelicLevels -= fromRespecPool;
            s.ConsumedRelicLevels += fromNewDeposits;
            s.PendingRelicLevels = 0;

            PlayerData.instance.TakeGeo(s.PendingRosaries);
            HeroController.instance.TakeGeo(s.PendingRosaries);
            s.TotalSpentRosaries += s.PendingRosaries;
            s.PendingRosaries = 0;

            s.TotalRosaryLevels += s.PendingRosaryLevels;
            s.PendingRosaryLevels = 0;

            s.StrengthStat += StrengthIncrease;
            s.DexterityStat += DexterityIncrease;
            s.IntelligenceStat += IntelligenceIncrease;
            s.ArtificeStat += ArtificeIncrease;
            s.ResilienceStat += ResilienceIncrease;
            s.WisdomStat += WisdomIncrease;
            s.LuckStat += LuckIncrease;

            Bonfire.Log.LogDebug(
                "[LevelingSystem] Level up applied: "
                + StrengthIncrease + " Strength, "
                + DexterityIncrease + " Dexterity, "
                + IntelligenceIncrease + " Intelligence, "
                + ArtificeIncrease + " Artifice, "
                + ResilienceIncrease + " Resilience, "
                + WisdomIncrease + " Wisdom and "
                + LuckIncrease + " Luck."
            );

            StrengthIncrease = 0;
            DexterityIncrease = 0;
            IntelligenceIncrease = 0;
            ArtificeIncrease = 0;
            ResilienceIncrease = 0;
            WisdomIncrease = 0;
            LuckIncrease = 0;

            HeroController.instance.CharmUpdate();
            UpdateBlueHP();
        }

        /// <summary>
        /// Refunds all spent levels and resets stats to 1, consuming silkeaters.
        /// </summary>
        public void Respec()
        {
            var s = Bonfire.Instance.Status;
            var silkEaters = Resources.FindObjectsOfTypeAll<CollectableItem>()
                .FirstOrDefault(i => i.name == "Silk Grub");

            if (silkEaters == null || silkEaters.CollectedAmount < s.RespecCost) return;

            int relicLevelsSpent = s.StrengthStat + s.DexterityStat + s.IntelligenceStat
                                + s.ArtificeStat + s.ResilienceStat + s.WisdomStat + s.LuckStat
                                - (s.TotalRosaryLevels - 1);

            if (s.TotalRosaryLevels == 1 && relicLevelsSpent == 0) return;

            RefreshRelicsDeposited();
            if (CachedRelicsObtained < 0) return;
            s.RespecRelicLevels = s.ConsumedRelicLevels = CachedRelicsObtained;

            PlayerData.instance.AddGeo(s.TotalSpentRosaries);
            HeroController.instance.AddGeo(s.TotalSpentRosaries);
            s.TotalSpentRosaries = 0;
            s.TotalRosaryLevels = 1;

            s.StrengthStat = 0;
            s.DexterityStat = 0;
            s.IntelligenceStat = 0;
            s.ArtificeStat = 0;
            s.ResilienceStat = 0;
            s.WisdomStat = 0;
            s.LuckStat = 0;

            StrengthIncrease = 0;
            DexterityIncrease = 0;
            IntelligenceIncrease = 0;
            ArtificeIncrease = 0;
            ResilienceIncrease = 0;
            WisdomIncrease = 0;
            LuckIncrease = 0;

            s.PendingRosaries = 0;
            s.PendingRosaryLevels = 0;
            s.PendingRelicLevels = 0;

            silkEaters.Take(s.RespecCost);
            s.RespecCost++;

            HeroController.instance.CharmUpdate();
            UpdateBlueHP();
        }

        /// <summary>
        /// Reset pending stat levels.
        /// </summary>
        public void ResetPendingLevelUp()
        {
            var s = Bonfire.Instance.Status;
            var ls = Instance;

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
        }

        /// <summary>
        /// Checks stat level range and prevents leveling beyond stat cap of 99.
        /// </summary>
        public bool CheckStatLevels()
        {
            var s = Bonfire.Instance.Status;
            bool valid = true;

            if (s.StrengthStat < 0) s.StrengthStat = 0;
            else if (s.StrengthStat > 99) s.StrengthStat = 99;
            else if (s.StrengthStat + StrengthIncrease > 99)
            {
                StrengthIncrease--;
                valid = false;
            }

            if (s.DexterityStat < 0) s.DexterityStat = 0;
            else if (s.DexterityStat > 99) s.DexterityStat = 99;
            else if (s.DexterityStat + DexterityIncrease > 99)
            {
                DexterityIncrease--;
                valid = false;
            }

            if (s.IntelligenceStat < 0) s.IntelligenceStat = 0;
            else if (s.IntelligenceStat > 99) s.IntelligenceStat = 99;
            else if (s.IntelligenceStat + IntelligenceIncrease > 99)
            {
                IntelligenceIncrease--;
                valid = false;
            }

            if (s.ArtificeStat < 0) s.ArtificeStat = 0;
            else if (s.ArtificeStat > 99) s.ArtificeStat = 99;
            else if (s.ArtificeStat + ArtificeIncrease > 99)
            {
                ArtificeIncrease--;
                valid = false;
            }

            if (s.ResilienceStat < 0) s.ResilienceStat = 0;
            else if (s.ResilienceStat > 99) s.ResilienceStat = 99;
            else if (s.ResilienceStat + ResilienceIncrease > 99)
            {
                ResilienceIncrease--;
                valid = false;
            }

            if (s.WisdomStat < 0) s.WisdomStat = 0;
            else if (s.WisdomStat > 99) s.WisdomStat = 99;
            else if (s.WisdomStat + WisdomIncrease > 99)
            {
                WisdomIncrease--;
                valid = false;
            }

            if (s.LuckStat < 0) s.LuckStat = 0;
            else if (s.LuckStat > 99) s.LuckStat = 99;
            else if (s.LuckStat + LuckIncrease > 99)
            {
                LuckIncrease--;
                valid = false;
            }

            return valid;
        }

        public int GetAvailableRelicLevels()
        {
            var s = Bonfire.Instance.Status;
            return CachedRelicsObtained - s.ConsumedRelicLevels + s.RespecRelicLevels - s.PendingRelicLevels;
        }

        /// <summary>
        /// Helper for caching obtained relics data; otherwise bench ui would re-calculate this every single frame.
        /// To access cached data, use CachedRelicsObtained getter.
        /// </summary>
        public void RefreshRelicsDeposited()
        {
            _cachedRelicsDeposited = RelicsDeposited();
        }
        public int CachedRelicsObtained => _cachedRelicsDeposited;

        /// <summary>
        /// Update blue mask count from Resilience stat.
        /// </summary>
        public static void UpdateBlueHP()
        {
            Bonfire.Log.LogDebug("[LevelingSystem] Called UpdateBlueHP");
            Instance.StartCoroutine(RefreshResilienceBlueHealth());
        }

        /// <summary>
        /// Resets all initialization flags when in main menu.
        /// </summary>
        public static void ResetInitFlags()
        {
            if (GameManager.instance.GameState == GameState.MAIN_MENU)
            {
                _bindCostInit = false;
                BlueHpInit = false;
            }
        }

        /// <summary>
        /// Delays blue up update: waits until player is detected at bench, then adds a small extra delay.
        /// Should be used after fast travel teleport finishes as it will trigger normal UpdateBlueHP call prematurely,
        /// then fix any possible visual issues with another update loop.
        /// </summary>
        public static IEnumerator CallDelayedUpdateBlueHP()
        {
            yield return new WaitUntil(() => GameManager.instance.GameState == GameState.PLAYING);
            yield return new WaitForSecondsRealtime(0.25f);
            UpdateBlueHP();
        }

        // Override bind cost gameplay value in FSM.
        // SilkSpool_BindCost_Getter handles the patching of getter value which is used for visually matching the silk 
        // spool with current silk available.
        public static void UpdateBindCostFSM()
        {
            // _bindCostInit is just to prevent calling Fsm event search loop continuously: _bindCostInit starts 
            // false, which forces loop to run once and initialize "Silk Cost" and "Silk Cost Witch" int values when 
            // loading into a save. Afterwards it should only repeat this if player is currently binding. And finally: 
            // going back to main menu should reset init value so that init may be repeated after each "save & exit 
            // into load into game" sequence. 
            // Is this absolutely necessary? No, probably doesn't even affect game performance to be honest, but 
            // just a good habit to prevent a pointless execution loop because UpdateBindCostFSM is called on each
            // frame.

            if (!GameManager.instance.hero_ctrl.cState.isBinding && _bindCostInit) return;

            if (HeroController.instance == null || Instance == null) return;

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

                _bindCostInit = true;
            }
        }

        public static bool CanLevelUp(bool gotFreeLevel, int rosariesToLvlUp)
        {
            var s = Bonfire.Instance.Status;
            return PlayerData.instance.atBench &&
                   (gotFreeLevel || (rosariesToLvlUp + s.PendingRosaries <= PlayerData.instance.geo));
        }

        public static int GetCurrentLevel()
        {
            var s = Bonfire.Instance.Status;

            return s.StrengthStat + s.DexterityStat + s.IntelligenceStat
                    + s.ArtificeStat + s.ResilienceStat + s.WisdomStat + s.LuckStat
                    + s.PendingRelicLevels
                    + s.PendingRosaryLevels
                    + 1; // base level
        }

        public static int DisplayPendingRosaryLevel()
        {
            var s = Bonfire.Instance.Status;
            return s.TotalRosaryLevels + s.PendingRosaryLevels;
        }

        public static int GetLevelUpRosaryRequirement(int rosaryLevels)
        {
            return (int)(Math.Pow(rosaryLevels, 2.0) + (10 * rosaryLevels) + 50.0);
        }

        /// <summary>
        /// Returns average damage blocking chance from current Resilience stat value.
        /// </summary>
        public static float AverageBlockChance(int totalRes)
        {
            float f = BlockChance(totalRes);
            float[] multipliers = { 10f, 20f, 30f, 50f, 70f, 80f, 90f };
            float sum = 0f;
            foreach (var m in multipliers)
                sum += m * f;
            return sum / multipliers.Length;
        }

        // -- stat formulas -- //

        // strength: needle damage
        public static int NeedleDamage(int totalStr) =>
            (int)Math.Round((5 + 4 * PlayerData.instance.nailUpgrades) * Math.Pow(1.2, Math.Log(totalStr + 1, 2.0)));

        // dexterity: needle attack speed
        public static float AttackSpeed(int totalDex) =>
            (float)Math.Round(2.7 / (1.0 + 1.82 * Math.Exp(-0.08 * (totalDex + 1))) - 0.01, 2);

        // dexterity: needle crit damage
        public static int CritDamage(int totalDex, int needleDamage) =>
            (int)(needleDamage * (1.2 + Math.Log(totalDex + 1)));

        // intelligence: silk skill damage
        public static int SpellDamage(int baseDamage, int totalInt) =>
            (int)Math.Round(baseDamage * Math.Pow(1.25, Math.Log(totalInt + 1, 2.0)));

        // intelligence: binding (healing) cost
        public static int BindCost(int totalInt) => (int)Math.Round(9.1 * Math.Exp(-0.01 * (totalInt + 2.0)));

        // artifice: red and blue tool damage
        public static int ToolDamage(int baseDamage, int totalArt)
        {
            if (totalArt <= 0) return baseDamage;

            return (int)Math.Round(baseDamage * (1.0 + 2.0 / (1.0 + 15.0 * Math.Exp(-0.1 * totalArt))));
        }

        // artifice: amount of time it takes to passively replenish a single tool charge (without costing any shards)
        // starts from 60 seconds timer at lvl 5: this prevents player from putting just a single point for such an 
        // impactful upgrade
        public static int ToolRegenInterval(int totalArt)
        {
            if (totalArt < 5) return -1;

            return (int)(2.0 + 58.0 * Math.Exp(-0.06 * (totalArt - 5)));
        }

        // resilience: extra lifeblood masks. Caps at 8 on level 40; using a single plasmium from phial will then
        // activate overdose regen effect
        public static int ExtraMasks(int totalRes)
        {
            if (totalRes >= 40) return 8;

            return (int)Math.Round(8.0 * (1.0 - Math.Exp(-0.055 * (totalRes - 1))));
        }

        // resilience: chance to block an enemy hit
        public static float BlockChance(int totalRes) => (float)(0.72 / (1.0 + 12.0 * Math.Exp(-0.09 * totalRes)));

        // wisdom: extra silk gained when striking an enemy with needle (+1 silk for every 20 levels)
        public static int ExtraSilk(int totalWsdm, int baseSilk) => (int)(baseSilk + Math.Floor(totalWsdm / 20f));

        // wisdom: amount of time it takes to passively produce a single silk
        // starts from 40 seconds timer at lvl 5 to prevents players from putting just a single point and
        // reaping the benefits
        public static int SilkRegenInterval(int totalWsdm)
        {
            if (totalWsdm < 5) return -1;

            return (int)(4.0 + 36.0 * Math.Exp(-0.06 * (totalWsdm - 5)));
        }

        // luck: crit chance
        public static int CritChance(int totalLck)
        {
            if (totalLck <= 0) return 0;

            return (int)Math.Round(60.0 / (1.0 + 15.0 * Math.Exp(-0.08 * totalLck)));
        }

        // luck: rosary drop multiplier from enemies.
        // Multiplier per level is additive 5% to current stat e.g. 1x -> 1.05x -> 1.10x -> 1.15x etc.
        public static int IncreaseRosaries(int droppedRosaries, int totalLck) =>
            (int)(droppedRosaries * (1f + (totalLck / 20f)));

        // -- stat formulas end here -- //


        private int _cachedRelicsDeposited = 0;

        private static LevelingSystem _instance;
        private static bool _blueHpAddingInProgress = false;
        private static bool _bindCostInit = false;

        private void Awake()
        {
            _instance = this;
        }

        // counts all tier 3-5 relics the player has given to relic seeker npc
        private static int RelicsDeposited()
        {
            var relicChoralCommandments = Resources.FindObjectsOfTypeAll<CollectableItemRelicType>()
                .FirstOrDefault(r => r.name == "R Seal Chit");

            var relicRuneHarps = Resources.FindObjectsOfTypeAll<CollectableItemRelicType>()
                .FirstOrDefault(r => r.name == "R Weaver Record");

            var relicArcaneEggs = Resources.FindObjectsOfTypeAll<CollectableItemRelicType>()
                .FirstOrDefault(r => r.name == "R Ancient Egg");

            if (relicChoralCommandments == null || relicRuneHarps == null || relicArcaneEggs == null)
                return -1;

            int total = 0;
            foreach (var relic in relicChoralCommandments.Relics
                .Concat(relicRuneHarps.Relics)
                .Concat(relicArcaneEggs.Relics))
            {
                var data = relic.SavedData;
                if (data.IsDeposited) total++;
            }

            return total;
        }

        // helper for RefreshResilienceBlueHealth
        private static bool BlueHpCheckInvOrMenu()
        {
            if (PlayerData.instance.isInventoryOpen ||
                GameManager.instance.isPaused ||
                SatDownAtBench)
            {
                _blueHpAddingInProgress = false;
                return true;
            }

            return false;
        }

        // handles blue health updates for Resilience stat by clearing existing masks then applies update count after.
        // Includes multiple intermediary checks which exit the routine if either inventory or pause menu is opened.
        // It's important to immediately call this method again if this happens: this is already done in LateUpdate 
        // callback.
        //
        // Previously a much simpler logic was used: HeroController_UpdateBlueHealth patch would simply set PlayerData's
        // healthBlue equal to Resilience stat mask amount. This would work just fine except the visuals lagged a 
        // single mask behind. Thus all of the extra code logic is to fix that small but annoying issue + make sure the 
        // masks are also properly preserved after menu interactions. It might still break occasionally if
        // player spams inventory and/or pause on and off repeatedly.
        private static IEnumerator RefreshResilienceBlueHealth()
        {
            Bonfire.Log.LogDebug("[LevelingSystem] blue hp refresh");
            yield return new WaitForSecondsRealtime(0.25f);

            if (_blueHpAddingInProgress) yield break;
            _blueHpAddingInProgress = true;

            if (BlueHpCheckInvOrMenu())
            {
                if (SatDownAtBench)
                {
                    Bonfire.Log.LogDebug("SatDownAtBench is true, call refresh again");
                    SatDownAtBench = false;
                    Instance.StartCoroutine(RefreshResilienceBlueHealth());
                }
                yield break;
            }
            SatDownAtBench = false;

            // removes existing blue hp before applying new values: closing the inventory automatically sets healthBlue 
            // = 0 while closing pause menu doesn't so it's important to do it here manually. HeroCtrl-MaxHealth fsm 
            // then removes mask visual. This way SendEvent for AddBlueHealth can just add the hp without caring 
            // about previous blue mask state.
            // For future updates: if there will be new ways to obtain blue masks which are added during bench visits 
            // e.g. tools similar to Hollow Knight's lifeblood charms, then healthBlue = 0 would override these. In 
            // that case, just introduce a new variable to track base game masks separately then set 
            // healthBlue = base masks and keep current SendEvent logic for modded masks. Even better: updates could
            // make update logic less complex, leading to a complete removal of this Do-It-Yourself implementation.
            PlayerData.instance.healthBlue = 0;
            HeroController.instance.proxyFSM.SendEvent("HeroCtrl-MaxHealth");
            // HeroController.MaxHealth also sends EventRegister.SendEvent(EventRegisterEvents.HeroHealedToMax), but 
            // apparently it's not needed to reset blue mask state

            if (BlueHpCheckInvOrMenu()) yield break;

            yield return new WaitForSecondsRealtime(0.25f);

            int masks = ExtraMasks(Bonfire.Instance.Status.ResilienceStat);

            if (masks >= 1)
            {
                for (int i = 0; i < masks; i++)
                {
                    if (BlueHpCheckInvOrMenu()) yield break;

                    EventRegister.SendEvent(EventRegisterEvents.AddBlueHealth);
                    yield return new WaitForSecondsRealtime(0.15f);

                    if (HeroController.instance.IsInLifebloodState)
                    {
                        HeroController.instance.EnterLifebloodStateQuick();
                        yield return new WaitForSecondsRealtime(0.25f);

                        _blueHpAddingInProgress = false;

                        yield break;
                    }
                }
            }

            if (BlueHpCheckInvOrMenu()) yield break;

            _blueHpAddingInProgress = false;
        }
    }
}