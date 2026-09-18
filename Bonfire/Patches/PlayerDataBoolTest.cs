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
                // prevent OnEnter from running if the door should stay open.
                return !SettingsPatch.CheckIfCrestChapelDoorOpen(__instance);
            }
        }
    }
}