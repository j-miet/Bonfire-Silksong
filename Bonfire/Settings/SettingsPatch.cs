using GlobalSettings;

using HutongGames.PlayMaker.Actions;

namespace BonfireSilksong.Settings
{
    /// <summary>
    /// Settings logic which needs to be injected into game code.
    /// </summary>
    public static class SettingsPatch
    {
        /// <summary>
        /// Helper for registering crest chapel scene visits.
        /// </summary>
        public static void RegisterCrestChapelScenes(string scene)
        {
            if (scene == null) return;

            var s = Bonfire.Instance.Status;

            Bonfire.Log.LogDebug($"[SettingsPatch] Checking if scene '{scene}' is a crest chapel scene.");

            switch (scene)
            {
                case "Chapel_Wanderer":
                    s.VisitedWandererChapel = true;
                    return;
                case "Greymoor_20c":
                    s.VisitedReaperChapel = true;
                    return;
                case "Under_20":
                    s.VisitedArchitectChapel = true;
                    return;
            }
        }

        /// <summary>
        /// SprintRaceController.GetRaceInfo (Postfix): reduce SprintMaster Swift's run speed by 50%.
        /// </summary>
        public static float MakeSprintmasterRaceEasier(float currentBaseSpeed)
        {
            if (!Bonfire.Instance.Status.EasierSprintmasterRaces) return currentBaseSpeed;

            float updatedSpeed = currentBaseSpeed *= 0.5f;
            return updatedSpeed;
        }

        /// <summary>
        /// PlayerDataBoolTest.OnEnter (Prefix): Check if Chapel doors for crests should be open. Used with following
        /// crests: Wanderer, Beast, Reaper. Architect uses its own CheckIfArchitectCrestDoorOpen method, other crests 
        /// don't require this at all.
        /// </summary>
        public static bool CheckIfCrestChapelDoorOpen(PlayerDataBoolTest instance)
        {
            // each crest uses simple check: if player has unlocked the corresponding crest using manual unlock feature,
            // the crest door would normally become closed. However the custom status "Visited*Chapel" check allows
            // them to visit location one time. After entering the chapel scene, visited flag is set as true and leaving
            // the chapel will immediately follow the normal checks, closing the door.
            // Beast crest uses slightly different logic: it will actually check if player has killed Savage Beastfly
            // instead of using the one-time flag. Reason is that Savage Beastfly must be defeated in order to get the
            // second encounter and its mask shard reward. So one-time check would permanently prevent 100% completion
            // on current save file. Other chapels don't have anything that affects game completion percentage, only
            // stuff like rosaries. So for those its not as critical if player accidentally enters then immediately
            // leaves.
            if (instance?.boolName == null)
                return false;

            var b = instance.boolName.Value;
            if (!b.StartsWith("chapel"))
                return false;

            Bonfire.Log.LogDebug($"[SettingsPatch] Crest Chapel Door check for bool {b}");

            var s = Bonfire.Instance.Status;
            bool keepOpen = false;
            string currentScene = GameManager.instance.GetSceneNameString();

            Bonfire.Log.LogDebug($"Scene: {currentScene}");

            if (!s.VisitedWandererChapel &&
                b == "chapelClosed_wanderer" &&
                Gameplay.WandererCrest.IsUnlocked &&
                currentScene == "Bonegrave")
            {
                keepOpen = true;
            }

            if (b == "chapelClosed_beast" &&
                Gameplay.WarriorCrest.IsUnlocked &&
                !PlayerData.instance.defeatedBoneFlyerGiant)
            {
                keepOpen = true;
            }

            if (!s.VisitedReaperChapel &&
                b == "chapelClosed_reaper" &&
                Gameplay.ReaperCrest.IsUnlocked &&
                currentScene == "Greymoor_20b")
            {
                keepOpen = true;
            }

            if (keepOpen)
            {
                // FSM event to keep the chapel gate open. It has other values: "CLOSE" which means its closed
                // when scene loads and "DO CLOSE" which triggers the door closing animation
                instance.Fsm.Event("OPEN");
                instance.Finish();

                return true;
            }

            return false;
        }

        /// <summary>
        /// GetIsCrestUnlocked.IsTrue (Postfix): Check if door for Chapel of the Architect should remain open.
        /// </summary>
        public static bool CheckIfArchitectChapelDoorOpen(GetIsCrestUnlocked instance)
        {
            if (instance.Crest == null)
                return false;

            var c = instance.Crest.Value as ToolCrest;
            if (c == null)
                return false;

            Bonfire.Log.LogDebug("[SettingsPatch] Crest Name: " + c.name);

            if (!Bonfire.Instance.Status.VisitedArchitectChapel &&
                Gameplay.ToolmasterCrest.IsUnlocked &&
                c.name == "Toolmaster")
            {
                string currentScene = GameManager.instance.GetSceneNameString();

                if (currentScene == "Under_17")
                {
                    // FSM event to check if crest in unlocked which is then used for door state. Uses boolean value: 
                    // "TRUE" if crest is unlocked & door closed, and "FALSE" for opposite.
                    instance.Fsm.Event("FALSE");
                    return true;
                }
                else if (currentScene == "Under_20")
                {
                    Bonfire.Instance.Status.VisitedArchitectChapel = true;
                }
            }

            return false;
        }

        /// <summary>
        /// DeliveryQuestItem.TakeHitForItem (Prefix):
        /// Remove damage penalties from courier delivery quests + prevent teleporting with active delivery items.
        /// </summary>
        public static bool DeliveryQuestHitCheck()
        {
            // this will ensure bench teleporting breaks all delivery quest items after it calls TakeHit.
            if (PlayerData.instance.atBench) return true;

            if (!Bonfire.Instance.Status.DeliveryQuestsRemoveDamage) return true;

            return false;
        }

        /// <summary>
        /// HeroController.TickDeliveryItems (Prefix): Remove timers from courier delivery quests.
        /// </summary>
        public static bool DeliveryQuestTimerCheck()
        {
            if (!Bonfire.Instance.Status.DeliveryQuestsRemoveTimer) return true;

            return false;
        }
    }
}