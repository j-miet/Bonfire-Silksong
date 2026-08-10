using HarmonyLib;

namespace BonfireSilksong
{
    internal static class SprintRaceController_Patch
    {
        [HarmonyPatch(typeof(SprintRaceController), "GetRaceInfo")]
        class SprintRaceController_GetRaceInfo
        {
            static void Postfix(ref float currentBaseSpeed)
            {
                SettingsPatchHelpers.MakeSprintmasterRaceEasier(ref currentBaseSpeed);
            }
        }
    }
}