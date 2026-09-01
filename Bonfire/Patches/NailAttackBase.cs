using HarmonyLib;

namespace BonfireSilksong
{
    internal class NailAttackBase_Patch
    {
        [HarmonyPatch(typeof(NailAttackBase), "OnSlashStarting")]
        class NailAttackBase_OnSlashStarting
        {
            static void Postfix(NailAttackBase __instance)
            {
                StrengthStat.UpdateNeedleRange(__instance);
            }
        }
    }
}

