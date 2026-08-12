using HarmonyLib;
using HutongGames.PlayMaker.Actions;

namespace BonfireSilksong
{
    internal class PlayerDataBoolTest_Patch
    {
        [HarmonyPatch(typeof(PlayerDataBoolTest), "OnEnter")]
        class Patch_PlayerDataBoolTest
        {
            static bool Prefix(PlayerDataBoolTest __instance)
            {
                // negate return value to ignore running the actual OnEnter when door should stay open
                return !SettingsLogic.KeepBeastChapelDoorStateOpen(__instance);
            }
        }
    }
}