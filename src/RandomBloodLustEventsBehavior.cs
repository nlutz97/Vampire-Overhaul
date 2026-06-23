using System;
using System.Collections.Generic;
using System.Reflection;
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
        private const string BloodLustIncidentId = "vo_bloodlust_event";

        private const float FailedHuntBloodLustGain = 10f;

        private static bool s_bloodLustOptionsRegistered;

        private static readonly string[] GroupTypes = { "peasants", "brigands", "merchants", "patrols", "homestead" };

        private Incident? _bloodLustIncident;
        private bool _forceNextIncident;
        private bool _ignoreIncidentConditions;
        private string _activeGroup = "travelers";

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public void OnBeforeNonReadyObjectsDeleted()
        {
            _bloodLustIncident = null;
            s_bloodLustOptionsRegistered = false;
            RegisterCustomIncidents();
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            RegisterCustomIncidents();
        }

        private void RegisterCustomIncidents()
        {
            if (Game.Current?.ObjectManager == null)
            {
                return;
            }

            _bloodLustIncident = Game.Current.ObjectManager.GetObject<Incident>(BloodLustIncidentId);
            if (_bloodLustIncident == null)
            {
                _bloodLustIncident = Game.Current.ObjectManager.RegisterPresumedObject(
                    new Incident(BloodLustIncidentId));
            }

            if (_bloodLustIncident == null || s_bloodLustOptionsRegistered)
            {
                return;
            }

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
                            () => true,
                            () =>
                            {
                                ApplySuccessfulHunt();
                                return new List<TextObject>
                                {
                                    new TextObject("You return from the hunt feeling satisfied. Your blood lust has been sated.")
                                };
                            },
                            _ => new List<TextObject> { new TextObject("May sate your blood lust") }),
                        IncidentEffect.Custom(
                            () => true,
                            () =>
                            {
                                ApplyFailedHunt();
                                return GetFailedHuntMessages();
                            },
                            _ => new List<TextObject> { new TextObject("May fail and worsen your hunger") }),
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
                            new TextObject("You steel yourself and push the hunger down. The hunger remains, but you endure.")
                        },
                        _ => new List<TextObject> { new TextObject("Resist the hunger; blood lust unchanged") })
                },
                null,
                ClearForcedIncident);

            s_bloodLustOptionsRegistered = true;
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

            QueueRandomIncident();
        }

        public void QueueRandomIncident()
        {
            TryQueueIncident(ignoreConditions: false, out _);
        }

        public bool TryQueueIncident(bool ignoreConditions, out string failureReason)
        {
            _activeGroup = GroupTypes[MBRandom.RandomInt(GroupTypes.Length)];
            return TryQueueBloodLustIncident(ignoreConditions, out failureReason);
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
            if (!_forceNextIncident)
            {
                return false;
            }

            if (_ignoreIncidentConditions)
            {
                if (!IsVampireReadyForForcedIncident())
                {
                    return false;
                }
            }
            else if (!IsVampireReadyForIncidents())
            {
                return false;
            }

            description.SetTextVariable("BLOODLUST_GROUP", _activeGroup);
            return true;
        }

        private bool IsVampireReadyForForcedIncident()
        {
            Settings? settings = Settings.Instance;
            VampireComponent? vampire = Campaign.Current?.GetCampaignBehavior<VampireComponent>();

            return settings != null
                && settings.EnableVampireMechanics
                && vampire != null
                && vampire.IsVampire;
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

        private bool TryQueueBloodLustIncident(bool ignoreConditions, out string failureReason)
        {
            failureReason = string.Empty;
            RegisterCustomIncidents();

            if (Campaign.Current == null)
            {
                failureReason = "no active campaign";
                return false;
            }

            if (Hero.MainHero.IsPrisoner)
            {
                failureReason = "your hero is a prisoner";
                return false;
            }

            MapState? mapState = GameStateManager.Current?.ActiveState as MapState;
            if (mapState == null || _bloodLustIncident == null)
            {
                failureReason = "you must be on the campaign map";
                return false;
            }

            if (MobileParty.MainParty?.MapEvent != null)
            {
                failureReason = "you are currently in a battle";
                return false;
            }

            ClearIncidentCooldown(_bloodLustIncident);
            _ignoreIncidentConditions = ignoreConditions;
            _forceNextIncident = true;
            if (!_bloodLustIncident.CanIncidentBeInvoked())
            {
                ResetForcedIncidentState();
                failureReason = ignoreConditions
                    ? "incident conditions failed unexpectedly"
                    : "blood lust must be above 0 and vampire mechanics must be enabled";
                return false;
            }

            mapState.StartIncident(_bloodLustIncident);
            mapState.NextIncident = null;
            return true;
        }

        private void ApplySuccessfulHunt()
        {
            VampireComponent? vampire = Campaign.Current?.GetCampaignBehavior<VampireComponent>();
            if (vampire == null)
            {
                return;
            }

            vampire.CurrentBloodLust = 0f;
        }

        private void ApplyFailedHunt()
        {
            VampireComponent? vampire = Campaign.Current?.GetCampaignBehavior<VampireComponent>();
            Settings? settings = Settings.Instance;
            if (vampire == null || settings == null)
            {
                return;
            }

            vampire.CurrentBloodLust = MathF.Min(
                vampire.CurrentBloodLust + FailedHuntBloodLustGain,
                settings.MaxBloodLust);
        }

        private List<TextObject> GetFailedHuntMessages()
        {
            string message = _activeGroup is "brigands" or "patrols"
                ? "The encounter turns violent. You barely escape, and the hunger only grows stronger."
                : "The hunt goes poorly. You return empty-handed, blood lust rising.";

            return new List<TextObject> { new TextObject(message) };
        }

        private void ClearForcedIncident()
        {
            ResetForcedIncidentState();
        }

        private void ResetForcedIncidentState()
        {
            _forceNextIncident = false;
            _ignoreIncidentConditions = false;
        }

        private static void ClearIncidentCooldown(Incident incident)
        {
            IncidentsCampaignBehaviour? behavior = Campaign.Current?.GetCampaignBehavior<IncidentsCampaignBehaviour>();
            if (behavior == null)
            {
                return;
            }

            FieldInfo? cooldownField = typeof(IncidentsCampaignBehaviour).GetField(
                "_incidentsOnCooldown",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (cooldownField?.GetValue(behavior) is Dictionary<Incident, CampaignTime> cooldowns)
            {
                cooldowns.Remove(incident);
            }
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}