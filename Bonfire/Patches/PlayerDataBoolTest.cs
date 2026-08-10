using HarmonyLib;
using HutongGames.PlayMaker.Actions;

namespace BonfireSilksong
{
    internal static class PlayerDataBoolTest_Patch
    {
        [HarmonyPatch(typeof(PlayerDataBoolTest), "OnEnter")]
        class Patch_PlayerDataBoolTest
        {
            static bool Prefix(PlayerDataBoolTest __instance)
            {
                return SettingsPatchHelpers.UpdateBeastChapelDoorState(__instance);
            }
        }
    }
}