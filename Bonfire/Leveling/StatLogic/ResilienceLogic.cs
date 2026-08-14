using System;
using System.Collections;
using System.Reflection;

using GlobalEnums;
using UnityEngine;

namespace BonfireSilksong
{
    public static class ResilienceLogic
    {
        public static bool SatDownAtBench = false;
        public static bool BlueHpInit = false;

        public static int ResilienceMilestone_1 => 1;
        public static int ResilienceMilestone_2 => 2;
        public static int ResilienceMilestone_3 => 3;
        public static int ResilienceMilestone_4 => 4;
        public static bool CheckResilienceMilestone_1 =>
            Bonfire.Instance.Status.ResilienceStat >= ResilienceMilestone_1;
        public static bool CheckResilienceMilestone_2 =>
            Bonfire.Instance.Status.ResilienceStat >= ResilienceMilestone_2;
        public static bool CheckResilienceMilestone_3 =>
            Bonfire.Instance.Status.ResilienceStat >= ResilienceMilestone_3;
        public static bool CheckResilienceMilestone_4 =>
            Bonfire.Instance.Status.ResilienceStat >= ResilienceMilestone_4;

        // Stat scaling: extra lifeblood masks. Caps at 8 on level 40; using a single plasmium from phial will then
        // activate overdose regen effect
        public static int ExtraMasks(int totalRes)
        {
            if (totalRes >= 40) return 8;

            return (int)Math.Round(8.0 * (1.0 - Math.Exp(-0.055 * (totalRes - 1))));
        }

        // Stat scaling: chance to block an enemy hit
        public static float BlockChance(int totalRes)
        {
            return (float)(0.72 / (1.0 + 12.0 * Math.Exp(-0.09 * totalRes)));
        }

        /// <summary>
        /// LevelingGUI helper: Returns average damage blocking chance from current Resilience stat value.
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

        /// <summary>
        /// Update blue mask count from Resilience stat.
        /// </summary>
        public static void CallUpdateBlueHP()
        {
            Bonfire.Log.LogDebug("[ResilienceLogic] Called");
            Bonfire.Instance.StartCoroutine(UpdateBlueHP());
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
            CallUpdateBlueHP();
        }

        /// <summary>
        /// HeroController.UpdateBlueHealth (Postfix): Update lifeblood mask count.
        /// </summary>
        public static void CustomUpdateBlueHealth()
        {
            if (!BlueHpInit)
            {
                BlueHpInit = true;
                return;
            }

            CallUpdateBlueHP();
        }

        /// <summary>
        /// HeroController.TakeDamage (Prefix): Check if damage should get blocked via Resilience block functionality 
        /// when an enemy hits the player, and if so, set StatsShared.BlockNextHit = true.
        /// </summary>
        public static void HandleBlockCheck(HeroController instance, HazardType hazardType)
        {
            if (instance.cState.evading || instance.cState.invulnerable) return;

            Bonfire.Log.LogDebug("[ResilienceLogic] Took damage from hazard type: " + hazardType);

            if (hazardType != HazardType.ENEMY) return; // only block enemy attacks

            var s = Bonfire.Instance.Status;
            if (s.ResilienceStat <= 0) return;

            int hitsSinceShielded = _hitsSinceShielded;
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
                _hitsSinceShielded = 0;
                LuckyShieldSetActive();
                StatsShared.BlockNextHit = true;
                Bonfire.Log.LogDebug("Blocked a hit!");
            }
            else
            {
                _hitsSinceShielded++;
            }
        }

        /// <summary>
        /// PlayerData.TakeHealth (Prefix): Prevent damage taken by setting incoming damage equal to 0 during 
        /// block state.
        /// </summary>
        public static int PreventHPLossOnBlock(int amount)
        {
            if (Bonfire.Instance.Status.ResilienceStat <= 0) return amount;
            if (StatsShared.BlockNextHit)
            {
                StatsShared.BlockNextHit = false;
                return 0;
            }

            return amount;
        }

        private static bool _blueHpAddingInProgress = false;
        private static int _hitsSinceShielded = 0;
        private readonly static FieldInfo SpawnedLuckyDiceShieldEffect = typeof(HeroController)
            .GetField("spawnedLuckyDiceShieldEffect", BindingFlags.Instance | BindingFlags.NonPublic);

        // helper for HandleBlockCheck to display same effect what Magnetite Dice tool uses on blocked hits.
        private static void LuckyShieldSetActive()
        {
            var hc = HeroController.instance;
            var effect = (GameObject)SpawnedLuckyDiceShieldEffect?.GetValue(hc);

            if (effect != null)
            {
                effect.SetActive(false);
                effect.SetActive(true);
            }
        }

        // helper for RefreshResilienceBlueHealth to interrupt ongoing
        private static bool BlueHpCheckCancelState()
        {
            // if inventory or pause menu is open, or if player has stood up and sat down during last blue hp 
            // update loop, ongoing update loop should be cancelled.
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
        private static IEnumerator UpdateBlueHP()
        {
            Bonfire.Log.LogDebug("[UpdateBlueHP] blue hp refresh");
            yield return new WaitForSecondsRealtime(0.25f);

            if (_blueHpAddingInProgress) yield break;
            _blueHpAddingInProgress = true;

            if (BlueHpCheckCancelState())
            {
                if (SatDownAtBench)
                {
                    Bonfire.Log.LogDebug("SatDownAtBench was true, call refresh again");
                    SatDownAtBench = false;
                    Bonfire.Instance.StartCoroutine(UpdateBlueHP());
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

            if (BlueHpCheckCancelState()) yield break;

            yield return new WaitForSecondsRealtime(0.25f);

            int masks = ExtraMasks(Bonfire.Instance.Status.ResilienceStat);

            if (masks >= 1)
            {
                for (int i = 0; i < masks; i++)
                {
                    if (BlueHpCheckCancelState()) yield break;

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

            if (BlueHpCheckCancelState()) yield break;

            _blueHpAddingInProgress = false;
        }
    }
}