using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace VampireOverhaul.Events.BloodLust
{
    public class HuntingEvent : BloodLustEventBase
    {
        public override string EventId => "hunting_event";
        public override string EventTitle => "The Hunger Calls";

        public override bool CanTrigger(VampireComponent vampire)
        {
            if (!IsVampireMechanicsEnabled() || !vampire.IsVampire)
            {
                return false;
            }

            return vampire.CurrentBloodLust > GetMaxBloodLust() * 0.5f;
        }

        public override void Trigger(bool ignoreConditions = false)
        {
            InformationManager.DisplayMessage(new InformationMessage(
                "You feel the hunger rising. A hunting opportunity presents itself.", Colors.Cyan));
        }
    }
}