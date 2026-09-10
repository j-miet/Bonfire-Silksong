using HarmonyLib;
using HutongGames.PlayMaker.Actions;

using BonfireSilksong.Settings;

namespace BonfireSilksong.Patches
{
    internal class PlayerDataBoolTest_Patch
    {
        [HarmonyPatch(typeof(PlayerDataBoolTest), "OnEnter")]
        class Patch_PlayerDataBoolTest
        {
            static bool Prefix(PlayerDataBoolTest __instance)
            {
                // negate return value to ignore running the actual OnEnter when door should stay open
                return !SettingsPatch.KeepBeastChapelDoorStateOpen(__instance);
            }
        }
    }
}