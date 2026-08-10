using HarmonyLib;

namespace BonfireSilksong
{
    internal static class CurrencyManager_Patch
    {
        [HarmonyPatch(typeof(CurrencyManager), "ProcessAddCurrency")]
        class CurrencyManager_ProcessAddCurrency
        {
            static void Postfix(CurrencyType type)
            {
                SettingsPatchHelpers.RefillShellShardsAtBench(type);
            }
        }

        [HarmonyPatch(typeof(CurrencyManager), "ProcessTakeCurrency")]
        class CurrencyManager_ProcessTakeCurrency
        {
            static void Postfix(CurrencyType type)
            {
                SettingsPatchHelpers.RefillShellShardsAtBench(type);
            }
        }
    }
}