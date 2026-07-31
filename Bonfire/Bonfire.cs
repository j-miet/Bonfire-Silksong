using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GlobalEnums;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BonfireSilksong
{
    /// <summary>
    /// Main class for Bonfire-Silksong mod
    /// </summary>
    [BepInPlugin("hksilksong.mod.bonfire-silksong", "Bonfire-Silksong", "0.6.0")]
    public class Bonfire : BaseUnityPlugin
    {
        public PlayerStatus Status;

        public static Bonfire Instance;
        internal static ManualLogSource Log;

        public void SaveSettings()
        {
            if (GameManager.instance == null) return;

            int saveProfile = GameManager.instance.profileID;
            Log.LogDebug("SaveSettings: save slot " + saveProfile);

            if (saveProfile < 1)
            {
                _firstSave = true;
                return;
            }

            if (_firstSave)
            {
                LoadSettings();
                _firstSave = false;
                return;
            }

            var json = JsonUtility.ToJson(Status, prettyPrint: true);
            File.WriteAllText(SavePath(), json);
        }

        public void LoadSettings()
        {
            if (GameManager.instance == null)
            {
                Status = new PlayerStatus();
                return;
            }

            int saveProfile = GameManager.instance.profileID;
            Log.LogDebug("LoadSettings: save slot " + saveProfile);

            if (saveProfile < 1)
            {
                _firstSave = true;
                Status = new PlayerStatus();
                return;
            }

            if (!File.Exists(SavePath()))
            {
                Status = new PlayerStatus();
                return;
            }

            var json = File.ReadAllText(SavePath());
            Status = JsonUtility.FromJson<PlayerStatus>(json);
        }

        /// <summary>
        /// Display cursor when player sits on a bench in order to access leveling GUI.
        /// Can be used in enabling cursor outside menu screens, but also in map menu screen for fast travel.
        /// If called in Update callbacks, use LateUpdate instead or game will still hide the cursor.
        /// </summary>
        public void ShowCursor()
        {
            if (HeroController.instance != null &&
                HeroController.instance.cState != null &&
                GameManager.instance != null)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = PlayerData.instance.atBench || GameManager.instance.isPaused;
                return;
            }

            if (GameManager.instance != null)
                Cursor.visible = GameManager.instance.isPaused;
        }

        /// <summary>
        /// When saving and exiting, sometimes the game inputs become unresponsive for several seconds in main menu 
        /// before resuming normally. Checking if GameManager is null or if game is still in gameplay scene then 
        /// preventing update loops might help a bit (maybe makes it less frequent?) but doesn't fix this entirely.
        /// </summary>
        public static bool CheckIfNoGameManager()
        {
            if (GameManager.instance == null || !GameManager.instance.IsGameplayScene())
            {
                Log.LogDebug("GameManager instance null or in gameplay scene");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Add blue masks from Resilience stat.
        /// </summary>
        public static void UpdateBlueHP()
        {
            Log.LogDebug("Called UpdateBlueHP");
            Instance.StartCoroutine(AddBlueHealth());
        }


        // _firstSave guard flag: it stays true until GameManager.instance != null and its profileID field has value 
        // greater than 1 which corresponds to a valid save slot (1-4). And every time the user quits to main menu, it 
        // flips back  to true upon detecting invalid profileID. Then, when user continues from another save slot, it 
        // prevents SaveSettings from using previous save's PlayerStatus data to override data for current slot.
        private bool _firstSave = true;
        private bool _invClosedCheck = false;

        private int _critRoll = 0;
        public bool _crit = false;
        private float _silkRegenTime = 0f;
        private float _toolRegenTime = 0f;
        public int _hitsSinceShielded = 0;
        private bool _blockNextHit = false;

        private ConfigEntry<KeyboardShortcut> _enemyHpBarToggleHotkey;
        private bool _enemyHPBarsEnabled = false;

        private static bool _blueHpAddingInProgress = false;

        // -- Unity callbacks -- //

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            Log.LogInfo("Initializing Bonfire-Silksong...");

            _enemyHpBarToggleHotkey = Instance.Config.Bind(
                "Hotkeys",
                "Enemy HP bar Toggle",
                new KeyboardShortcut(KeyCode.H, KeyCode.LeftControl),
                "Toggle enemy HP bars on/off");

            LoadSettings();
            _firstSave = true;

            Harmony.CreateAndPatchAll(typeof(Bonfire).Assembly);

            SceneManager.sceneLoaded += OnSceneLoaded;

            Log.LogInfo("Bonfire-Silksong initialized");
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Update()
        {
            if (CheckIfNoGameManager()) return;

            UpdateBindCostFSM();

            // hotkeys

            // enemy hp bar toggle
            if (_enemyHpBarToggleHotkey.Value.IsDown())
            {
                _enemyHPBarsEnabled = !_enemyHPBarsEnabled;
                Instance.Status.EnemyHealthBarsEnabled = _enemyHPBarsEnabled;
            }
        }

        private void LateUpdate()
        {
            if (CheckIfNoGameManager()) return;

            if (PlayerData.instance == null) return;

            var pd = PlayerData.instance;

            if (pd.atBench)
            {
                if (!pd.isInventoryOpen && !GameManager.instance.isPaused)
                {
                    ShowCursor();

                    // this check refreshes blue masks after inventory is closed, otherwise they get wiped
                    if (_invClosedCheck)
                    {
                        UpdateBlueHP();

                        _invClosedCheck = false;
                    }
                }
                else
                    _invClosedCheck = true;
            }
        }

        // -- mod initialization and scene updates -- //

        private string SavePath()
        {
            int saveProfile = GameManager.instance.profileID;
            return Path.Combine(Paths.ConfigPath, $"Bonfire-Silksong_Save{saveProfile}.json");
        }

        private void Init()
        {
            if (PlayerData.instance == null) return;

            if (LevelingSystem.Instance == null)
                gameObject.AddComponent<LevelingSystem>();

            if (BonfireGUI.Instance == null)
                gameObject.AddComponent<BonfireGUI>();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SaveSettings();
            Init();
            LevelingSystem.Instance.RefreshRelicsObtained();

            //Log.LogDebug("Current scene: " + GameManager.instance.GetSceneNameString());

            //Debug.VisitedScenes();
        }

        // -- helpers -- //

        // Override bind cost gameplay value in FSM.
        // SilkSpool_BindCost_Getter handles the patching of getter value which is used for visually matching the silk 
        // spool with current silk available.
        private void UpdateBindCostFSM()
        {
            // the "Silk Cost" variable below is responsible for many different silk functions and therefore must
            // only run if player is in active binding state.
            // Interestingly this issue arose only after testing Silk Soar: normal harpoon would work just fine
            // even if cost was set well above it's usual of 1, but soaring would always check the updated cost. 
            if (!GameManager.instance.hero_ctrl.cState.isBinding) return;

            if (HeroController.instance == null || LevelingSystem.Instance == null) return;

            int cost = LevelingSystem.Instance.BindCost(Status.IntelligenceStat);

            foreach (var fsm in HeroController.instance.GetComponents<PlayMakerFSM>())
            {
                foreach (var variable in fsm.FsmVariables.IntVariables)
                {
                    if (variable.Name == "Silk Cost" || variable.Name == "Silk Cost Witch")
                        variable.Value = cost;
                }
            }
        }

        // helper for AddBlueHealth
        private static bool BlueHpCheckInvOrMenu()
        {
            if (PlayerData.instance.isInventoryOpen || GameManager.instance.isPaused)
            {
                _blueHpAddingInProgress = false;
                return true;
            }

            return false;
        }

        // handles blue health updates for Resilience stat by clearing existing masks then applies update count after.
        // Includes multiple intermediary checks which exit the routine if either inventory or pause menu is opened. 
        // It's important to immediately call this method again if this happens: this is already done in LateUpdate 
        // callback
        //
        // Previously a much simpler logic was used: HeroController_UpdateBlueHealth patch would simply set PlayerData's
        // healthBlue equal to Resilience stat mask amount. This would work just fine except the visuals lagged a 
        // single mask behind. Thus all of the extra code logic is to fix that small but annoying issue + make sure the 
        // masks are also properly preserved after menu interactions.
        private static IEnumerator AddBlueHealth()
        {
            if (_blueHpAddingInProgress) yield break;
            _blueHpAddingInProgress = true;

            // removes existing blue hp before applying new values: closing the inventory automatically sets healthBlue 
            // = 0 while closing pause menu doesn't so it's important to do it here manually. HeroCtrl-MaxHealth fsm 
            // then removes mask visual. This way AddBlueHealth event calls below can just add the hp without worrying 
            // about current blue mask state.
            // For future updates: if there will be new ways to obtain blue masks which are added during bench visits 
            // e.g. tools similar to Hollow Knight's lifeblood charms, then healthBlue = 0 would override these. In 
            // that case, just introduce a new variable to track base game masks separately then set 
            // healthBlue = base masks and keep current SendEvent logic for modded masks.
            PlayerData.instance.healthBlue = 0;
            HeroController.instance.proxyFSM.SendEvent("HeroCtrl-MaxHealth");
            // HeroController.MaxHealth also calls this, but apparently it's not needed to reset blue mask state
            //EventRegister.SendEvent(EventRegisterEvents.HeroHealedToMax);

            yield return new WaitForSecondsRealtime(0.25f);
            if (BlueHpCheckInvOrMenu()) yield break;

            int masks = LevelingSystem.Instance.ExtraMasks(Instance.Status.ResilienceStat);

            if (masks >= 1)
            {
                for (int i = 0; i < masks; i++)
                {
                    if (CheckIfNoGameManager()) yield break;

                    if (BlueHpCheckInvOrMenu()) yield break;

                    EventRegister.SendEvent(EventRegisterEvents.AddBlueHealth);
                    yield return new WaitForSecondsRealtime(0.25f);

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

            yield return new WaitForSecondsRealtime(0.25f);

            _blueHpAddingInProgress = false;
        }

        // rosary and shell shard drop quantities from enemies
        private int GetRosaries(string size, HealthManager enemy)
        {
            FieldInfo fi = enemy.GetType().GetField(size + "GeoDrops", BindingFlags.NonPublic | BindingFlags.Instance);
            object rosaries = fi.GetValue(enemy);
            return rosaries == null ? 0 : (int)rosaries;
        }

        private int GetShellShards(HealthManager enemy)
        {
            FieldInfo fi = enemy.GetType().GetField("shellShardDrops", BindingFlags.NonPublic | BindingFlags.Instance);
            object shards = fi.GetValue(enemy);
            return shards == null ? 0 : (int)shards;
        }

        // lucky dice visual effect trigger when the player blocks a hit (HealthManager_TakeDamage)
        private void LuckyShieldSetActive()
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
        // the visual effect in HealthManager_Hit (FlashGrimmflame) only applies to player. To also display effect 
        // on the hit enemy, DamageEnemies_DoDamage requires access to following internal fields via reflection
        private static readonly FieldInfo IsHeroDamageField = typeof(DamageEnemies)
            .GetField("isHeroDamage", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo SourceIsHeroField = typeof(DamageEnemies)
            .GetField("sourceIsHero", BindingFlags.Instance | BindingFlags.NonPublic);

        private static bool IsHeroDamage(DamageEnemies instance)
        {
            bool a = (bool)(IsHeroDamageField?.GetValue(instance) ?? false);
            bool b = (bool)(SourceIsHeroField?.GetValue(instance) ?? false);
            return a || b;
        }

        // -- patched methods -- //

        // force config data saving when charms are updated
        // as leveling up and respeccing needs these, it's important to call CharmUpdate inside them in LevelingSystem
        [HarmonyPatch(typeof(HeroController), "CharmUpdate")]
        class HeroController_CharmUpdate
        {
            static void Postfix()
            {
                LevelingSystem.Instance.RefreshRelicsObtained();
                Instance.SaveSettings();
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
                    var ls = LevelingSystem.Instance;
                    var i = Instance;
                    var s = i.Status;
                    var gui = BonfireGUI.Settings;

                    if (!gui.EnemyMaxHp.ContainsKey(__instance))
                    {
                        gui.EnemyMaxHp.Add(__instance, __instance.hp);
                        gui.EnemyIsBoss.Add(__instance, BossSceneController.Instance != null);
                    }

                    Log.LogDebug("[Vanilla] rosary and shell shard values:");
                    Log.LogDebug($"small={i.GetRosaries("small", __instance)}");
                    Log.LogDebug($"medium={i.GetRosaries("medium", __instance)}");
                    Log.LogDebug($"large={i.GetRosaries("large", __instance)}");
                    Log.LogDebug($"shards={i.GetShellShards(__instance)}");

                    Log.LogDebug("[Bonfire] rosary and shell shard values:");
                    Log.LogDebug($"small={ls.IncreaseRosaries(i.GetRosaries("small", __instance), s.LuckStat)}");
                    Log.LogDebug($"medium={ls.IncreaseRosaries(i.GetRosaries("medium", __instance), s.LuckStat)}");
                    Log.LogDebug($"large={ls.IncreaseRosaries(i.GetRosaries("large", __instance), s.LuckStat)}");
                    Log.LogDebug($"shards={ls.IncreaseRosaries(i.GetShellShards(__instance), s.LuckStat)}");

                    __instance.SetGeoSmall(ls.IncreaseRosaries(i.GetRosaries("small", __instance), s.LuckStat));
                    __instance.SetGeoMedium(ls.IncreaseRosaries(i.GetRosaries("medium", __instance), s.LuckStat));
                    __instance.SetGeoLarge(ls.IncreaseRosaries(i.GetRosaries("large", __instance), s.LuckStat));

                    __instance.SetShellShards(ls.IncreaseRosaries(i.GetShellShards(__instance), s.LuckStat));
                }
            }
        }

        // update lifeblood masks when sitting down on a bench
        [HarmonyPatch(typeof(HeroController), "UpdateBlueHealth")]
        class HeroController_UpdateBlueHealth
        {
            static void Postfix(HeroController __instance)
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
                int cost = LevelingSystem.Instance.BindCost(Instance.Status.IntelligenceStat);

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

                float attackSpeed = LevelingSystem.Instance.AttackSpeed(Instance.Status.DexterityStat);

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

                float attackSpeed = LevelingSystem.Instance.AttackSpeed(Instance.Status.DexterityStat);

                typeof(HeroController)
                    .GetField("attackDuration", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(__instance, config.AttackDuration / attackSpeed);
            }
        }

        // damage blocking based on Resilience stat
        [HarmonyPatch(typeof(HeroController), "TakeDamage")]
        class HealthManager_TakeDamage
        {
            static void Prefix(HeroController __instance, GameObject go, ref int damageAmount, HazardType hazardType)
            {
                if (__instance.cState.evading || __instance.cState.invulnerable) return;

                Log.LogDebug("Took damage from hazard type: " + hazardType);

                if (hazardType != HazardType.ENEMY) return; // only block enemy attacks

                var s = Instance.Status;
                if (s.ResilienceStat <= 0) return;

                int hitsSinceShielded = Instance._hitsSinceShielded;
                if (hitsSinceShielded > 7)
                    hitsSinceShielded = 7;

                float num = Random.Range(1, 100);
                float blockChance = LevelingSystem.Instance.BlockChance(s.ResilienceStat);
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

                Log.LogDebug($"Range value {num} <= {multiplier} * {blockChance}");
                if (multiplier > 0 && num <= multiplier * blockChance)
                {
                    Instance._hitsSinceShielded = 0;
                    Instance.LuckyShieldSetActive();
                    Instance._blockNextHit = true;
                    Log.LogDebug("Blocked a hit!");
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
                if (Instance.Status.ResilienceStat <= 1) return;
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

                var s = Instance.Status;
                int extraSilk = LevelingSystem.Instance.ExtraSilk(s.WisdomStat, 1) - 1;
                if (extraSilk > 0)
                    HeroController.instance.AddSilk(extraSilk, heroEffect: false);
            }
        }

        // clean enemy hp bars, handle crit roll and passive silk/tool regen from wisdom/artifice stat respectively
        [HarmonyPatch(typeof(HeroController), "Update")]
        class HeroController_Update
        {
            static void Postfix(HeroController __instance)
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
                    Instance._critRoll = Random.Range(1, 100);

                if (HeroController.instance == null || PlayerData.instance == null)
                    return;

                try
                {
                    var i = Instance;
                    var ls = LevelingSystem.Instance;
                    float dt = Time.deltaTime;

                    // passive silk regen
                    if (i._silkRegenTime > 0)
                    {
                        i._silkRegenTime -= dt;
                    }
                    else
                    {
                        int silkRegenInterval = ls.SilkRegenInterval(Instance.Status.WisdomStat);
                        if (silkRegenInterval <= 0)
                        {
                            i._silkRegenTime = 1f;
                            return;
                        }

                        i._silkRegenTime = silkRegenInterval;
                        HeroController.instance.AddSilk(1, heroEffect: false);

                        Log.LogDebug("Regenerated 1 silk");
                    }

                    // passive tool regen: this will replenish charges for ALL red tools currently equipped
                    if (i._toolRegenTime > 0)
                    {
                        i._toolRegenTime -= dt;
                    }
                    else
                    {
                        Log.LogDebug("Checking tool regen conditions");
                        int toolRegenInterval = ls.ToolRegenInterval(Instance.Status.ArtificeStat);
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

                        Log.LogDebug("Replenished 1 charge for all red tools");
                    }
                }
                catch { }
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

                var s = Instance.Status;
                var ls = LevelingSystem.Instance;

                int critChance = ls.CritChance(s.LuckStat);
                if (critChance < 1) // must have at least 1% crit chance
                {
                    Instance._crit = false;
                    return;
                }

                int critRoll = Instance._critRoll;

                Log.LogDebug($"Crit chance: {critChance}. Rolled {critRoll}.");

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
                var s = Instance.Status;
                var ls = LevelingSystem.Instance;

                if (hitInstance.Source == null) return;

                string sourceName = hitInstance.Source.name;
                var tool = hitInstance.RepresentingTool;

                Log.LogDebug("Recorded a hit => Hit source: " + sourceName);
                Log.LogDebug("Tool instance: " + tool?.name ?? "null");

                if (tool != null && tool.Type == ToolItemType.Skill)
                {
                    // -- silk skills --
                    // the "Type == ToolItemType.Skill" should already target only the silk skills, but in case exact 
                    // names are needed for hitInstance.RepresentingTool.name, these are:
                    //   Silk Spear, Thread Sphere, Parry, Silk Charge, Silk Bomb, Silk Boss Needle
                    Log.LogDebug($"[Vanilla] Silk Skill: {tool}. Damage: {hitInstance.DamageDealt}");

                    hitInstance.DamageDealt =
                        LevelingSystem.Instance.SpellDamage(hitInstance.DamageDealt, s.IntelligenceStat);

                    Log.LogDebug($"[Bonfire] Silk Skill: {tool}. Damage: {hitInstance.DamageDealt}");
                }
                else if (tool != null && (tool.Type == ToolItemType.Red || tool.Type == ToolItemType.Blue))
                {
                    // -- red and blue tools --
                    Log.LogDebug($"[Vanilla] Tool: {tool}. Damage: {hitInstance.DamageDealt}");

                    hitInstance.DamageDealt =
                        LevelingSystem.Instance.ToolDamage(hitInstance.DamageDealt, s.ArtificeStat);

                    Log.LogDebug($"[Bonfire] Tool: {tool}. Damage: {hitInstance.DamageDealt}");
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
                    Log.LogDebug($"[Vanilla] Damage for {sourceName} = {hitInstance.DamageDealt}");

                    hitInstance.DamageDealt = ls.NeedleDamage(s.StrengthStat);

                    Log.LogDebug($"[Bonfire] Damage for {sourceName} = {hitInstance.DamageDealt}");

                    if (Instance._crit)
                    {
                        hitInstance.DamageDealt = ls.CritDamage(s.DexterityStat, hitInstance.DamageDealt);

                        Log.LogDebug($"[Crit] Damage for {sourceName} = {hitInstance.DamageDealt}");

                        HeroController.instance.GetComponent<SpriteFlash>().FlashGrimmflame();
                        Instance._crit = false;
                    }
                }
            }
        }
    }
}