using HarmonyLib;

namespace BonfireSilksong
{
    internal class SprintRaceController_Patch
    {
        // public void GetRaceInfo(out int runnerLaps, out int heroLaps, out float currentBaseSpeed)
        [HarmonyPatch(typeof(SprintRaceController), "GetRaceInfo")]
        class SprintRaceController_GetRaceInfo
        {
            static void Postfix(ref float currentBaseSpeed)
            {
                currentBaseSpeed = SettingsPatch.MakeSprintmasterRaceEasier(currentBaseSpeed);
            }
        }
    }
}