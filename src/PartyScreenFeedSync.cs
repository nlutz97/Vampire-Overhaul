using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;

namespace VampireOverhaul
{
    internal static class PartyScreenFeedSync
    {
        private static readonly System.Reflection.FieldInfo InitialDataField =
            AccessTools.Field(typeof(PartyScreenLogic), "_initialData");

        public static void SyncInitialPrisonerCount(
            PartyScreenLogic partyScreenLogic,
            PartyScreenLogic.PartyRosterSide side,
            CharacterObject prisoner,
            int delta)
        {
            if (partyScreenLogic == null || prisoner == null || delta == 0)
            {
                return;
            }

            PartyScreenData? initialData = InitialDataField.GetValue(partyScreenLogic) as PartyScreenData;
            if (initialData == null)
            {
                return;
            }

            TroopRoster initialRoster = side == PartyScreenLogic.PartyRosterSide.Right
                ? initialData.RightPrisonerRoster
                : initialData.LeftPrisonerRoster;

            initialRoster.AddToCounts(prisoner, delta);
        }
    }
}