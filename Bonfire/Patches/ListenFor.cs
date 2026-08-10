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
                if (!CursorPatchHelpers.AllowInput()) return false;

                // TODO 
                // make needle strike charge time very short + auto-cast it when fully charged (Strength milestone 1)

                return true;
            }
        }

        [HarmonyPatch(typeof(ListenForAttackV2), "OnUpdate")]
        class ListenForAttack_OnUpdateV2
        {
            static bool Prefix()
            {
                if (!CursorPatchHelpers.AllowInput()) return false;

                // TODO 
                // make needle strike charge time very short + auto-cast it when fully charged (Strength milestone 1)

                return true;
            }
        }

        [HarmonyPatch(typeof(ListenForJump), "OnUpdate")]
        class ListenForJump_OnUpdate
        {
            static bool Prefix()
            {
                if (!CursorPatchHelpers.AllowInput()) return false;

                return true;
            }
        }

        [HarmonyPatch(typeof(ListenForJumpV2), "OnUpdate")]
        class ListenForJump_OnUpdateV2
        {
            static bool Prefix()
            {
                if (!CursorPatchHelpers.AllowInput()) return false;

                return true;
            }
        }
    }
}