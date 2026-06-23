using TaleWorlds.CampaignSystem;

namespace VampireOverhaul.Events.BloodLust
{
    public abstract class BloodLustEventBase
    {
        public abstract string EventId { get; }
        public abstract string EventTitle { get; }

        public abstract bool CanTrigger(VampireComponent vampire);
        public abstract void Trigger(bool ignoreConditions = false);

        protected float GetMaxBloodLust()
        {
            Settings? settings = Settings.Instance;
            return settings?.MaxBloodLust ?? 100f;
        }

        protected bool IsVampireMechanicsEnabled()
        {
            return Settings.Instance?.EnableVampireMechanics == true;
        }
    }
}