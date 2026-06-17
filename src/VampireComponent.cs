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

            // Passive daily gain
            float dailyGain = 4f * settings.BloodGainMultiplier;
            CurrentBloodLust = MathF.Min(CurrentBloodLust + dailyGain, settings.MaxBloodLust);

            _warningShownToday = false;

            // Daily report
            InformationManager.DisplayMessage(new InformationMessage(
                $"[Dusk] Blood Lust: {CurrentBloodLust:F0} / {settings.MaxBloodLust:F0}",
                Colors.Cyan));

            // High Blood Lust warning
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

        private void OnAgentRemoved(Agent victim, Agent killer, AgentState agentState, KillingBlow blow)
        {
            var settings = Settings.Instance;
            if (settings == null || !IsVampire) return;

            // Only reduce Blood Lust when the player kills a human in melee
            if (killer != null && killer == Agent.Main && victim.IsHuman
                && agentState == AgentState.Killed && !blow.IsMissile)
            {
                CurrentBloodLust = MathF.Max(0f, CurrentBloodLust - settings.BloodReductionOnKill);

                // Optional feedback
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Blood Lust reduced by feeding on the kill.", Colors.Green));
            }
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

            public override void OnAgentRemoved(
                Agent affectedAgent,
                Agent affectorAgent,
                AgentState agentState,
                KillingBlow blow)
            {
                _component.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
            }
        }
    }
}