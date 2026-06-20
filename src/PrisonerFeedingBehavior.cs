using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Library;

namespace VampireOverhaul
{
    public class PrisonerFeedingBehavior : CampaignBehaviorBase
    {
        private bool _isFeedingToDeath = false;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, AddDialogues);
        }

        private void AddDialogues(CampaignGameStarter starter)
        {
            // Existing feeding start line
            starter.AddDialogLine(
                "vampire_feeding_start",
                "lord_prisoner_start",
                "vampire_feeding_options",
                "You seem... hungry.",
                IsValidPrisoner,
                null,
                120,
                null);

            // Feed & Release
            starter.AddPlayerLine(
                "vampire_feed_once",
                "vampire_feeding_options",
                "vampire_feed_tone",
                "I only need a taste. You'll live.",
                IsValidPrisoner,
                () => StartFeeding(false),
                100,
                null,
                null);

            // Feed until Dead
            starter.AddPlayerLine(
                "vampire_feed_kill",
                "vampire_feeding_options",
                "vampire_feed_tone",
                "I'm going to drain you dry.",
                IsValidPrisoner,
                () => StartFeeding(true),
                99,
                null,
                null);

            // Tone choices after selecting feeding type
            starter.AddDialogLine(
                "vampire_feed_tone_start",
                "vampire_feed_tone",
                "vampire_feed_tone_options",
                "How do you approach this?",
                null,
                null,
                100,
                null);

            starter.AddPlayerLine(
                "tone_thirsty",
                "vampire_feed_tone_options",
                "lord_prisoner_start",
                "Just thirsty. Nothing more.",
                null,
                () => FinishFeeding("thirsty"),
                100,
                null,
                null);

            starter.AddPlayerLine(
                "tone_remorseful",
                "vampire_feed_tone_options",
                "lord_prisoner_start",
                "I... I have to do this.",
                null,
                () => FinishFeeding("remorseful"),
                99,
                null,
                null);

            starter.AddPlayerLine(
                "tone_predatory",
                "vampire_feed_tone_options",
                "lord_prisoner_start",
                "Your blood is mine.",
                null,
                () => FinishFeeding("predatory"),
                98,
                null,
                null);

            starter.AddPlayerLine(
                "tone_sadistic",
                "vampire_feed_tone_options",
                "lord_prisoner_start",
                "Scream for me.",
                null,
                () => FinishFeeding("sadistic"),
                97,
                null,
                null);
        }

        private bool IsValidPrisoner()
        {
            Settings? settings = Settings.Instance;
            if (settings == null || !settings.EnableVampireMechanics) return false;

            Hero hero = Hero.OneToOneConversationHero;
            if (hero == null || !hero.IsPrisoner) return false;

            PartyBase? playerParty = Hero.MainHero.PartyBelongedTo?.Party;
            return playerParty != null && hero.PartyBelongedToAsPrisoner == playerParty;
        }

        private void StartFeeding(bool kill)
        {
            _isFeedingToDeath = kill;
        }

        private void FinishFeeding(string tone)
        {
            VampireComponent? vampire = Campaign.Current?.GetCampaignBehavior<VampireComponent>();
            if (vampire == null) return;

            Settings? settings = Settings.Instance;
            if (settings == null) return;

            float reduction = _isFeedingToDeath ? 75f : 25f;
            vampire.CurrentBloodLust = MathF.Max(0f, vampire.CurrentBloodLust - reduction);

            Hero hero = Hero.OneToOneConversationHero;
            if (hero != null && IsValidPrisoner())
            {
                if (_isFeedingToDeath)
                {
                    KillCharacterAction.ApplyByMurder(Hero.MainHero, hero, true);
                    PrisonerFeedingActions.ApplyMoralePenalty();
                }
            }

            string message = _isFeedingToDeath
                ? "You drain the prisoner completely."
                : "You take just enough to satisfy your hunger.";

            InformationManager.DisplayMessage(new InformationMessage(message, _isFeedingToDeath ? Colors.Red : Colors.Green));
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}