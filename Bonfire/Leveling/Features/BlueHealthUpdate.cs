using System.Collections;

using GlobalEnums;
using UnityEngine;

namespace BonfireSilksong.Leveling
{
    /// <summary>
    /// Custom blue health logic e.g. to update lifeblood masks during bench visits. Usage:
    /// - Always reset BlueHpInit back to false when quitting to main menu.
    /// - Set SatDownAtBench to true after entering bench, it will be flipped to false internally.
    /// - Patch CustomUpdateBlueHealth into HeroController.UpdateBlueHealth.
    /// Additional helpers (CallDelayedpdateBlueHP, CancelBlueHPUpdate) can be also used to either delay the update
    /// e.g. during scene transitions and to cancel ongoing update loops.
    /// </summary>
    public static class BlueHealthUpdate
    {
        public static bool SatDownAtBench = false;
        public static bool BlueHpInit = false;

        /// <summary>
        /// Update blue mask count. Passing negative 'masks' value defaults to using Resilience.BlueMasks with current
        /// Resilience stat value instead.
        /// </summary>
        public static void CallUpdateBlueHP(int masks = -1)
        {
            Bonfire.Log.LogDebug("[ResilienceLogic] CallUpdateBlueHP");

            int amount;

            if (masks < 0)
            {
                amount = ResilienceStat.BlueMasks(Bonfire.Instance.Status.ResilienceStat);
            }
            else
            {
                amount = masks;
            }

            Bonfire.Instance.StartCoroutine(UpdateBlueHP(amount));
        }

        /// <summary>
        /// Delays blue up update: waits until player is detected at bench, then adds a small extra delay.
        /// Should be used after fast travel teleport finishes as it will trigger normal UpdateBlueHP call prematurely,
        /// then fix any possible visual issues with another update loop.
        /// </summary>
        public static IEnumerator CallDelayedUpdateBlueHP(int masks = -1)
        {
            yield return new WaitUntil(() => GameManager.instance.GameState == GameState.PLAYING);
            yield return new WaitForSecondsRealtime(2f);
            CallUpdateBlueHP(masks);
        }

        /// <summary>
        /// Sets internal cancel flag true which forcefully interrupts current blue hp update loop. This should be only 
        /// called by LevelingSystem's Apply and Respec methods.
        /// </summary>
        public static void CancelBlueHPUpdate()
        {
            _cancelBlueHPUpdate = true;
        }

        /// <summary>
        /// HeroController.UpdateBlueHealth (Postfix): Update lifeblood mask count.
        /// </summary>
        public static void CustomUpdateBlueHealth()
        {
            // game calls HeroController.UpdateBlueHealth twice when loading into save, this prevents the first call.
            // if future updates somehow require more than a single block, BlueHpInit should become an int counter
            // instead.
            if (!BlueHpInit)
            {
                BlueHpInit = true;
                return;
            }

            CallUpdateBlueHP();
        }


        // helper for UpdateBlueHP to interrupt ongoing update loop.
        private static bool BlueHpCheckCancelState()
        {
            // if inventory or pause menu is open, or if player has stood up and sat down during last blue hp 
            // update loop, ongoing update loop should be cancelled.
            if (PlayerData.instance.isInventoryOpen ||
                GameManager.instance.isPaused ||
                SatDownAtBench ||
                _cancelBlueHPUpdate)
            {
                _blueHpAddingInProgress = false;
                _cancelBlueHPUpdate = false;

                return true;
            }

            return false;
        }

        // handles blue health updates for Resilience stat by clearing existing masks then applies update count after.
        // Includes multiple intermediary checks which exit the routine if either inventory or pause menu is opened,
        // or if previous loop needs to forcefully cancelled after applying a level/respeccing.
        // It's important to immediately call this method again if this happens: this is already done in LateUpdate 
        // callback.
        //
        // Previously a much simpler logic was used: HeroController_UpdateBlueHealth patch would simply set PlayerData's
        // healthBlue equal to Resilience stat mask amount. This would work just fine except the visuals lagged a 
        // single mask behind. Thus all of the extra code logic is to fix that small but annoying issue + make sure the 
        // masks are also properly preserved after menu interactions. It might still break occasionally if for example
        // player spams inventory and/or pause on and off repeatedly. But for normal gameplay the current system should 
        // be very consistent.
        private static IEnumerator UpdateBlueHP(int amount)
        {
            Bonfire.Log.LogDebug("[ResilienceLogic] Blue hp refresh");
            yield return new WaitForSecondsRealtime(0.25f);

            if (_blueHpAddingInProgress) yield break;

            _blueHpAddingInProgress = true;
            _cancelBlueHPUpdate = false;

            if (BlueHpCheckCancelState())
            {
                if (SatDownAtBench)
                {
                    Bonfire.Log.LogDebug("SatDownAtBench was true, call refresh again");
                    SatDownAtBench = false;
                    Bonfire.Instance.StartCoroutine(UpdateBlueHP(amount));
                }
                yield break;
            }
            SatDownAtBench = false;

            // removes existing blue hp before applying new values: closing the inventory automatically sets healthBlue 
            // = 0 while closing pause menu doesn't so it's important to do it here manually. HeroCtrl-MaxHealth fsm 
            // then ensures that mask visuals are also removed. This way SendEvent for AddQueuedBlueHealth can just add 
            // the hp without caring about previous blue mask state.
            // For future updates: if there will be new ways to obtain blue masks which are added during bench visits 
            // e.g. tools similar to Hollow Knight's lifeblood charms, then healthBlue = 0 would override these. In 
            // that case, just introduce a new variable to track base game masks separately then set 
            // healthBlue = base masks and keep current SendEvent logic for modded masks. 
            // Even better: DLCs could make this logic less complex, possibly allowing the reuse of the original idea 
            // of Postfix patching the HeroController.UpdateBlueHealth which is exactly what Hollow Knight uses.
            // -- Updated in mod version 0.9.0: replaced AddBlueHealth with AddQueuedBlueHealth. This will immediately
            // load all masks simultaneously so using a for-loop and its multiple yield waits can now be avoided.
            PlayerData.instance.healthBlue = 0;
            HeroController.instance.proxyFSM.SendEvent("HeroCtrl-MaxHealth");
            // HeroController.MaxHealth also sends EventRegister.SendEvent(EventRegisterEvents.HeroHealedToMax), but 
            // apparently it's not needed to reset blue mask state.

            if (BlueHpCheckCancelState()) yield break;

            yield return new WaitForSecondsRealtime(0.25f);

            GameManager.instance.QueuedBlueHealth = amount;
            EventRegister.SendEvent(EventRegisterEvents.AddQueuedBlueHealth);

            if (BlueHpCheckCancelState()) yield break;

            yield return new WaitForSecondsRealtime(0.25f);

            GameManager.instance.QueuedBlueHealth = 0;

            _blueHpAddingInProgress = false;
        }


        private static bool _cancelBlueHPUpdate = false;
        private static bool _blueHpAddingInProgress = false;
    }
}