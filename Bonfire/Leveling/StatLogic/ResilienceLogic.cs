using System;
using System.Collections;
using System.Reflection;

using GlobalEnums;
using UnityEngine;

namespace BonfireSilksong
{
    public enum BlueState
    {
        THORNED,
        BLUEBLOODED,
        DISABLED
    }

    public static class ResilienceLogic
    {
        public const int ReflectBaseDamage = 5;

        public static BlueState BlueMaskMode = BlueState.DISABLED;

        public static bool SatDownAtBench = false;
        public static bool BlueHpInit = false;
        public static bool BlockNextHit = false;

        public static int ResilienceMilestone(int m)
        {
            switch (m)
            {
                case 1: return 1;
                case 2: return 2;
                case 3: return 3;
                default: return 999;
            }
        }

        public static bool CheckResilienceMilestone(int m)
        {
            var s = Bonfire.Instance.Status.ResilienceStat + LevelingSystem.MilestoneBonus;
            return s >= ResilienceMilestone(m);
        }

        /// <summary>
        /// Extra lifeblood masks.
        /// </summary>
        public static int ExtraMasks(int totalRes)
        {
            // TODO buff mask scaling, this should realistically give >15 masks with higher investments
            return (int)Math.Round(8.0 * (1.0 - Math.Exp(-0.055 * (totalRes - 1))));
        }

        /// <summary>
        /// Chance to block an enemy hit.
        /// </summary>
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
        /// Reflect damage.
        /// </summary>
        public static int ReflectDamage(int totalRes, bool isGUI = false)
        {
            int damage = 5 * ExtraMasks(totalRes);

            if (IsInBlueStateAndPlasmified(BlueState.THORNED))
            {
                damage += 10 * PlayerData.instance.health;
            }

            if (isGUI && IsInBlueStateAndPlasmified(BlueState.THORNED))
            {
                return damage += 10 * PlayerData.instance.health;
            }

            Bonfire.Log.LogDebug($"[ResilienceLogic] Challenge taunt final damage: {damage}");

            return damage;
        }

        public static float ReflectArea()
        {
            BlueState thorned = BlueState.THORNED;
            float range = ReflectDamageRange;

            if (IsInBlueState(thorned) && CheckResilienceMilestone(2))
                range += 1f;

            if (IsInBlueStateAndPlasmified(thorned))
                range += 2f;

            return range;
        }

        public static int GetBlueBloodedMilestoneBonus(int totalRes)
        {
            if (totalRes < 0) return 0;

            BlueState s = BlueState.BLUEBLOODED;

            if (IsInBlueStateAndPlasmified(s))
            {
                return 10;
            }
            else if (IsInBlueState(s) && CheckResilienceMilestone(2))
            {
                return 5;
            }
            else if (IsInBlueState(s))
            {
                return 3;
            }
            else
            {
                return 0;
            }
        }

        public static bool IsInBlueState(BlueState state)
        {
            return CheckResilienceMilestone(1) && BlueMaskMode == state;
        }

        public static bool IsInBlueStateAndPlasmified(BlueState state)
        {
            return HeroController.instance.IsInLifebloodState && CheckResilienceMilestone(3) && BlueMaskMode == state;
        }

        /// <summary>
        /// Update blue mask count from Resilience stat.
        /// </summary>
        public static void CallUpdateBlueHP()
        {
            Bonfire.Log.LogDebug("[ResilienceLogic] CallUpdateBlueHP");
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

            Bonfire.Log.LogInfo("[ResilienceLogic] Took damage from hazard type: " + hazardType);

            // handle reflect damage here after iframe checks. 
            // Also HazardType arg should be kept in case this code needs to be called elsewhere.
            // Finally give 1 silk.
            DealReflectDamage(hazardType);
            if (IsInBlueState(BlueState.THORNED))
                HeroController.instance.AddSilk(1, false);

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
                BlockNextHit = true;
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
            if (BlockNextHit)
            {
                BlockNextHit = false;
                return 0;
            }

            return amount;
        }

        public static void DealReflectDamage(HazardType hazardType)
        {
            if (hazardType != HazardType.ENEMY) return;

            Bonfire.Log.LogInfo("[ResilienceLogic] Reflecting damage to enemies.");

            if (!IsInBlueState(BlueState.THORNED))
                return;

            var hc = HeroController.instance;

            float range = ReflectArea(); // for now keep this fixed, might add range increases to later milestones

            foreach (var e in EnemyHealth.EnemyMaxHp.Keys)
            {
                if (e == null)
                    continue;

                float dist = Vector2.Distance(hc.transform.position, e.transform.position);
                if (dist > range)
                    continue;

                HealthManager hm = e.GetComponent<HealthManager>();

                if (hm == null)
                    continue;

                HitInstance hit = CreateReflectHitInstance(hc);

                hm.Hit(hit);
            }
        }

        /// <summary>
        /// Sets internal cancel flag true which forcefully interrupts current blue hp update loop. This should be only 
        /// called by LevelingSystem's Apply and Respec methods.
        /// </summary>
        public static void CancelBlueHPUpdate()
        {
            _cancelBlueHPUpdate = true;
        }


        private const float ReflectDamageRange = 3f;

        private static bool _cancelBlueHPUpdate = false;
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
        private static IEnumerator UpdateBlueHP()
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
                    Bonfire.Instance.StartCoroutine(UpdateBlueHP());
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

            int masks = ExtraMasks(Bonfire.Instance.Status.ResilienceStat);
            GameManager.instance.QueuedBlueHealth = masks;
            EventRegister.SendEvent(EventRegisterEvents.AddQueuedBlueHealth);

            if (BlueHpCheckCancelState()) yield break;

            yield return new WaitForSecondsRealtime(0.25f);

            GameManager.instance.QueuedBlueHealth = 0;

            _blueHpAddingInProgress = false;
        }

        private static HitInstance CreateReflectHitInstance(HeroController instance)
        {
            instance.gameObject.name = "Bonfire-Reflect";

            return new HitInstance
            {
                DamageDealt = ReflectBaseDamage,
                Source = instance.gameObject,
                AttackType = AttackTypes.Generic,
                IsFirstHit = false,
                HitEffectsType = EnemyHitEffectsProfile.EffectsTypes.Full,
                Direction = 0f,
                MagnitudeMultiplier = 1f,
                Multiplier = 1f,
                IsHeroDamage = true,
                SilkGeneration = HitSilkGeneration.Full
            };
        }
    }
}