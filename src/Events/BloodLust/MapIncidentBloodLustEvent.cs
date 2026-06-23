using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace VampireOverhaul.Events.BloodLust
{
    public class MapIncidentBloodLustEvent : BloodLustEventBase
    {
        public override string EventId => "map_incident_bloodlust";
        public override string EventTitle => "Blood Lust Rising";

        public override bool CanTrigger(VampireComponent vampire)
        {
            if (!IsVampireMechanicsEnabled() || !vampire.IsVampire)
            {
                return false;
            }

            return vampire.CurrentBloodLust > 0f;
        }

        public override void Trigger(bool ignoreConditions = false)
        {
            RandomBloodLustEventsBehavior? behavior = Campaign.Current?.GetCampaignBehavior<RandomBloodLustEventsBehavior>();
            if (behavior == null)
            {
                return;
            }

            if (!behavior.TryQueueIncident(ignoreConditions, out string failureReason))
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[Blood Lust] Could not open incident: {failureReason}.", Colors.Red));
            }
        }
    }
}