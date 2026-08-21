using HarmonyLib;

namespace BonfireSilksong
{
    internal class PlayerData_Patch
    {
        // reduce fleatopia game score requirements by 50% (odd values are rounded down).
        // There are 6 checks in total: 3 for becoming Champion, 3 for beating Seth's scores.
        // These will not update scoreboard visuals.
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

        // public void TakeHealth(int amount, bool hasBlueHealth, bool allowFracturedMaskBreak)
        [HarmonyPatch(typeof(PlayerData), "TakeHealth")]
        class PlayerData_TakeHealth
        {
            static void Prefix(ref int amount)
            {
                amount = ResilienceLogic.PreventHPLossOnBlock(amount);
            }
        }

        [HarmonyPatch(typeof(PlayerData), "SilkSkillCost", MethodType.Getter)]
        class PlayerData_SilkSkillCost
        {
            static void Postfix(ref int __result)
            {
                if (IntelligenceLogic.CheckWeakBindRequirement())
                {
                    if (__result > 0)
                        __result -= 1;
                }

                if (IntelligenceLogic.CheckIntelligenceMilestone(4))
                {
                    if (__result > 0)
                        __result -= 1;
                }
            }
        }
    }
}