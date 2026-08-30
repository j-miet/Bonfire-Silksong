using System.Collections.Generic;
using System.Linq;
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
            Log.LogDebug("[Debug] Beast chapel door state");
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
                    Log.LogDebug($" - Action: {action.GetType().FullName}");
                    if (action is PlayerDataBoolTest test)
                    {
                        Log.LogDebug($"boolName {test.boolName.Value}");
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
            Log.LogDebug("[Debug] Bench Control");
            if (spawnPoint == null) return;

            var benchFSM = FSMUtility.LocateFSM(spawnPoint.gameObject, "Bench Control");
            if (benchFSM == null)
            {
                Log.LogDebug("No Bench Control FSM found on spawnPoint");
                return;
            }

            Log.LogDebug("'Bench Control' FSM States");
            foreach (var state in benchFSM.FsmStates)
            {
                Log.LogDebug($"- State: {state.Name}");
                foreach (var transition in state.Transitions)
                    Log.LogDebug($"  - Event: {transition.EventName} => {transition.ToState}");
            }
        }

        /// <summary>
        /// Log blue mask related data.
        /// </summary>
        public static void BlueHpVariableFSM()
        {
            Log.LogDebug("[Debug] Blue Health variables");
            var gameCameras = GameCameras.instance.gameObject;
            foreach (var fsm in gameCameras.GetComponentsInChildren<PlayMakerFSM>(includeInactive: true))
            {
                if (fsm.FsmName != "Blue Health Control") continue;

                Log.LogDebug("Variables:");
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
            Log.LogDebug("[Debug ] FSM global events");
            foreach (var e in FsmEvent.globalEvents)
                Log.LogDebug(e);
        }

        /// <summary>
        /// Log all collectable items and tools.
        /// </summary>
        public static void CollectablesAndTools()
        {
            Log.LogDebug("[Debug] Collectables and tools");
            Log.LogDebug("-- CollectableItem --");
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
                Log.LogDebug("[Debug] Enemy drops");
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
            Log.LogDebug("[Debug] HeroController FSM variables");
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
        /// Log all names of visited scenes in save file.
        /// </summary>
        public static void VisitedScenes()
        {
            Log.LogDebug("[Debug] Visited scenes");
            foreach (var s in PlayerData.instance.scenesVisited)
                Log.LogDebug("Scene: " + s);
        }

        /// <summary>
        /// Iterates over a Transform objects children recursively until no more Transform are found.
        /// </summary>
        /// <param name="root">Root transform</param>
        /// <param name="indent">Custom indentation</param>
        public static void DumpHierarchy(Transform root, string indent = "")
        {
            if (root == null)
                return;

            Component[] components = root.GetComponents<Component>();

            string componentNames = string.Join(
                ", ",
                components
                    .Where(c => c != null)
                    .Select(c => c.GetType().Name)
            );

            Log.LogDebug(
                $"{indent}{root.name} | " +
                $"scale={root.localScale} | " +
                $"pos={root.localPosition} | " +
                $"components=[{componentNames}]"
            );

            for (int i = 0; i < root.childCount; i++)
            {
                DumpHierarchy(root.GetChild(i), indent + "  ");
            }
        }

        /// <summary>
        /// Get all Fsm tied to player character, their FsmStates and finally type names of actions from each FSMState.
        /// </summary>
        public static void GetHornetAllFsmStateAction()
        {
            var hc = HeroController.instance;

            foreach (var fsm in hc.GetComponents<PlayMakerFSM>())
            {
                Bonfire.Log.LogInfo($"### {fsm.FsmName}");
                if (fsm == null) continue;

                foreach (var s in fsm.FsmStates)
                {
                    Bonfire.Log.LogInfo($" == {s.Name} ==");
                    if (s == null) continue;

                    foreach (var a in s.Actions)
                    {
                        Bonfire.Log.LogInfo($"  - {a.GetType().Name}");
                    }
                }
            }
        }

        public static void GetAllScenes()
        {
            Bonfire.Log.LogInfo("== All Scenes ==");
            foreach (var s in Resources.FindObjectsOfTypeAll<GameMapScene>())
                Bonfire.Log.LogInfo(s.Name);
        }

        /// <summary>
        /// Get all scene names with benches and the type of bench marker. However this also gives invalid benches
        /// that can be teleported at, but all there is a black screen.
        /// </summary>
        public static void GetAllBenchScenes()
        {
            Dictionary<string, SceneTeleportMap.SceneInfo> teleportMap = SceneTeleportMap.GetTeleportMap();
            if (teleportMap == null)
            {
                Bonfire.Log.LogError("SceneTeleportMap is null!");
                return;
            }

            SceneTeleportMap.SceneInfo info;

            Bonfire.Log.LogInfo("== Bench Scenes ==");
            foreach (var pair in teleportMap)
            {
                info = pair.Value;
                if (info == null)
                    continue;

                var respawnMarkers = info.RespawnPoints
                    .FindAll(p => p.Contains("Bench"));

                foreach (var marker in respawnMarkers)
                {
                    if (!string.IsNullOrEmpty(marker) &&
                        info.TransitionGates.Any(t =>
                            t.StartsWith("left") ||
                            t.StartsWith("right") ||
                            t.StartsWith("top") ||
                            t.StartsWith("bot")))
                    {
                        Bonfire.Log.LogInfo($"{pair.Key} | {marker}");
                    }
                }
            }
        }


        private static readonly ManualLogSource Log = Bonfire.Log;
    }
}