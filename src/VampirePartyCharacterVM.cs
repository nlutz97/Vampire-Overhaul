using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace VampireOverhaul
{
    public class VampirePartyCharacterVM : PartyCharacterVM
    {
        private bool _isVampireFeedPrisonerVisible;
        private bool _isVampireFeedPrisonerEnabled;

        public VampirePartyCharacterVM(
            PartyScreenLogic partyScreenLogic,
            PartyVM partyVm,
            TroopRoster troops,
            int index,
            PartyScreenLogic.TroopType type,
            PartyScreenLogic.PartyRosterSide side,
            bool isTroopTransferrable)
            : base(partyScreenLogic, partyVm, troops, index, type, side, isTroopTransferrable)
        {
            VampireFeedPrisonerHint = new BasicTooltipViewModel(GetVampireFeedHint);
            UpdateVampireFeedState();
        }

        [DataSourceProperty]
        public bool IsVampireFeedPrisonerVisible
        {
            get => _isVampireFeedPrisonerVisible;
            set
            {
                if (value != _isVampireFeedPrisonerVisible)
                {
                    _isVampireFeedPrisonerVisible = value;
                    OnPropertyChangedWithValue(value, nameof(IsVampireFeedPrisonerVisible));
                }
            }
        }

        [DataSourceProperty]
        public bool IsVampireFeedPrisonerEnabled
        {
            get => _isVampireFeedPrisonerEnabled;
            set
            {
                if (value != _isVampireFeedPrisonerEnabled)
                {
                    _isVampireFeedPrisonerEnabled = value;
                    OnPropertyChangedWithValue(value, nameof(IsVampireFeedPrisonerEnabled));
                }
            }
        }

        [DataSourceProperty]
        public BasicTooltipViewModel VampireFeedPrisonerHint { get; private set; }

        public void ExecuteVampireFeedPrisoner()
        {
            if (!IsVampireFeedPrisonerEnabled)
            {
                return;
            }

            CharacterObject prisoner = Character;
            PrisonerFeedingActions.ShowTroopFeedInquiry(prisoner, kill => FeedTroopPrisoner(kill));
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            UpdateVampireFeedState();
        }

        private void FeedTroopPrisoner(bool kill)
        {
            TroopRoster roster = _partyScreenLogic.PrisonerRosters[(uint)Side];
            CharacterObject prisoner = Character;

            PrisonerFeedingActions.ApplyBloodLustReduction(kill);

            if (kill)
            {
                roster.AddToCounts(prisoner, -1);
                PartyScreenFeedSync.SyncInitialPrisonerCount(_partyScreenLogic, Side, prisoner, -1);

                InformationManager.DisplayMessage(new InformationMessage(
                    $"You drain a {prisoner.Name} completely. Their body goes limp.", Colors.Red));

                int index = roster.FindIndexOfTroop(prisoner);
                if (index < 0)
                {
                    RemoveSelfFromPrisonerList();
                }
                else
                {
                    Troop = roster.GetElementCopyAtIndex(index);
                    ThrowOnPropertyChanged();
                }
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"You take just enough from a {prisoner.Name} to satisfy your hunger. They survive.", Colors.Green));
            }

            UpdateVampireFeedState();
        }

        private void RemoveSelfFromPrisonerList()
        {
            MBBindingList<PartyCharacterVM> prisonerList = Side == PartyScreenLogic.PartyRosterSide.Right
                ? _partyVm.MainPartyPrisoners
                : _partyVm.OtherPartyPrisoners;

            if (prisonerList.Contains(this))
            {
                prisonerList.Remove(this);
            }
        }

        private void UpdateVampireFeedState()
        {
            bool canFeed = IsPrisonerOfPlayer
                && !IsHero
                && PrisonerFeedingActions.CanFeedTroopPrisoner(Character);

            IsVampireFeedPrisonerVisible = canFeed;
            IsVampireFeedPrisonerEnabled = canFeed && Troop.Number > 0;
        }

        private string GetVampireFeedHint()
        {
            if (!PrisonerFeedingActions.IsVampireMechanicsActive())
            {
                return "Vampire mechanics are disabled.";
            }

            if (!PrisonerFeedingActions.IsInWilderness())
            {
                return "You can only feed on prisoners while traveling in the wilderness.";
            }

            return "Feed on this prisoner. Take a taste or drain them dry.";
        }
    }
}