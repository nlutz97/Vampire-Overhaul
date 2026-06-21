using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Incidents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using IncidentTrigger = TaleWorlds.CampaignSystem.CampaignBehaviors.IncidentsCampaignBehaviour.IncidentTrigger;
using IncidentType = TaleWorlds.CampaignSystem.CampaignBehaviors.IncidentsCampaignBehaviour.IncidentType;

namespace VampireOverhaul
{
    public class RandomBloodLustEventsBehavior : CampaignBehaviorBase, INonReadyObjectHandler
    {
        private static readonly string[] GroupTypes = { "peasants", "brigands", "merchants", "patrols", "homestead" };

        private Incident? _bloodLustIncident;
        private bool _incidentsRegistered;
        private bool _forceNextIncident;
        private string _activeGroup = "travelers";

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public void OnBeforeNonReadyObjectsDeleted()
        {
            _incidentsRegistered = false;
            _bloodLustIncident = null;
            RegisterCustomIncidents();
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            RegisterCustomIncidents();
        }

        private void RegisterCustomIncidents()
        {
            if (_incidentsRegistered || Game.Current?.ObjectManager == null)
            {
                return;
            }

            _bloodLustIncident = Game.Current.ObjectManager.RegisterPresumedObject(
                new Incident("vo_bloodlust_event"));

            _bloodLustIncident.Initialize(
                "Blood Lust Rising",
                "Your blood lust has grown difficult to control. You come across a group of {BLOODLUST_GROUP} ahead, and the hunger stirs within you.",
                IncidentTrigger.LeavingEncounter,
                IncidentType.PartyCampLife,
                CampaignTime.Days(2f),
                CanShowBloodLustIncident);

            _bloodLustIncident.AddOption(
                "I will hunt them for their blood.",
                new List<IncidentEffect>
                {
                    IncidentEffect.Select(
                        IncidentEffect.Custom(
                            ApplySuccessfulHunt,
                            () => new List<TextObject>
                            {
                                new TextObject("You return from the hunt feeling satisfied. Your blood lust has been sated.")
                            },
                            _ => new List<TextObject> { new TextObject("May sate your blood lust") }),
                        IncidentEffect.Group(
                            IncidentEffect.MoraleChange(-8f),
                            IncidentEffect.Custom(
                                ApplyFailedHunt,
                                GetFailedHuntMessages,
                                _ => new List<TextObject> { new TextObject("Risky choice") })),
                        0.65f)
                },
                null,
                ClearForcedIncident);

            _bloodLustIncident.AddOption(
                "I must resist this urge.",
                new List<IncidentEffect>
                {
                    IncidentEffect.Custom(
                        () => true,
                        () => new List<TextObject>
                        {
                            new TextObject("You steel yourself and push the hunger down.")
                        },
                        _ => new List<TextObject> { new TextObject("Resist the hunger") })
                },
                null,
                ClearForcedIncident);

            _incidentsRegistered = true;
        }

        private void OnDailyTick()
        {
            if (!ShouldRollForBloodLustIncident())
            {
                return;
            }

            VampireComponent? vampire = Campaign.Current?.GetCampaignBehavior<VampireComponent>();
            Settings? settings = Settings.Instance;
            if (vampire == null || settings == null || !vampire.IsVampire)
            {
                return;
            }

            float bloodLustPercent = vampire.CurrentBloodLust / settings.MaxBloodLust;
            float triggerChance = bloodLustPercent > 0.7f ? 0.28f : 0.06f;

            if (MBRandom.RandomFloat > triggerChance)
            {
                return;
            }

            _activeGroup = GroupTypes[MBRandom.RandomInt(GroupTypes.Length)];
            QueueBloodLustIncident();
        }

        private bool ShouldRollForBloodLustIncident()
        {
            if (Campaign.Current == null || Hero.MainHero.IsPrisoner)
            {
                return false;
            }

            Settings? settings = Settings.Instance;
            if (settings == null || !settings.EnableVampireMechanics)
            {
                return false;
            }

            MobileParty? mainParty = MobileParty.MainParty;
            if (mainParty == null || mainParty.MapEvent != null)
            {
                return false;
            }

            return GameStateManager.Current?.ActiveState is MapState;
        }

        private bool CanShowBloodLustIncident(TextObject description)
        {
            if (!_forceNextIncident || !IsVampireReadyForIncidents())
            {
                return false;
            }

            description.SetTextVariable("BLOODLUST_GROUP", _activeGroup);
            return true;
        }

        private bool IsVampireReadyForIncidents()
        {
            Settings? settings = Settings.Instance;
            VampireComponent? vampire = Campaign.Current?.GetCampaignBehavior<VampireComponent>();

            return settings != null
                && settings.EnableVampireMechanics
                && vampire != null
                && vampire.IsVampire
                && vampire.CurrentBloodLust > 0f;
        }

        private void QueueBloodLustIncident()
        {
            RegisterCustomIncidents();

            MapState? mapState = GameStateManager.Current?.LastOrDefault<MapState>();
            if (mapState == null || _bloodLustIncident == null)
            {
                return;
            }

            _forceNextIncident = true;
            if (!_bloodLustIncident.CanIncidentBeInvoked())
            {
                _forceNextIncident = false;
                return;
            }

            mapState.NextIncident = _bloodLustIncident;
        }

        private bool ApplySuccessfulHunt()
        {
            VampireComponent? vampire = Campaign.Current?.GetCampaignBehavior<VampireComponent>();
            if (vampire == null)
            {
                return false;
            }

            vampire.CurrentBloodLust = 0f;
            return true;
        }

        private bool ApplyFailedHunt()
        {
            MobileParty? party = MobileParty.MainParty;
            if (party != null)
            {
                party.RecentEventsMorale -= 8f;
            }

            return true;
        }

        private List<TextObject> GetFailedHuntMessages()
        {
            string message = _activeGroup is "brigands" or "patrols"
                ? "The encounter turns violent. You barely escape."
                : "The hunt goes poorly. You return empty-handed.";

            return new List<TextObject> { new TextObject(message) };
        }

        private void ClearForcedIncident()
        {
            _forceNextIncident = false;
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}