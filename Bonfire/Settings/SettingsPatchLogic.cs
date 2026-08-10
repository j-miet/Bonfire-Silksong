using GlobalSettings;
using HutongGames.PlayMaker.Actions;

namespace BonfireSilksong
{
    public static class SettingsPatchLogic
    {
        /// <summary>
        /// CurrencyManager.ProcessAddCurrency (Postfix) + CurrencyManager.ProcessTakeCurrency (Postfix): shell shards 
        /// replenishment to full when visiting a bench.
        /// </summary>
        public static void RefillShellShardsAtBench(CurrencyType type)
        {
            if (Bonfire.Instance.Status.RefillShardsAtBench && !PlayerData.instance.atBench) return;
            if (type != CurrencyType.Shard) return;

            PlayerData.instance.ShellShards = Gameplay.GetCurrencyCap(CurrencyType.Shard);
        }

        /// <summary>
        /// SprintRaceController.GetRaceInfo (Postfix): reduce SprintMaster Swift's run speed by 50%.
        /// </summary>
        public static void MakeSprintmasterRaceEasier(ref float currentBaseSpeed)
        {
            if (!Bonfire.Instance.Status.EasierSprintmasterRaces) return;
            currentBaseSpeed *= 0.5f;
        }

        /// <summary>
        /// PlayerDataBoolTest.OnEnter (Prefix): check if Chapel of the Beast door should be open.
        /// </summary>
        public static bool UpdateBeastChapelDoorState(PlayerDataBoolTest instance)
        {
            // if player uses unlock all crests feature, the beast crest unlock prevents fighting Savage Beastfly boss, 
            // thus preventing getting the second encounter and its mask shard reward.
            // This fix intercepts the check when player enters the chapel of the beast gate: normally it would
            // check if player already has the crest (in this case always true) and set door open/closed. Luckily 
            // there's a helper flag for boss kill called 'defeatedBoneFlyerGiant' which can be then used for tracking 
            // this state.
            // -> thus if player leaves the chapel after killing the boss, the door closes permanently but otherwise
            // stays open. Closing is of course no longer an issue because there's no crest to collect anyway if it was 
            // already unlocked and at this point boss has also been killed, allowing the second encounter wish to 
            // appear later.
            // [Note] there's also PlayerData.instance.chapelClosed_beast, but game will override its value before door 
            // state logic, making it useless for this particular issue

            if (Bonfire.Instance.Status.UnlockedAllCrests
                        && instance.boolName.Value == "chapelClosed_beast"
                        && Gameplay.WarriorCrest.IsUnlocked
                        && !PlayerData.instance.defeatedBoneFlyerGiant)
            {
                // FSM event to keep the chapel gate open. It has other values: "CLOSE" which means its closed
                // when scene loads and "DO CLOSE" which triggers the door closing animation
                instance.Fsm.Event("OPEN");
                instance.Finish();

                return false; // return false to skip default OnEnter behavior
            }

            return true;
        }

        /// <summary>
        /// DeliveryQuestItem.TakeHitForItem (Prefix):
        /// Remove damage penalties from courier delivery quests + prevent teleporting with active delivery items.
        /// </summary>
        public static bool DeliveryQuestHitCheck()
        {
            // this will ensure bench teleporting breaks all delivery quest items after it calls TakeHit.
            if (PlayerData.instance.atBench) return true;

            if (!Bonfire.Instance.Status.DeliveryQuestsRemoveDamage) return true;

            return false;
        }

        /// <summary>
        /// HeroController.TickDeliveryItems (Prefix): Remove timers from courier delivery quests.
        /// </summary>
        public static bool DeliveryQuestTimerCheck()
        {
            if (!Bonfire.Instance.Status.DeliveryQuestsRemoveTimer) return true;

            return false;
        }

    }
}