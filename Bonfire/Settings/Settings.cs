using GlobalSettings;
using HarmonyLib;
using HutongGames.PlayMaker.Actions;

namespace BonfireSilksong
{
    /// <summary>
    /// Settings window logic.
    /// </summary>
    public class Settings
    {
        // if player uses unlock all crest feature, the beast crest unlock prevents fighting Savage Beastfly boss, thus 
        // preventing getting the second encounter and its mask shard reward.
        // This fix intercepts the check when player enters the chapel of the beast gate: normally it would
        // check if player already has the crest (in this case always true) and set door open/closed. Luckily there's
        // a helper flag for boss kill called 'defeatedBoneFlyerGiant' which can be then used for tracking this state
        // -> thus if player leaves the chapel after killing the boss, the door closes permanently. This is no longer
        // an issue because there's no crest to collect anyway if it was already unlocked.
        // [Note] there's also PlayerData.instance.chapelClosed_beast, but game will override its value before door 
        // state logic, making it useless for this particular issue
        [HarmonyPatch(typeof(PlayerDataBoolTest), "OnEnter")]
        class Patch_PlayerDataBoolTest
        {
            static bool Prefix(PlayerDataBoolTest __instance)
            {
                if (Bonfire.Instance.Status.UnlockedAllCrests
                    && __instance.boolName.Value == "chapelClosed_beast"
                    && Gameplay.WarriorCrest.IsUnlocked
                    && !PlayerData.instance.defeatedBoneFlyerGiant)
                {
                    // FSM event to keep the chapel gate open. It has other values: "CLOSE" which means its closed
                    // when scene loads and "DO CLOSE" which triggers the door closing animation
                    __instance.Fsm.Event("OPEN");
                    __instance.Finish();

                    return false; // return false to skip default OnEnter behavior
                }

                return true;
            }
        }

        // reduce fleatopia game score requirements by 50% (odd values are rounded down)
        [HarmonyPatch(typeof(PlayerData), "FleaGamesIsJugglingChampion", MethodType.Getter)]
        class PlayerData_FleaGamesIsJugglingChampion
        {
            static void Postfix(PlayerData __instance, ref bool __result)
            {
                if (!Bonfire.Instance.Status.ReduceFleaGamesScoreLimit) return;
                __result = __instance.fleaGames_juggling_highscore > 15;
            }
        }

        [HarmonyPatch(typeof(PlayerData), "FleaGamesIsJugglingSethChampion", MethodType.Getter)]
        class PlayerData_FleaGamesIsJugglingSethChampion
        {
            static void Postfix(PlayerData __instance, ref bool __result)
            {
                if (!Bonfire.Instance.Status.ReduceFleaGamesScoreLimit) return;
                __result = __instance.fleaGames_juggling_highscore > 27;
            }
        }

        [HarmonyPatch(typeof(PlayerData), "FleaGamesIsBouncingChampion", MethodType.Getter)]
        class PlayerData_FleaGamesIsBouncingChampion
        {
            static void Postfix(PlayerData __instance, ref bool __result)
            {
                if (!Bonfire.Instance.Status.ReduceFleaGamesScoreLimit) return;
                __result = __instance.fleaGames_bouncing_highscore > 21;
            }
        }

        [HarmonyPatch(typeof(PlayerData), "FleaGamesIsBouncingSethChampion", MethodType.Getter)]
        class PlayerData_FleaGamesIsBouncingSethChampion
        {
            static void Postfix(PlayerData __instance, ref bool __result)
            {
                if (!Bonfire.Instance.Status.ReduceFleaGamesScoreLimit) return;
                __result = __instance.fleaGames_bouncing_highscore > 34;
            }
        }

        [HarmonyPatch(typeof(PlayerData), "FleaGamesIsDodgingChampion", MethodType.Getter)]
        class PlayerData_FleaGamesIsDodgingChampion
        {
            static void Postfix(PlayerData __instance, ref bool __result)
            {
                if (!Bonfire.Instance.Status.ReduceFleaGamesScoreLimit) return;
                __result = __instance.fleaGames_dodging_highscore > 32;
            }
        }

        [HarmonyPatch(typeof(PlayerData), "FleaGamesIsDodgingSethChampion", MethodType.Getter)]
        class PlayerData_FleaGamesIsDodgingSethChampion
        {
            static void Postfix(PlayerData __instance, ref bool __result)
            {
                if (!Bonfire.Instance.Status.ReduceFleaGamesScoreLimit) return;
                __result = __instance.fleaGames_dodging_highscore > 45;
            }
        }

        // shell shards replenishment
        [HarmonyPatch(typeof(CurrencyManager), "ProcessAddCurrency")]
        class CurrencyManager_ProcessAddCurrency
        {
            static void Postfix(CurrencyType type)
            {
                if (!Bonfire.Instance.Status.InfiniteShellShardsEnabled) return;
                if (type != CurrencyType.Shard) return;
                if (Bonfire.Instance.Status.InfiniteShardsBenchOnly && !PlayerData.instance.atBench) return;

                PlayerData.instance.ShellShards = Gameplay.GetCurrencyCap(CurrencyType.Shard);
            }
        }

        [HarmonyPatch(typeof(CurrencyManager), "ProcessTakeCurrency")]
        class CurrencyManager_ProcessTakeCurrency
        {
            static void Postfix(CurrencyType type)
            {
                if (!Bonfire.Instance.Status.InfiniteShellShardsEnabled) return;
                if (type != CurrencyType.Shard) return;
                if (Bonfire.Instance.Status.InfiniteShardsBenchOnly && !PlayerData.instance.atBench) return;

                PlayerData.instance.ShellShards = Gameplay.GetCurrencyCap(CurrencyType.Shard);
            }
        }
    }
}