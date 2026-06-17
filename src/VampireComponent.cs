using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;

namespace VampireOverhaul
{
    public class VampireComponent : CampaignBehaviorBase
    {
        [SaveableField(1)]
        private bool _isVampire = false;

        [SaveableField(2)]
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

        private bool _warningShownToday = false;

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
        }

        private void OnDailyTick()
        {
            var settings = Settings.Instance;
            if (settings == null) return;

            if (settings.EnableVampireMechanics && !IsVampire)
            {
                IsVampire = true;
                CurrentBloodLust = 0f;

                InformationManager.DisplayMessage(new InformationMessage(
                    "You are now a vampire.", Colors.Red));
            }

            if (!IsVampire) return;

            // Passive daily gain — scales with multiplier and current blood lust pressure
            float pressureFactor = 1f + (CurrentBloodLust / MathF.Max(settings.MaxBloodLust, 1f)) * 0.25f;
            float dailyGain = 3.1f * settings.BloodGainMultiplier * pressureFactor;
            CurrentBloodLust = MathF.Min(CurrentBloodLust + dailyGain, settings.MaxBloodLust);

            // Reset warning flag for the new day
            _warningShownToday = false;

            // Show daily Blood Lust report
            InformationManager.DisplayMessage(new InformationMessage(
                $"[Dusk] Blood Lust: {CurrentBloodLust:F0} / {settings.MaxBloodLust:F0}",
                Colors.Cyan));

            // Warning if Blood Lust is getting high
            float warningThreshold = settings.MaxBloodLust * settings.BloodLustWarningThreshold;
            if (CurrentBloodLust >= warningThreshold && !_warningShownToday)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "Your blood lust is rising dangerously high...", Colors.Red));
                _warningShownToday = true;
            }
        }

        private void OnMissionStarted(IMission mission)
        {
            if (!IsVampire || !(mission is Mission bannerlordMission))
            {
                return;
            }

            bannerlordMission.AddMissionBehavior(new VampireMissionBehavior(this));
        }

        internal void OnPlayerMeleeKill()
        {
            var settings = Settings.Instance;
            if (settings == null || !IsVampire)
            {
                return;
            }

            CurrentBloodLust = MathF.Max(0f, CurrentBloodLust - settings.BloodReductionOnKill);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("IsVampire", ref _isVampire);
            dataStore.SyncData("CurrentBloodLust", ref _currentBloodLust);
        }

        private sealed class VampireMissionBehavior : MissionBehavior
        {
            private readonly VampireComponent _component;

            public VampireMissionBehavior(VampireComponent component)
            {
                _component = component;
            }

            public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

            public override void OnAgentHit(
                Agent affectorAgent,
                Agent affectedAgent,
                in MissionWeapon weapon,
                in Blow blow,
                in AttackCollisionData attackCollisionData)
            {
                var settings = Settings.Instance;
                if (settings == null || !_component.IsVampire)
                {
                    return;
                }

                // Only reduce Blood Lust on melee kills by the player
                if (affectorAgent != Agent.Main || !affectedAgent.IsHuman || blow.IsMissile)
                {
                    return;
                }

                if (blow.InflictedDamage >= affectedAgent.HealthLimit)
                {
                    _component.OnPlayerMeleeKill();
                }
            }
        }
    }
}