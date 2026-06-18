using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using LeaveType = TaleWorlds.CampaignSystem.GameMenus.GameMenuOption.LeaveType;
using MenuOverlayType = TaleWorlds.CampaignSystem.GameMenus.GameMenu.MenuOverlayType;

namespace VampireOverhaul
{
    public class PrisonerFeedingBehavior : CampaignBehaviorBase
    {
        private const string VampireHubMenuId = "vampire_hub_menu";
        private const string TroopFeedMenuId = "vampire_feed_troop_prisoners";

        // Primary settlement menus (see docs.bannerlordmodding.lt/gauntletui/menus):
        // town, castle, village — NOT town_center / town_outside.
        private static readonly string[] VampireHubParentMenus =
        {
            "town",
            "castle",
            "village",
            "town_keep",
            "tavern",
            "arena",
            "encounter",
            "army_wait",
            "settlement_wait",
            "hideout_wait",
            "town_enter",
            "town_outside",
            "town_center",
            "town_streets",
            "town_wait",
            "town_wait_menus",
            "castle_outside",
            "castle_enter",
            "village_center",
            "village_outside",
            "village_wait_menus",
        };

        private readonly List<CharacterObject> _troopPrisonerSelection = new();
        private int _selectedTroopPrisonerIndex;
        private string _returnMenuId = "village";

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            AddHeroPrisonerDialogues(starter);
            AddVampireMenus(starter);
        }

        private void AddHeroPrisonerDialogues(CampaignGameStarter starter)
        {
            starter.AddDialogLine(
                "vampire_feeding_start",
                "prisoner_recruit_start_player",
                "vampire_feeding_options",
                "You seem... hungry.",
                CanFeedHeroPrisoner,
                null,
                120,
                null);

            starter.AddPlayerLine(
                "vampire_feed_once",
                "vampire_feeding_options",
                "prisoner_recruit_start_player",
                "I only need a taste. You'll live.",
                CanFeedHeroPrisoner,
                () => FeedHeroPrisoner(false),
                100,
                null,
                null);

            starter.AddPlayerLine(
                "vampire_feed_kill",
                "vampire_feeding_options",
                "prisoner_recruit_start_player",
                "I'm going to drain you dry.",
                CanFeedHeroPrisoner,
                () => FeedHeroPrisoner(true),
                99,
                null,
                null);

            starter.AddPlayerLine(
                "vampire_feed_once_direct",
                "prisoner_recruit_start_player",
                "prisoner_recruit_start_player",
                "I only need a taste. You'll live.",
                CanFeedHeroPrisoner,
                () => FeedHeroPrisoner(false),
                95,
                null,
                null);

            starter.AddPlayerLine(
                "vampire_feed_kill_direct",
                "prisoner_recruit_start_player",
                "prisoner_recruit_start_player",
                "I'm going to drain you dry.",
                CanFeedHeroPrisoner,
                () => FeedHeroPrisoner(true),
                94,
                null,
                null);
        }

        private void AddVampireMenus(CampaignGameStarter starter)
        {
            starter.AddGameMenu(
                VampireHubMenuId,
                "{=!}{VAMPIRE_HUB_STATUS}",
                InitVampireHubMenu,
                MenuOverlayType.SettlementWithBoth);

            starter.AddGameMenuOption(
                VampireHubMenuId,
                "vampire_hub_feed_troops",
                "Feed on troop prisoners",
                VampireHubFeedTroopsCondition,
                _ => OpenTroopFeedMenu());

            starter.AddGameMenuOption(
                VampireHubMenuId,
                "vampire_hub_back",
                "Return",
                VampireHubBackCondition,
                _ => ReturnFromVampireHub());

            starter.AddGameMenu(
                TroopFeedMenuId,
                "{=!}{VAMPIRE_TROOP_FEED_STATUS}",
                InitTroopFeedMenu,
                MenuOverlayType.Encounter);

            starter.AddGameMenuOption(
                TroopFeedMenuId,
                "vampire_feed_troop_taste",
                "Take a taste (prisoner survives)",
                TroopFeedTasteCondition,
                _ => FeedSelectedTroopPrisoner(false));

            starter.AddGameMenuOption(
                TroopFeedMenuId,
                "vampire_feed_troop_kill",
                "Drain them dry",
                TroopFeedKillCondition,
                _ => FeedSelectedTroopPrisoner(true));

            starter.AddGameMenuOption(
                TroopFeedMenuId,
                "vampire_feed_troop_next",
                "Next prisoner",
                TroopFeedNextCondition,
                _ => SelectNextTroopPrisoner());

            starter.AddGameMenuOption(
                TroopFeedMenuId,
                "vampire_feed_troop_back",
                "Back to vampire menu",
                TroopFeedBackCondition,
                _ => GameMenu.SwitchToMenu(VampireHubMenuId));

            foreach (string parentMenuId in VampireHubParentMenus)
            {
                starter.AddGameMenuOption(
                    parentMenuId,
                    $"vampire_open_hub_{parentMenuId}",
                    "Vampire",
                    VampireHubEntryCondition,
                    _ => OpenVampireHubMenu(),
                    false,
                    2);
            }
        }

        private bool VampireHubEntryCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = LeaveType.Submenu;
            return PrisonerFeedingActions.IsVampireMechanicsActive();
        }

        private bool VampireHubFeedTroopsCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = LeaveType.Conversation;
            return HasTroopPrisoners();
        }

        private bool VampireHubBackCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = LeaveType.Leave;
            return true;
        }

        private void InitVampireHubMenu(MenuCallbackArgs args)
        {
            VampireComponent? vampire = GetVampireComponent();
            Settings? settings = Settings.Instance;

            if (vampire == null || settings == null)
            {
                MBTextManager.SetTextVariable("VAMPIRE_HUB_STATUS", "The darkness stirs within you.", false);
                return;
            }

            string prisonerNote = HasTroopPrisoners()
                ? "Troop prisoners are available to feed on."
                : "No troop prisoners in your party.";

            MBTextManager.SetTextVariable(
                "VAMPIRE_HUB_STATUS",
                $"Blood Lust: {vampire.CurrentBloodLust:F0} / {settings.MaxBloodLust:F0}. {prisonerNote}",
                false);
        }

        private void OpenVampireHubMenu()
        {
            string? currentMenuId = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId;
            if (!string.IsNullOrEmpty(currentMenuId))
            {
                _returnMenuId = NormalizeReturnMenuId(currentMenuId);
            }

            GameMenu.SwitchToMenu(VampireHubMenuId);
        }

        private void ReturnFromVampireHub()
        {
            if (Campaign.Current?.GameMenuManager.GetGameMenu(_returnMenuId) != null)
            {
                GameMenu.SwitchToMenu(_returnMenuId);
                return;
            }

            GameMenu.ExitToLast();
        }

        private static string NormalizeReturnMenuId(string menuId)
        {
            switch (menuId)
            {
                case "village_wait_menus":
                case "village_center":
                case "village_outside":
                    return "village";
                case "town_wait_menus":
                case "town_wait":
                case "town_center":
                case "town_outside":
                case "town_enter":
                case "town_streets":
                case "town_keep":
                case "tavern":
                case "arena":
                    return "town";
                case "castle_outside":
                case "castle_enter":
                    return "castle";
                default:
                    return menuId;
            }
        }

        private bool CanFeedHeroPrisoner()
        {
            if (!PrisonerFeedingActions.IsVampireMechanicsActive()) return false;

            Hero hero = Hero.OneToOneConversationHero;
            if (hero == null || !hero.IsPrisoner) return false;

            PartyBase? playerParty = Hero.MainHero.PartyBelongedTo?.Party;
            return playerParty != null && hero.PartyBelongedToAsPrisoner == playerParty;
        }

        private bool TroopFeedTasteCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = LeaveType.Conversation;
            return HasSelectedTroopPrisoner();
        }

        private bool TroopFeedKillCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = LeaveType.HostileAction;
            return HasSelectedTroopPrisoner();
        }

        private bool TroopFeedNextCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = LeaveType.Continue;
            return _troopPrisonerSelection.Count > 1;
        }

        private bool TroopFeedBackCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = LeaveType.Leave;
            return true;
        }

        private void InitTroopFeedMenu(MenuCallbackArgs args)
        {
            RefreshTroopPrisonerSelection();
            if (_troopPrisonerSelection.Count == 0)
            {
                MBTextManager.SetTextVariable("VAMPIRE_TROOP_FEED_STATUS", "You have no troop prisoners to feed on.", false);
                return;
            }

            CharacterObject prisoner = _troopPrisonerSelection[_selectedTroopPrisonerIndex];
            int count = GetTroopPrisonerCount(prisoner);
            MBTextManager.SetTextVariable(
                "VAMPIRE_TROOP_FEED_STATUS",
                $"Prisoner: {prisoner.Name} ({count} captive{(count == 1 ? string.Empty : "s")})",
                false);
        }

        private void OpenTroopFeedMenu()
        {
            RefreshTroopPrisonerSelection();
            if (_troopPrisonerSelection.Count == 0) return;

            _selectedTroopPrisonerIndex = 0;
            GameMenu.SwitchToMenu(TroopFeedMenuId);
        }

        private void SelectNextTroopPrisoner()
        {
            if (_troopPrisonerSelection.Count == 0) return;

            _selectedTroopPrisonerIndex = (_selectedTroopPrisonerIndex + 1) % _troopPrisonerSelection.Count;
            GameMenu.SwitchToMenu(TroopFeedMenuId);
        }

        private void FeedHeroPrisoner(bool kill)
        {
            Hero hero = Hero.OneToOneConversationHero;
            if (hero == null || !CanFeedHeroPrisoner()) return;

            PrisonerFeedingActions.ApplyBloodLustReduction(kill);

            if (kill)
            {
                KillCharacterAction.ApplyByMurder(Hero.MainHero, hero, true);
                InformationManager.DisplayMessage(new InformationMessage(
                    "You drain the prisoner completely. Their body goes limp.", Colors.Red));
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "You take just enough to satisfy your hunger. The prisoner survives.", Colors.Green));
            }
        }

        private void FeedSelectedTroopPrisoner(bool kill)
        {
            if (!HasSelectedTroopPrisoner()) return;

            CharacterObject prisoner = _troopPrisonerSelection[_selectedTroopPrisonerIndex];
            MobileParty? playerParty = MobileParty.MainParty;
            if (playerParty == null || GetTroopPrisonerCount(prisoner) <= 0) return;

            PrisonerFeedingActions.FeedTroopPrisonerFromRoster(playerParty.Party.PrisonRoster, prisoner, kill);

            RefreshTroopPrisonerSelection();
            if (_troopPrisonerSelection.Count == 0)
            {
                GameMenu.SwitchToMenu(VampireHubMenuId);
                return;
            }

            if (_selectedTroopPrisonerIndex >= _troopPrisonerSelection.Count)
            {
                _selectedTroopPrisonerIndex = 0;
            }

            GameMenu.SwitchToMenu(TroopFeedMenuId);
        }

        private VampireComponent? GetVampireComponent()
        {
            return Campaign.Current?.GetCampaignBehavior<VampireComponent>();
        }

        private bool HasTroopPrisoners()
        {
            TroopRoster? roster = MobileParty.MainParty?.Party?.PrisonRoster;
            if (roster == null) return false;

            foreach (TroopRosterElement element in roster.GetTroopRoster())
            {
                if (element.Character != null && !element.Character.IsHero && element.Number > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasSelectedTroopPrisoner()
        {
            return _troopPrisonerSelection.Count > 0
                && _selectedTroopPrisonerIndex >= 0
                && _selectedTroopPrisonerIndex < _troopPrisonerSelection.Count;
        }

        private void RefreshTroopPrisonerSelection()
        {
            _troopPrisonerSelection.Clear();

            TroopRoster? roster = MobileParty.MainParty?.Party?.PrisonRoster;
            if (roster == null) return;

            foreach (TroopRosterElement element in roster.GetTroopRoster())
            {
                if (element.Character != null && !element.Character.IsHero && element.Number > 0)
                {
                    _troopPrisonerSelection.Add(element.Character);
                }
            }
        }

        private int GetTroopPrisonerCount(CharacterObject prisoner)
        {
            return PrisonerFeedingActions.GetTroopPrisonerCount(prisoner);
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}