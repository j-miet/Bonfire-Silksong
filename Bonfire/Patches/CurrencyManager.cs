using HarmonyLib;

namespace BonfireSilksong
{
    internal class CurrencyManager_Patch
    {
        // public static void ProcessAddCurrency(int amount, CurrencyType type, bool showCounter)
        [HarmonyPatch(typeof(CurrencyManager), "ProcessAddCurrency")]
        class CurrencyManager_ProcessAddCurrency
        {
            static void Postfix(CurrencyType type)
            {
                SettingsLogic.RefillShellShardsAtBench(type);
            }
        }

        // public static void ProcessTakeCurrency(int amount, CurrencyType type, bool showCounter)
        [HarmonyPatch(typeof(CurrencyManager), "ProcessTakeCurrency")]
        class CurrencyManager_ProcessTakeCurrency
        {
            static void Postfix(CurrencyType type)
            {
                SettingsLogic.RefillShellShardsAtBench(type);
            }
        }
    }
}