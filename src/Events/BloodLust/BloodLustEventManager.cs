using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace VampireOverhaul.Events.BloodLust
{
    public static class BloodLustEventManager
    {
        private static readonly List<BloodLustEventBase> RegisteredEvents = new List<BloodLustEventBase>();
        private static readonly Random Random = new Random();
        private static bool _defaultEventsRegistered;

        public static void RegisterDefaultEvents()
        {
            if (_defaultEventsRegistered)
            {
                return;
            }

            RegisterEvent(new HuntingEvent());
            RegisterEvent(new MapIncidentBloodLustEvent());
            _defaultEventsRegistered = true;
        }

        public static void RegisterEvent(BloodLustEventBase bloodLustEvent)
        {
            if (RegisteredEvents.Exists(e => e.EventId == bloodLustEvent.EventId))
            {
                return;
            }

            RegisteredEvents.Add(bloodLustEvent);
        }

        public static void TriggerRandomEvent(VampireComponent vampire, bool ignoreConditions = false)
        {
            if (RegisteredEvents.Count == 0 || vampire == null)
            {
                return;
            }

            List<BloodLustEventBase> possibleEvents = ignoreConditions
                ? new List<BloodLustEventBase>(RegisteredEvents)
                : RegisteredEvents.FindAll(e => e.CanTrigger(vampire));

            if (possibleEvents.Count == 0)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "[Debug] No Blood Lust events are available to trigger right now.", Colors.Red));
                return;
            }

            BloodLustEventBase selectedEvent = possibleEvents[Random.Next(possibleEvents.Count)];
            selectedEvent.Trigger(ignoreConditions);
        }

        public static void TriggerSpecificEvent(string eventId, VampireComponent vampire, bool ignoreConditions = false)
        {
            BloodLustEventBase? selectedEvent = RegisteredEvents.Find(e => e.EventId == eventId);
            if (selectedEvent == null)
            {
                return;
            }

            if (!ignoreConditions && !selectedEvent.CanTrigger(vampire))
            {
                return;
            }

            selectedEvent.Trigger(ignoreConditions);
        }
    }
}