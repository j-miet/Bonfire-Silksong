using HarmonyLib;

using BonfireSilksong.Settings;

namespace BonfireSilksong.Patches
{
    internal class DeliveryQuestItem_Patch
    {
        // public static void TakeHitForItem(ActiveItem item, bool hitEffect, int amount)
        [HarmonyPatch(typeof(DeliveryQuestItem), "TakeHitForItem", new[] {
            typeof(DeliveryQuestItem.ActiveItem),
            typeof(bool),
            typeof(int)
        })]
        class DeliveryQuestItem_TakeHitForItem
        {
            static bool Prefix()
            {
                return SettingsPatch.DeliveryQuestHitCheck();
            }
        }
    }
}