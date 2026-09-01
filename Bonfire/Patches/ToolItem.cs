using GlobalSettings;
using HarmonyLib;

namespace BonfireSilksong
{
    internal class ToolItem_Patch
    {
        [HarmonyPatch(typeof(ToolItem), "IsEquipped", MethodType.Getter)]
        class ToolItem_IsEquipped
        {
            static void Postfix(ToolItem __instance, ref bool __result)
            {
                __result = SettingsPatch.ToggleCompass(__instance);
            }
        }
    }
}