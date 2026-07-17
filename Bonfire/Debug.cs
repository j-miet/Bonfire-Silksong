using System.Reflection;

using BepInEx.Logging;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace BonfireSilksong
{

    /// <summary>
    /// Reusable debug logging methods. Anything that's potentially useful in the future is kept here e.g. if/when
    /// future DLCs change stuff.
    /// </summary>
    public static class Debug
    {
        /// <summary>
        /// Log "Chapel of the Beast" entry gate state.
        /// </summary>
        public static void BeastChapelDoorState()
        {
            Log.LogDebug("== Beast chapel door state ==");
            var obj = GameObject.Find("Chapel Door Control");

            Log.LogDebug(obj == null ? "Door not found" : $"Found {obj.name}");

            var fsm = obj?.GetComponent<PlayMakerFSM>();

            Log.LogDebug(fsm == null ? "No FSM" : $"FSM: {fsm.FsmName}");
            Log.LogDebug($"FSM: {fsm.FsmName}");

            foreach (var state in fsm.FsmStates)
            {
                Log.LogDebug($"State: {state.Name}");

                foreach (var action in state.Actions)
                {
                    Log.LogDebug($"  - Action: {action.GetType().FullName}");
                    if (action is PlayerDataBoolTest test)
                    {
                        Log.LogDebug($"Testing {test.boolName.Value}");
                        Log.LogDebug($"True -> {test.isTrue?.Name}");
                        Log.LogDebug($"False -> {test.isFalse?.Name}");
                    }
                }

                foreach (var transition in state.Transitions)
                {
                    Log.LogDebug($"  - Event: {transition.EventName} => {transition.ToState}");
                }
            }

            Log.LogDebug("Beast chapel beastfly defeated: " + PlayerData.instance.defeatedBoneFlyerGiant);
            Log.LogDebug("Beast chapel door closed: " + PlayerData.instance.chapelClosed_beast);
        }

        /// <summary>
        /// Log all "Bench Control" states.
        /// Must patch HeroController.Respawn(Transform spawnPoint) and use its Transform arg here.
        /// </summary>
        public static void BenchFSM(Transform spawnPoint)
        {
            if (spawnPoint == null) return;

            var benchFSM = FSMUtility.LocateFSM(spawnPoint.gameObject, "Bench Control");
            if (benchFSM == null)
            {
                Bonfire.Log.LogDebug("No Bench Control FSM found on spawnPoint");
                return;
            }

            Bonfire.Log.LogDebug("== 'Bench Control' FSM States ==");
            foreach (var state in benchFSM.FsmStates)
            {
                Bonfire.Log.LogDebug($"State: {state.Name}");
                foreach (var transition in state.Transitions)
                    Bonfire.Log.LogDebug($"  - Event: {transition.EventName} => {transition.ToState}");
            }
        }

        /// <summary>
        /// Log blue mask related data.
        /// </summary>
        public static void BlueHpVariableFSM()
        {
            var gameCameras = GameCameras.instance.gameObject;
            foreach (var fsm in gameCameras.GetComponentsInChildren<PlayMakerFSM>(includeInactive: true))
            {
                if (fsm.FsmName != "Blue Health Control") continue;

                Log.LogDebug("== Blue Health variables ==");
                foreach (var v in fsm.FsmVariables.IntVariables)
                    Log.LogDebug($"  int: {v.Name} = {v.Value}");
                foreach (var v in fsm.FsmVariables.FloatVariables)
                    Log.LogDebug($"  float: {v.Name} = {v.Value}");
                foreach (var v in fsm.FsmVariables.BoolVariables)
                    Log.LogDebug($"  bool: {v.Name} = {v.Value}");
            }
        }

        /// <summary>
        /// Logs all global FSM events.
        /// </summary>
        public static void AllGlobalFSMEvents()
        {
            Log.LogDebug("== FSM events ==");
            foreach (var e in FsmEvent.globalEvents)
                Log.LogDebug(e);
        }

        /// <summary>
        /// Log all collectable items and tools.
        /// </summary>
        public static void CollectablesAndTools()
        {
            Log.LogDebug("== Collectables and tools ==");
            Log.LogDebug("-- Collectable --");
            foreach (var item in Resources.FindObjectsOfTypeAll<CollectableItem>())
                Log.LogDebug(item.name);

            Log.LogDebug("-- Tools --");
            foreach (var tool in Resources.FindObjectsOfTypeAll<ToolItem>())
                Log.LogDebug($"{tool.name} ({tool.GetType().FullName})");
        }

        /// <summary>
        /// Log enemy drops.
        /// </summary>
        public static void EnemyDrops()
        {
            var enemies = Resources.FindObjectsOfTypeAll<HealthManager>();
            if (enemies.Length > 0)
            {
                Log.LogDebug("== Enemy drops ==");
                foreach (var field in enemies[0].GetType().GetFields(
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                {
                    if (field.Name.ToLower().Contains("geo")
                        || field.Name.ToLower().Contains("rosary")
                        || field.Name.ToLower().Contains("drop"))
                        Log.LogDebug($"{field.Name}: {field.GetValue(enemies[0])}");
                }
            }
        }

        /// <summary>
        /// Log PlayMaker FSM variables, specifically int, float and bool types.
        /// </summary>
        public static void FSMVariables()
        {
            Log.LogDebug("== FSM variables ==");
            foreach (var fsm in HeroController.instance.GetComponents<PlayMakerFSM>())
            {
                Log.LogDebug($"FSM: {fsm.FsmName}");
                foreach (var variable in fsm.FsmVariables.FloatVariables)
                    Log.LogDebug($"  float: {variable.Name} = {variable.Value}");
                foreach (var variable in fsm.FsmVariables.IntVariables)
                    Log.LogDebug($"  int: {variable.Name} = {variable.Value}");
                foreach (var variable in fsm.FsmVariables.BoolVariables)
                    Log.LogDebug($"  bool: {variable.Name} = {variable.Value}");
            }
        }

        /// <summary>
        /// Log attack speed values.
        /// </summary>
        public static void AttackSpeed()
        {
            Log.LogDebug("== Attack speed data ==");

            var config = HeroController.instance?.Config;
            if (config == null) return;

            Log.LogDebug("Attack duration: " + config.AttackDuration);
            Log.LogDebug("Attack cooldown time: " + config.AttackCooldownTime);
            Log.LogDebug("Quick attack speed mult: " + config.QuickAttackSpeedMult);
            Log.LogDebug("Quick attack cooldown time: " + config.QuickAttackCooldownTime);
        }

        /// <summary>
        /// Log all names of visited scenes in save file.
        /// </summary>
        public static void VisitedScenes()
        {
            Log.LogDebug("== Visited scenes ==");
            foreach (var s in PlayerData.instance.scenesVisited)
                Log.LogDebug("Scene: " + s);
        }


        private static readonly ManualLogSource Log = Bonfire.Log;
    }
}