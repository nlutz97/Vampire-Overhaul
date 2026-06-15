using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace VampireOverhaul
{
    public class VampireComponent : CampaignBehaviorBase
    {
        private bool _isVampire = false;
        private float _currentBloodLust = 0f;

        public bool IsVampire
        {
            get => _isVampire;
            set => _isVampire = value;
        }

        public float CurrentBloodLust
        {
            get => _currentBloodLust;
            set => _currentBloodLust = value;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        private void OnDailyTick()
        {
            var settings = Settings.Instance;
            if (settings == null) return;

            var hero = Hero.MainHero;
            if (hero == null) return;

            // Check if player enabled vampire mechanics in MCM
            if (settings.EnableVampireMechanics && !IsVampire)
            {
                IsVampire = true;
                CurrentBloodLust = 0f;

                InformationManager.DisplayMessage(new InformationMessage(
                    "You are now a vampire.", Colors.Red));
            }

            if (!IsVampire) return;

            // Passive Blood Lust gain
            float dailyGain = 5f * settings.BloodGainMultiplier;
            CurrentBloodLust = MathF.Min(CurrentBloodLust + dailyGain, settings.MaxBloodLust);

            // Daily debug message
            InformationManager.DisplayMessage(new InformationMessage(
                $"[Dusk] Blood Lust: {CurrentBloodLust:F0} / {settings.MaxBloodLust:F0}",
                Colors.Cyan));
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("IsVampire", ref _isVampire);
            dataStore.SyncData("CurrentBloodLust", ref _currentBloodLust);
        }
    }
}