using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using GlobalEnums;
using HarmonyLib;
using UnityEngine;

namespace BonfireSilksong
{
    /// <summary>
    /// Leveling system logic. 
    /// 1. All Bonfire.instance.PlayerStatus write operations should also be done here.
    /// 2. All methods should be instanced: accessing leveling system through a single shared LevelingSystem.Instance 
    /// makes code clearer without mixing instanced/static calls, even if it technically violates OOP principles where 
    /// non-instance methods should be always static.
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

        public static LevelingSystem Instance
        {
            get => _instance;
            private set => _instance = value;
        }

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

            int relicsObtained = CachedRelicsObtained;
            if (relicsObtained < 0)
            {
                Bonfire.Log.LogError("Failed to fetch relic data");
                return;
            }

            int availableRelicLevels = relicsObtained - s.ConsumedRelicLevels
                                     + s.RespecRelicLevels - s.PendingRelicLevels;

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
        /// Commits the pending stat allocations, deducting rosaries and relic levels
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
                "Level up applied: "
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

            s.RespecRelicLevels = 0;
            s.RespecRelicLevels += relicLevelsSpent;

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

        public int GetAvailableRelicLevels()
        {
            var s = Bonfire.Instance.Status;
            return CachedRelicsObtained - s.ConsumedRelicLevels + s.RespecRelicLevels - s.PendingRelicLevels;
        }

        /// <summary>
        /// Helper for caching obtained relics data; otherwise bench ui would re-calculate this every single frame.
        /// To access cached data, use CachedRelicsObtained getter.
        /// </summary>
        public void RefreshRelicsObtained()
        {
            _cachedRelicsObtained = RelicsObtained();
        }
        public int CachedRelicsObtained => _cachedRelicsObtained;

        /// <summary>
        /// Update blue mask count from Resilience stat.
        /// </summary>
        public static void UpdateBlueHP()
        {
            Bonfire.Log.LogDebug("Called UpdateBlueHP");
            Instance.StartCoroutine(RefreshResilienceBlueHealth());
        }

        /// <summary>
        /// Delays blue up update: waits until player is detected at bench, then adds a small extra delay.
        /// Should be used after fast travel teleport finishes as it will trigger normal UpdateBlueHP call prematurely,
        /// then fix any possible visual issues with another update loop.
        /// </summary>
        public static IEnumerator CallDelayedUpdateBlueHP()
        {
            yield return new WaitUntil(() => PlayerData.instance.atBench);
            yield return new WaitForSecondsRealtime(0.5f);
            UpdateBlueHP();
        }

        // Override bind cost gameplay value in FSM.
        // SilkSpool_BindCost_Getter handles the patching of getter value which is used for visually matching the silk 
        // spool with current silk available.
        public static void UpdateBindCostFSM()
        {
            // the "Silk Cost" variable below is responsible for many different silk functions and therefore must
            // only run if player is in active binding state.
            // Interestingly this issue arose only after testing Silk Soar: normal harpoon would work just fine
            // even if cost was set well above it's usual of 1, but soaring would always check the updated cost. 
            if (!GameManager.instance.hero_ctrl.cState.isBinding) return;

            if (HeroController.instance == null || Instance == null) return;

            int cost = BindCost(Bonfire.Instance.Status.IntelligenceStat);

            foreach (var fsm in HeroController.instance.GetComponents<PlayMakerFSM>())
            {
                foreach (var variable in fsm.FsmVariables.IntVariables)
                {
                    if (variable.Name == "Silk Cost" || variable.Name == "Silk Cost Witch")
                        variable.Value = cost;
                }
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


        private int _cachedRelicsObtained = 0;

        private int _critRoll = 0;
        public bool _crit = false;
        private float _silkRegenTime = 0f;
        private float _toolRegenTime = 0f;
        public int _hitsSinceShielded = 0;
        private bool _blockNextHit = false;

        private static LevelingSystem _instance;
        private static bool _blueHpAddingInProgress = false;

        private static readonly FieldInfo IsHeroDamageField = typeof(DamageEnemies)
            .GetField("isHeroDamage", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo SourceIsHeroField = typeof(DamageEnemies)
            .GetField("sourceIsHero", BindingFlags.Instance | BindingFlags.NonPublic);

        private void Awake()
        {
            _instance = this;
        }

        // counts all tier 3-5 relics the player has given to relic seeker npc
        private static int RelicsObtained()
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

        // helper for UpdateBlueHP
        private static bool BlueHpCheckInvOrMenu()
        {
            if (PlayerData.instance.isInventoryOpen ||
                GameManager.instance.isPaused)
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
            yield return new WaitForSecondsRealtime(0.25f);

            if (_blueHpAddingInProgress) yield break;
            _blueHpAddingInProgress = true;

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

            yield return new WaitForSecondsRealtime(0.25f);
            if (BlueHpCheckInvOrMenu()) yield break;

            int masks = ExtraMasks(Bonfire.Instance.Status.ResilienceStat);

            if (masks >= 1)
            {
                for (int i = 0; i < masks; i++)
                {
                    if (Bonfire.CheckIfNoGameManager()) yield break;

                    if (BlueHpCheckInvOrMenu()) yield break;

                    EventRegister.SendEvent(EventRegisterEvents.AddBlueHealth);
                    yield return new WaitForSecondsRealtime(0.15f);

                    if (HeroController.instance.IsInLifebloodState)
                    {
                        HeroController.instance.HitMaxBlueHealthBurst();
                        yield return new WaitForSecondsRealtime(0.25f);

                        _blueHpAddingInProgress = false;

                        yield break;
                    }
                }
            }

            if (BlueHpCheckInvOrMenu()) yield break;

            yield return new WaitForSecondsRealtime(0.1f);

            _blueHpAddingInProgress = false;
        }

        // rosary and shell shard drop quantities from enemies
        private static int GetRosaries(string size, HealthManager enemy)
        {
            FieldInfo fi = enemy.GetType().GetField(size + "GeoDrops", BindingFlags.NonPublic | BindingFlags.Instance);
            object rosaries = fi.GetValue(enemy);
            return rosaries == null ? 0 : (int)rosaries;
        }

        private static int GetShellShards(HealthManager enemy)
        {
            FieldInfo fi = enemy.GetType().GetField("shellShardDrops", BindingFlags.NonPublic | BindingFlags.Instance);
            object shards = fi.GetValue(enemy);
            return shards == null ? 0 : (int)shards;
        }

        // lucky dice visual effect trigger when the player blocks a hit (HealthManager_TakeDamage)
        private static void LuckyShieldSetActive()
        {
            var hc = HeroController.instance;

            var effect = (GameObject)typeof(HeroController)
                .GetField("spawnedLuckyDiceShieldEffect", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(hc);

            if (effect != null)
            {
                effect.SetActive(false);
                effect.SetActive(true); // toggle off/on to retrigger the effect, matching vanilla LuckyDice behavior
            }
        }

        // crit visuals
        // the visual effect in HealthManager_Hit (FlashGrimmflame) only applies to the player. To also display effect 
        // on the enemy, DamageEnemies_DoDamage needs to explicitly check if damage was inflicted by player.
        private static bool IsHeroDamage(DamageEnemies instance)
        {
            bool a = (bool)(IsHeroDamageField?.GetValue(instance) ?? false);
            bool b = (bool)(SourceIsHeroField?.GetValue(instance) ?? false);
            return a || b;
        }

        // force config data saving when charms are updated
        // as leveling up and respeccing needs these, it's important to call CharmUpdate inside them in LevelingSystem
        [HarmonyPatch(typeof(HeroController), "CharmUpdate")]
        class HeroController_CharmUpdate
        {
            static void Postfix()
            {
                Instance.RefreshRelicsObtained();
                Bonfire.Instance.SaveSettings();
            }
        }

        // rosary/shell drop scaling from luck stat. Also fills enemy hp bar data for bench GUI.
        [HarmonyPatch(typeof(HealthManager), "OnEnable")]
        class HealthManager_OnEnable
        {
            static void Postfix(HealthManager __instance)
            {
                if (__instance != null)
                {
                    var ls = Instance;
                    var i = Bonfire.Instance;
                    var s = i.Status;
                    var gui = BonfireGUI.Settings;

                    if (!gui.EnemyMaxHp.ContainsKey(__instance))
                    {
                        gui.EnemyMaxHp.Add(__instance, __instance.hp);
                        gui.EnemyIsBoss.Add(__instance, BossSceneController.Instance != null);
                    }

                    Bonfire.Log.LogDebug("[Vanilla] rosary and shell shard values:");
                    Bonfire.Log.LogDebug($"small={GetRosaries("small", __instance)}");
                    Bonfire.Log.LogDebug($"medium={GetRosaries("medium", __instance)}");
                    Bonfire.Log.LogDebug($"large={GetRosaries("large", __instance)}");
                    Bonfire.Log.LogDebug($"shards={GetShellShards(__instance)}");

                    Bonfire.Log.LogDebug("[Bonfire] rosary and shell shard values:");
                    Bonfire.Log.LogDebug($"small={IncreaseRosaries(GetRosaries("small", __instance), s.LuckStat)}");
                    Bonfire.Log.LogDebug($"medium={IncreaseRosaries(GetRosaries("medium", __instance), s.LuckStat)}");
                    Bonfire.Log.LogDebug($"large={IncreaseRosaries(GetRosaries("large", __instance), s.LuckStat)}");
                    Bonfire.Log.LogDebug($"shards={IncreaseRosaries(GetShellShards(__instance), s.LuckStat)}");

                    __instance.SetGeoSmall(IncreaseRosaries(GetRosaries("small", __instance), s.LuckStat));
                    __instance.SetGeoMedium(IncreaseRosaries(GetRosaries("medium", __instance), s.LuckStat));
                    __instance.SetGeoLarge(IncreaseRosaries(GetRosaries("large", __instance), s.LuckStat));

                    __instance.SetShellShards(IncreaseRosaries(GetShellShards(__instance), s.LuckStat));
                }
            }
        }

        // update lifeblood masks when sitting down on a bench
        [HarmonyPatch(typeof(HeroController), "UpdateBlueHealth")]
        class HeroController_UpdateBlueHealth
        {
            static void Postfix()
            {
                UpdateBlueHP();
            }
        }

        // binding cost reduction (visually match the FSM variable value by updating silk spool)
        [HarmonyPatch(typeof(SilkSpool), "BindCost", MethodType.Getter)]
        class SilkSpool_BindCost_Getter
        {
            static void Postfix(ref float __result)
            {
                if (PlayerData.instance.IsAnyCursed) return;

                // in HK, soul container has range 0-99, focus costs 33 soul
                // in HK:SS, silk container has range 0-9, with all upgrades reaching 0-18, binding costs 3 silk
                // In Hk, range is then divided by 33, in silksong by 9
                // => Silksong scaling is not as smooth, but ramps a bit harder on high intelligence
                int cost = BindCost(Bonfire.Instance.Status.IntelligenceStat);

                __result = cost;
            }
        }

        // needle attack speed
        [HarmonyPatch(typeof(HeroController), "DidAttack")]
        class HeroController_DidAttack
        {
            static void Postfix(HeroController __instance)
            {
                var config = __instance.Config;
                if (config == null) return;

                float attackSpeed = AttackSpeed(Bonfire.Instance.Status.DexterityStat);

                typeof(HeroController)
                    .GetField("attack_cooldown", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(__instance, config.AttackCooldownTime / attackSpeed);
            }
        }

        [HarmonyPatch(typeof(HeroController), "Attack")]
        class HeroController_Attack
        {
            static void Postfix(HeroController __instance)
            {
                var config = __instance.Config;
                if (config == null) return;

                float attackSpeed = AttackSpeed(Bonfire.Instance.Status.DexterityStat);

                typeof(HeroController)
                    .GetField("attackDuration", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(__instance, config.AttackDuration / attackSpeed);
            }
        }

        // damage blocking based on Resilience stat
        [HarmonyPatch(typeof(HeroController), "TakeDamage")]
        class HealthManager_TakeDamage
        {
            static void Prefix(HeroController __instance, HazardType hazardType)
            {
                if (__instance.cState.evading || __instance.cState.invulnerable) return;

                Bonfire.Log.LogDebug("Took damage from hazard type: " + hazardType);

                if (hazardType != HazardType.ENEMY) return; // only block enemy attacks

                var s = Bonfire.Instance.Status;
                if (s.ResilienceStat <= 0) return;

                int hitsSinceShielded = Instance._hitsSinceShielded;
                if (hitsSinceShielded > 7)
                    hitsSinceShielded = 7;

                float num = UnityEngine.Random.Range(1, 100);
                float blockChance = BlockChance(s.ResilienceStat);
                float multiplier = 0f;

                switch (hitsSinceShielded)
                {
                    case 1: multiplier = 10f; break;
                    case 2: multiplier = 20f; break;
                    case 3: multiplier = 30f; break;
                    case 4: multiplier = 50f; break;
                    case 5: multiplier = 70f; break;
                    case 6: multiplier = 80f; break;
                    case 7: multiplier = 90f; break;
                }

                Bonfire.Log.LogDebug($"Range value {num} <= {multiplier} * {blockChance}");
                if (multiplier > 0 && num <= multiplier * blockChance)
                {
                    Instance._hitsSinceShielded = 0;
                    LuckyShieldSetActive();
                    Instance._blockNextHit = true;
                    Bonfire.Log.LogDebug("Blocked a hit!");
                }
                else
                {
                    Instance._hitsSinceShielded++;
                }
            }

            static void Postfix()
            {
                // safety feature to ensure that block flag is set to 'false' after TakeDamage and its internal 
                // TakeHealth call. Otherwise flag could get stuck on 'true', making player invulnerable
                Instance._blockNextHit = false;
            }
        }

        [HarmonyPatch(typeof(PlayerData), "TakeHealth")]
        class PlayerData_TakeHealth
        {
            static void Prefix(ref int amount)
            {
                if (Bonfire.Instance.Status.ResilienceStat <= 1) return;
                if (Instance._blockNextHit)
                {
                    amount = 0;
                    Instance._blockNextHit = false;
                }
            }
        }

        // extra silk gain when hitting enemies with needle
        [HarmonyPatch(typeof(HeroController), "SilkGain", new[] { typeof(HitInstance) })]
        class HeroController_SilkGain
        {
            static void Postfix(HitInstance hitInstance)
            {
                if (hitInstance.SilkGeneration == HitSilkGeneration.None) return;
                if (hitInstance.SilkGeneration == HitSilkGeneration.FirstHit && !hitInstance.IsFirstHit) return;
                if (PlayerData.instance.atBench) return;

                int extraSilk = ExtraSilk(Bonfire.Instance.Status.WisdomStat, 1) - 1;
                if (extraSilk > 0)
                    HeroController.instance.AddSilk(extraSilk, heroEffect: false);
            }
        }

        // clean enemy hp bars, handle crit roll and passive silk/tool regen from wisdom/artifice stat respectively
        [HarmonyPatch(typeof(HeroController), "Update")]
        class HeroController_Update
        {
            static void Postfix()
            {
                var gui = BonfireGUI.Settings;
                var toRemove = new List<HealthManager>();

                foreach (var e in gui.EnemyMaxHp.Keys)
                {
                    if (e == null || e.hp <= 0)
                        toRemove.Add(e);
                }
                foreach (var e in toRemove)
                {
                    gui.EnemyMaxHp.Remove(e);
                    gui.EnemyIsBoss.Remove(e);
                }

                if (GameManager.instance.inputHandler.inputActions.Attack.WasPressed == true)
                    Instance._critRoll = UnityEngine.Random.Range(1, 100);

                if (HeroController.instance == null || PlayerData.instance == null)
                    return;

                try
                {
                    var i = Instance;
                    float dt = Time.deltaTime;

                    // passive silk regen
                    if (i._silkRegenTime > 0)
                    {
                        i._silkRegenTime -= dt;
                    }
                    else
                    {
                        int silkRegenInterval = SilkRegenInterval(Bonfire.Instance.Status.WisdomStat);
                        if (silkRegenInterval <= 0)
                        {
                            i._silkRegenTime = 1f;
                            return;
                        }

                        i._silkRegenTime = silkRegenInterval;
                        HeroController.instance.AddSilk(1, heroEffect: false);

                        Bonfire.Log.LogDebug("Regenerated 1 silk");
                    }

                    // passive tool regen: this will replenish charges for ALL red tools currently equipped
                    if (i._toolRegenTime > 0)
                    {
                        i._toolRegenTime -= dt;
                    }
                    else
                    {
                        Bonfire.Log.LogDebug("Checking tool regen conditions");
                        int toolRegenInterval = ToolRegenInterval(Bonfire.Instance.Status.ArtificeStat);
                        if (toolRegenInterval <= 0)
                        {
                            i._toolRegenTime = 1f;
                            return;
                        }

                        i._toolRegenTime = toolRegenInterval;
                        foreach (var tool in Resources.FindObjectsOfTypeAll<ToolItem>())
                        {
                            if (tool.IsEquipped && tool.IsAutoReplenished() && tool.CanReload())
                                tool.CollectFree(1);
                            // tool.ReloadSingle() would do the same, but costs shards
                        }

                        Bonfire.Log.LogDebug("Replenished 1 charge for all red tools");
                    }
                }
                catch { }
            }
        }

        // Wisdom stat: remove Wispfire Lantern cost after enough points are spent.
        // First check if player has 0 silk: if yes, add one silk to spawn a wisp. Then refund this in TakeSilk
        // method.
        // This will naturally work as a weaker Weavelight tool: every time the game attempts to spawn a wisp and 
        // player has no silk, it will one for free assuming they have enough points in Wisdom.
        [HarmonyPatch(typeof(HeroWispLantern), "SpawnWispsRoutine")]
        class HeroWispLantern_SpawnWispsRoutine
        {
            static void Prefix()
            {
                var hc = HeroController.instance;

                if (Bonfire.Instance.Status.WisdomStat >= 30 && hc.playerData.silk <= 0)
                    hc.AddSilk(1, heroEffect: false);
            }
        }

        [HarmonyPatch(typeof(HeroController), "TakeSilk", new[] { typeof(int), typeof(SilkSpool.SilkTakeSource) })]
        class HeroController_TakeSilk
        {
            static void PostFix(HeroController __instance, int amount, SilkSpool.SilkTakeSource source)
            {
                if (source == SilkSpool.SilkTakeSource.Wisp && Bonfire.Instance.Status.WisdomStat >= 30)
                {
                    __instance.AddSilk(amount, heroEffect: false);
                }
            }
        }

        // handle crit roll (prefix) and crit effect when enemy gets hit (postfix)
        // DamageEnemies_DoDamage calls patched HealthManager_Hit internally so logic goes:
        // - Prefix rolls crit chance
        // - DoDamage calls Hit at some point
        // - Hit checks and handles player's perspective by adding damage and displaying visual effect on player
        // - Postfix adds the effect on enemy that got hit
        [HarmonyPatch(typeof(DamageEnemies), "DoDamage", new[] { typeof(GameObject), typeof(bool) })]
        class DamageEnemies_DoDamage
        {
            static void Prefix(DamageEnemies __instance, GameObject target)
            {
                if (target == null || target.layer != 11) return;
                if (!IsHeroDamage(__instance)) return;

                var s = Bonfire.Instance.Status;

                int critChance = CritChance(s.LuckStat);
                if (critChance < 1) // must have at least 1% crit chance
                {
                    Instance._crit = false;
                    return;
                }

                int critRoll = Instance._critRoll;

                Bonfire.Log.LogDebug($"Crit chance: {critChance}. Rolled {critRoll}.");

                Instance._crit = critRoll <= critChance;
            }

            static void Postfix(DamageEnemies __instance, GameObject target, bool __result)
            {
                if (!__result || target == null || target.layer != 11) return;
                if (!IsHeroDamage(__instance)) return;

                if (Instance._crit)
                {
                    // effect comes from prefab value, these can have both visual and sound included. 
                    // Prefer one with no audio + only mild visuals; you will be hearing/seeing this effect a lot if
                    // you level up luck stat
                    var prefab = HeroController.instance.takeHitBlackHealthNullifyPrefab;
                    prefab.transform.SetScaleX(0.5f);
                    prefab.transform.SetScaleY(0.5f);
                    Instantiate(prefab, target.transform.position, target.transform.rotation);
                }
            }
        }

        // silk skill, tool and needle base damage
        [HarmonyPatch(typeof(HealthManager), "Hit")]
        class HealthManager_Hit
        {
            static void Prefix(ref HitInstance hitInstance)
            {
                var s = Bonfire.Instance.Status;

                if (hitInstance.Source == null) return;

                string sourceName = hitInstance.Source.name;
                var tool = hitInstance.RepresentingTool;

                Bonfire.Log.LogDebug("Recorded a hit => Hit source: " + sourceName);
                Bonfire.Log.LogDebug("Tool instance: " + tool?.name ?? "null");

                if (tool != null && tool.Type == ToolItemType.Skill)
                {
                    // -- silk skills --
                    // the "Type == ToolItemType.Skill" should already target only the silk skills, but in case exact 
                    // names are needed for hitInstance.RepresentingTool.name, these are:
                    //   Silk Spear, Thread Sphere, Parry, Silk Charge, Silk Bomb, Silk Boss Needle
                    Bonfire.Log.LogDebug($"[Vanilla] Silk Skill: {tool}. Damage: {hitInstance.DamageDealt}");

                    hitInstance.DamageDealt = SpellDamage(hitInstance.DamageDealt, s.IntelligenceStat);

                    Bonfire.Log.LogDebug($"[Bonfire] Silk Skill: {tool}. Damage: {hitInstance.DamageDealt}");
                }
                else if (tool != null && (tool.Type == ToolItemType.Red || tool.Type == ToolItemType.Blue))
                {
                    // -- red and blue tools --
                    Bonfire.Log.LogDebug($"[Vanilla] Tool: {tool}. Damage: {hitInstance.DamageDealt}");

                    hitInstance.DamageDealt = ToolDamage(hitInstance.DamageDealt, s.ArtificeStat);

                    Bonfire.Log.LogDebug($"[Bonfire] Tool: {tool}. Damage: {hitInstance.DamageDealt}");
                }
                else if (sourceName.Contains("Slash") || sourceName == "Needle Damage")
                {
                    // -- basic needle hits + needle strike --
                    // 1. "Slash" can be used here, it includes Slash, UpSlash, DownSlash, and AltSlash + Charge Slash 
                    // variants for different crests (these are simply "Charge Slash <crest_name>") which should 
                    // be enough. 
                    // 2. Hollow Knight's mod used "lash", but this would also include something like "Splash Surface" 
                    // which gets recorded if player hits water surface
                    // 3. Finally, the "Needle Damage" check is probably not needed, but there's no harm in including it
                    Bonfire.Log.LogDebug($"[Vanilla] Damage for {sourceName} = {hitInstance.DamageDealt}");

                    hitInstance.DamageDealt = NeedleDamage(s.StrengthStat);

                    Bonfire.Log.LogDebug($"[Bonfire] Damage for {sourceName} = {hitInstance.DamageDealt}");

                    if (Instance._crit)
                    {
                        hitInstance.DamageDealt = CritDamage(s.DexterityStat, hitInstance.DamageDealt);

                        Bonfire.Log.LogDebug($"[Crit] Damage for {sourceName} = {hitInstance.DamageDealt}");

                        HeroController.instance.GetComponent<SpriteFlash>().FlashGrimmflame();
                        Instance._crit = false;
                    }
                }
            }
        }
    }
}