using HarmonyLib;
using HutongGames.PlayMaker.Actions;

namespace BonfireSilksong
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

                StrengthLogic.ReleaseNeedleChargeAutomatically();

                return true;
            }
        }

        [HarmonyPatch(typeof(ListenForAttackV2), "OnUpdate")]
        class ListenForAttackV2_OnUpdate
        {
            static bool Prefix()
            {
                if (!CursorPatchLogic.AllowInput()) return false;

                StrengthLogic.ReleaseNeedleChargeAutomatically();

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
                if (ResilienceLogic.IsInBlueState(BlueState.GLASSCANNON))
                    return false;

                return true;
            }
        }
    }
}