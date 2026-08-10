using HarmonyLib;

namespace BonfireSilksong
{
    internal static class DeliveryQuestItem_Patch
    {
        [HarmonyPatch(typeof(DeliveryQuestItem), "TakeHitForItem", new[] {
            typeof(DeliveryQuestItem.ActiveItem),
            typeof(bool),
            typeof(int)
        })]
        class DeliveryQuestItem_TakeHitForItem
        {
            static bool Prefix()
            {
                return SettingsPatchHelpers.DeliveryQuestHitCheck();
            }
        }
    }
}