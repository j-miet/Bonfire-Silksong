using System;
using System.Collections;
using System.Reflection;

using GlobalEnums;
using UnityEngine;

namespace BonfireSilksong
{
    public enum BlueState
    {
        DISABLED,
        REFLECT,
        GLASSCANNON,
    }

    public static class ResilienceLogic
    {
        public const int ReflectBaseDamage = 5;

        public static bool SatDownAtBench = false;
        public static bool BlueHpInit = false;
        public static bool BlockNextHit = false;
        public static bool UsedPlasmiumPhial = false;

        public static bool GlassCannonButNotPlasmified =>
            Bonfire.Instance.Status.BlueMaskMode == (int)BlueState.GLASSCANNON &&
            !IsInBlueStateAndPlasmified(BlueState.GLASSCANNON);

        public static int ResilienceMilestone(int m)
        {
            switch (m)
            {
                case 1: return 1;
                case 2: return 2;
                case 3: return 35;
                default: return 999;
            }
        }

        public static bool CheckResilienceMilestone(int m)
        {
            var s = Bonfire.Instance.Status.ResilienceStat;
            int b = LevelingSystem.MilestoneBonus;
            return s + b >= ResilienceMilestone(m) && s >= 1;
        }

        /// <summary>
        /// Extra lifeblood masks. Capped at 8 because at 9 the game transitions into Plasmified state.
        /// </summary>
        public static int BlueMasks(int totalRes)
        {
            if (totalRes <= 0) return 0;
            else if (totalRes < 4) return 1;
            else if (totalRes < 9) return 2;
            else if (totalRes < 15) return 3;
            else if (totalRes < 20) return 4;
            else if (totalRes < 25) return 5;
            else if (totalRes < 30) return 6;
            else if (totalRes < 35) return 7;
            else return 8;
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
        public static int ReflectDamage(int totalRes, bool isGUI = false, bool GUIPlasmified = false)
        {
            BlueState state = BlueState.REFLECT;
            if (totalRes <= 0 || !IsInBlueState(state))
                return 0;

            int damage = 5 + 3 * BlueMasks(totalRes) + 5 * (PlayerData.instance.maxHealthBase - 5);

            if (IsInBlueStateAndPlasmified(state) || (isGUI && GUIPlasmified))
            {
                damage += 10 * PlayerData.instance.health;
            }

            // skip log printing when GUI calls this
            if (!isGUI)
                Bonfire.Log.LogDebug($"[ResilienceLogic] Reflect damage: {damage}");

            return damage;
        }

        /// <summary>
        /// Reflect effect area.
        /// </summary>
        public static float ReflectArea(bool isGUI = false, bool GUIPlasmified = false)
        {
            BlueState state = BlueState.REFLECT;
            if (!IsInBlueState(state))
                return 0;

            float range = ReflectDamageArea;

            if (IsInBlueState(state) && CheckResilienceMilestone(2))
                range += 1f;

            if (IsInBlueStateAndPlasmified(state) || (isGUI && GUIPlasmified))
                range += 3f;

            if (!isGUI)
                Bonfire.Log.LogDebug($"[ResilienceLogic] Reflect effect area: {range}");

            return range;
        }

        /// <summary>
        /// Handle reflect damage output while in reflect blue state.
        /// </summary>
        public static void HandleReflectDamageHitLogic(HazardType hazard)
        {
            if (IsInBlueState(BlueState.REFLECT))
            {
                DealReflectDamage();

                if (Bonfire.Instance.Status.ShowCustomRangeIndicator)
                    RangeIndicator.Display(ReflectArea(isGUI: true));

                if (hazard == HazardType.ENEMY)
                    HeroController.instance.AddSilk(1, false);
            }
        }

        /// <summary>
        /// Amount of blue hp healed with bind while in damage reflect state.
        /// </summary>
        public static bool ReflectStateHealedBlueMasks()
        {
            if (IsInBlueState(BlueState.REFLECT) && !HeroController.instance.IsInLifebloodState)
            {
                int masks = 1;

                if (CheckResilienceMilestone(2))
                    masks += 1;


                int updated = PlayerData.instance.healthBlue + masks;
                int res = BlueMasks(Bonfire.Instance.Status.ResilienceStat);

                if (updated > res)
                    masks -= updated - res;

                Bonfire.Log.LogDebug("[ResilienceLogic] Blue hp healed in Reflect state: " + masks);

                GameManager.instance.QueuedBlueHealth = masks;
                EventRegister.SendEvent(EventRegisterEvents.AddQueuedBlueHealth);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Apply damage bonuses while in glass cannon state.
        /// </summary>
        public static int ApplyGlassCannonDamageBonus(int baseDamage)
        {
            int damage = (int)Math.Round(baseDamage * 1.5f * GlassCannonDamageMaskBonus());

            if (CheckResilienceMilestone(2) || IsInBlueState(BlueState.GLASSCANNON))
            {
                damage += 10;
                Bonfire.Log.LogDebug($"[ResilienceLogic] Applied glass cannon flat bonus");
            }

            return damage;
        }

        /// <summary>
        /// Get hp + blue mask damage bonuses in glass cannon state (separated from ApplyGlassCannonDamageBonus for 
        /// GUI utilization).
        /// </summary>
        public static float GlassCannonDamageMaskBonus(bool isGUI = false, bool GUIPlasmified = false)
        {
            BlueState state = BlueState.GLASSCANNON;
            if (!IsInBlueState(state))
                return 0;

            float bonus = 1f;

            if (IsInBlueStateAndPlasmified(state) || (isGUI && GUIPlasmified))
            {
                bonus += PlayerData.instance.health * 0.2f + (PlayerData.instance.maxHealthBase - 5) * 0.08f;
            }
            else
            {
                bonus += PlayerData.instance.healthBlue * 0.07f + (PlayerData.instance.maxHealthBase - 5) * 0.08f;
            }

            if (!isGUI)
                Bonfire.Log.LogDebug($"[ResilienceLogic] Glass cannon mask bonus: {bonus}");

            return bonus;
        }

        /// <summary>
        /// Get current milestone reduction bonus when in glass cannon state.
        /// </summary>
        public static int GetGlassCannonMilestoneBonus(int totalRes, bool isGUI = false, bool GUIPlasmified = false)
        {
            BlueState s = BlueState.GLASSCANNON;

            if (totalRes <= 0 || !IsInBlueState(s)) return 0;

            int m;

            if (IsInBlueStateAndPlasmified(s) || (isGUI && GUIPlasmified))
            {
                m = 10;
            }
            else if (IsInBlueState(s) && CheckResilienceMilestone(2))
            {
                m = 5;
            }
            else if (IsInBlueState(s))
            {
                m = 3;
            }
            else
            {
                m = 0;
            }

            if (!isGUI)
                Bonfire.Log.LogDebug($"[ResilienceLogic] Glass cannon milestone bonus: {m}");

            return m;
        }

        public static bool IsInBlueState(BlueState state)
        {
            return CheckResilienceMilestone(1) && Bonfire.Instance.Status.BlueMaskMode == (int)state;
        }

        public static bool IsInAnyBlueState()
        {
            return IsInBlueState(BlueState.REFLECT) || IsInBlueState(BlueState.GLASSCANNON);
        }

        public static bool IsInBlueStateAndPlasmified(BlueState state)
        {
            return HeroController.instance.IsInLifebloodState &&
                CheckResilienceMilestone(3) &&
                Bonfire.Instance.Status.BlueMaskMode == (int)state;
        }

        /// <summary>
        /// Update damage taken based on current blue state.
        /// </summary>
        public static int UpdateBlueStateDamageTaken(int amount)
        {
            int damageTaken = amount;

            if (IsInBlueStateAndPlasmified(BlueState.GLASSCANNON))
            {
                damageTaken += 2;
            }
            else if (IsInBlueStateAndPlasmified(BlueState.REFLECT))
            {
                // revert the -1 damage taken below: IsInBlueStateAndPlasmified must be checked first before 
                // IsInBlueState as the latter will always get accepted if the former is true. 
                // However while Plasmified, healthBlue should technically equal to 0 because game tracks normal hp
                // instead so this might not be necessary, but it can stay here as a safety check.
                damageTaken = amount;
            }
            else if (IsInAnyBlueState() && PlayerData.instance.healthBlue <= 0)
            {
                // if player has no blue masks left while in blue state, damage taken is 10x.
                damageTaken *= 10;
            }
            else if (IsInAnyBlueState())
            {
                damageTaken = Math.Max(1, amount - 1);
            }

            Bonfire.Log.LogDebug($"[ResilienceLogic] Damage taken in blue state: {damageTaken}");

            return damageTaken;
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
        /// Sets internal cancel flag true which forcefully interrupts current blue hp update loop. This should be only 
        /// called by LevelingSystem's Apply and Respec methods.
        /// </summary>
        public static void CancelBlueHPUpdate()
        {
            _cancelBlueHPUpdate = true;
        }

        /// <summary>
        /// HeroController.TakeDamage (Prefix): Check if damage should get blocked via Resilience block functionality 
        /// when an enemy hits the player, and if so, set StatsShared.BlockNextHit = true.
        /// </summary>
        public static void HandleBlockCheck(HeroController instance, HazardType hazardType)
        {
            if (instance.cState.evading || instance.cState.invulnerable) return;

            Bonfire.Log.LogDebug("[ResilienceLogic] Took damage from hazard type: " + hazardType);

            HandleReflectDamageHitLogic(hazardType); // reflect damage logic, triggered only outside iframes

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


        private const float ReflectDamageArea = 2f;

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

        // Create a reflect damage HitInstance and apply it on enemies in range.
        private static void DealReflectDamage()
        {
            Bonfire.Log.LogDebug("[ResilienceLogic] Reflecting damage to enemies.");

            if (!IsInBlueState(BlueState.REFLECT))
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

            int masks = BlueMasks(Bonfire.Instance.Status.ResilienceStat);
            GameManager.instance.QueuedBlueHealth = masks;
            EventRegister.SendEvent(EventRegisterEvents.AddQueuedBlueHealth);

            if (BlueHpCheckCancelState()) yield break;

            yield return new WaitForSecondsRealtime(0.25f);

            GameManager.instance.QueuedBlueHealth = 0;

            _blueHpAddingInProgress = false;
        }
    }
}