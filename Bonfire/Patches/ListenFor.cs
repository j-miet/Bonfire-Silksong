using HarmonyLib;
using HutongGames.PlayMaker.Actions;

using BonfireSilksong.GUIComponents;
using BonfireSilksong.Leveling;

namespace BonfireSilksong.Patches
{
    /// <summary>
    /// Patches for input listener classes, starting with ListenFor prefix e.g. ListenForAttack.
    /// </summary>
    internal class ListenFor_Patch
    {
        [HarmonyPatch(typeof(ListenForAttack), "OnUpdate")]
        class ListenForAttack_OnUpdate
        {
            static bool Prefix()
            {
                if (!CursorPatchLogic.AllowInput()) return false;

                StrengthStat.ReleaseNeedleChargeAutomatically();

                return true;
            }
        }

        [HarmonyPatch(typeof(ListenForAttackV2), "OnUpdate")]
        class ListenForAttackV2_OnUpdate
        {
            static bool Prefix()
            {
                if (!CursorPatchLogic.AllowInput()) return false;

                StrengthStat.ReleaseNeedleChargeAutomatically();

                return true;
            }
        }

        [HarmonyPatch(typeof(ListenForJump), "OnUpdate")]
        class ListenForJump_OnUpdate
        {
            static bool Prefix()
            {
                if (!CursorPatchLogic.AllowInput()) return false;

                return true;
            }
        }

        [HarmonyPatch(typeof(ListenForJumpV2), "OnUpdate")]
        class ListenForJumpV2_OnUpdate
        {
            static bool Prefix()
            {
                if (!CursorPatchLogic.AllowInput()) return false;

                return true;
            }
        }

        [HarmonyPatch(typeof(ListenForCast), "OnUpdate")]
        class ListenForCast_OnUpdate
        {
            static bool Prefix()
            {
                if (ResilienceStat.IsInBlueState(BlueState.GLASSCANNON) &&
                    HeroController.instance.CanBind()) // must check, can't otherwise acquire new abilities!
                {
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(ListenForTaunt), "OnUpdate")]
        class ListenForTaunt_OnUpdate
        {
            static void Prefix()
            {
                if (GameManager.instance.inputHandler.inputActions.Taunt.WasPressed)
                    LevelingPatch.Taunted = true;
            }
        }

        [HarmonyPatch(typeof(ListenForTauntV2), "OnUpdate")]
        class ListenForTauntV2_OnUpdate
        {
            static void Prefix()
            {
                if (GameManager.instance.inputHandler.inputActions.Taunt.WasPressed)
                    LevelingPatch.Taunted = true;
            }
        }
    }
}