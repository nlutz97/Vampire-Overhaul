using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace VampireOverhaul
{
    public static class PrisonerFeedingActions
    {
        public const float TasteBloodLustReduction = 25f;
        public const float DrainBloodLustReduction = 75f;

        public static bool IsInWilderness()
        {
            return MobileParty.MainParty?.CurrentSettlement == null;
        }

        public static bool IsVampireMechanicsActive()
        {
            Settings? settings = Settings.Instance;
            if (settings == null || !settings.EnableVampireMechanics)
            {
                return false;
            }

            VampireComponent? vampire = Campaign.Current?.GetCampaignBehavior<VampireComponent>();
            if (vampire == null)
            {
                return false;
            }

            if (!vampire.IsVampire)
            {
                vampire.IsVampire = true;
                vampire.CurrentBloodLust = 0f;
            }

            return true;
        }

        public static bool CanFeedTroopPrisoner(CharacterObject? character)
        {
            if (!IsVampireMechanicsActive() || !IsInWilderness() || character == null || character.IsHero)
            {
                return false;
            }

            return GetTroopPrisonerCount(character) > 0;
        }

        public static int GetTroopPrisonerCount(CharacterObject prisoner)
        {
            return MobileParty.MainParty?.Party?.PrisonRoster?.GetTroopCount(prisoner) ?? 0;
        }

        public static void ApplyBloodLustReduction(bool kill)
        {
            VampireComponent? vampire = Campaign.Current?.GetCampaignBehavior<VampireComponent>();
            if (vampire == null)
            {
                return;
            }

            float reduction = kill ? DrainBloodLustReduction : TasteBloodLustReduction;
            vampire.CurrentBloodLust = MathF.Max(0f, vampire.CurrentBloodLust - reduction);
        }

        public static float GetBloodLustReduction(bool kill)
        {
            return kill ? DrainBloodLustReduction : TasteBloodLustReduction;
        }

        public static void ShowTroopFeedInquiry(CharacterObject prisoner, Action<bool> onSelected)
        {
            string prisonerName = prisoner.Name?.ToString() ?? "prisoner";
            InquiryData inquiry = new InquiryData(
                "Feed on Prisoner",
                $"What do you want to do with the {prisonerName}?",
                true,
                true,
                "Take a taste",
                "Drain them dry",
                () => onSelected(false),
                () => onSelected(true));

            InformationManager.ShowInquiry(inquiry, false, false);
        }

        public static void FeedTroopPrisonerFromRoster(TroopRoster roster, CharacterObject prisoner, bool kill)
        {
            ApplyBloodLustReduction(kill);

            if (kill)
            {
                roster.RemoveTroop(prisoner, 1, default, 0);
                ApplyMoralePenalty();
                InformationManager.DisplayMessage(new InformationMessage(
                    $"You drain a {prisoner.Name} completely. Their body goes limp.", Colors.Red));
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"You take just enough from a {prisoner.Name} to satisfy your hunger. They survive.", Colors.Green));
            }
        }

        public static void ApplyMoralePenalty()
        {
            MobileParty? party = MobileParty.MainParty;
            if (party == null)
            {
                return;
            }

            int totalTroops = party.MemberRoster.TotalManCount;
            if (totalTroops <= 1)
            {
                return;
            }

            int witnessCount = 0;

            foreach (TroopRosterElement element in party.MemberRoster.GetTroopRoster())
            {
                if (element.Character == null || element.Number <= 0)
                {
                    continue;
                }

                if (element.Character.IsHero && element.Character.HeroObject == Hero.MainHero)
                {
                    continue;
                }

                witnessCount += element.Number;
            }

            if (witnessCount == 0)
            {
                return;
            }

            float witnessPercentage = (float)witnessCount / totalTroops;
            float finalPenalty = 8f * witnessPercentage;

            party.RecentEventsMorale -= finalPenalty;

            InformationManager.DisplayMessage(new InformationMessage(
                $"Your party is unsettled by what they witnessed. Morale decreased by {finalPenalty:F1}.",
                Colors.Red));
        }
    }
}