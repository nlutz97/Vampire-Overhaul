using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;

namespace VampireOverhaul
{
    [HarmonyPatch]
    internal static class VampirePartyCharacterVMPatches
    {
        private static readonly ConstructorInfo VanillaCtor = AccessTools.Constructor(
            typeof(PartyCharacterVM),
            new[]
            {
                typeof(PartyScreenLogic),
                typeof(PartyVM),
                typeof(TroopRoster),
                typeof(int),
                typeof(PartyScreenLogic.TroopType),
                typeof(PartyScreenLogic.PartyRosterSide),
                typeof(bool),
            });

        private static readonly ConstructorInfo VampireCtor = AccessTools.Constructor(
            typeof(VampirePartyCharacterVM),
            new[]
            {
                typeof(PartyScreenLogic),
                typeof(PartyVM),
                typeof(TroopRoster),
                typeof(int),
                typeof(PartyScreenLogic.TroopType),
                typeof(PartyScreenLogic.PartyRosterSide),
                typeof(bool),
            });

        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(PartyVM), "InitializePartyList");
            yield return AccessTools.Method(typeof(PartyVM), "TransferTroop");
            yield return AccessTools.Method(typeof(PartyVM), "UpgradeTroop");
            yield return AccessTools.Method(typeof(PartyVM), "RecruitTroop");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Newobj
                    && instruction.operand is ConstructorInfo ctor
                    && ctor == VanillaCtor)
                {
                    yield return new CodeInstruction(OpCodes.Newobj, VampireCtor);
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }
}