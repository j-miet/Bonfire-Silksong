using HarmonyLib;

namespace BonfireSilksong
{
    internal class HeroController_Patch
    {
        [HarmonyPatch(typeof(HeroController), "TickDeliveryItems")]
        class HeroController_TickDeliveryItems
        {
            static bool Prefix()
            {
                return SettingsPatchHelpers.DeliveryQuestTimerCheck();
            }
        }

        [HarmonyPatch(typeof(HeroController), "FinishedEnteringScene")]
        class HeroController_FinishedEnteringScene
        {
            static void Postfix(HeroController __instance)
            {
                TeleportPatchHelpers.HandleArrival(__instance);
            }
        }
    }
}