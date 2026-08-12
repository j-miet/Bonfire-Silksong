using HarmonyLib;

namespace BonfireSilksong
{
    internal class SprintRaceController_Patch
    {
        [HarmonyPatch(typeof(SprintRaceController), "GetRaceInfo")]
        class SprintRaceController_GetRaceInfo
        {
            static void Postfix(ref float currentBaseSpeed)
            {
                currentBaseSpeed = SettingsLogic.MakeSprintmasterRaceEasier(currentBaseSpeed);
            }
        }
    }
}