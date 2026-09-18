using HarmonyLib;
using HutongGames.PlayMaker.Actions;

using BonfireSilksong.Settings;

namespace BonfireSilksong.Patches
{
    internal class GetIsCrestUnlocked_Patch
    {
        [HarmonyPatch(typeof(GetIsCrestUnlocked), "IsTrue", MethodType.Getter)]
        class GetIsCrestUnlocked_OnEnter
        {
            static void Postfix(GetIsCrestUnlocked __instance, ref bool __result)
            {
                if (SettingsPatch.CheckIfArchitectChapelDoorOpen(__instance))
                    __result = true;
            }
        }
    }
}
