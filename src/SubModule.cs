using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace VampireOverhaul
{
    public class SubModule : MBSubModuleBase
    {
        private const string ModVersion = "v0.3.6";
        private static readonly Harmony HarmonyInstance = new Harmony("com.vampireoverhaul");

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            try
            {
                HarmonyInstance.PatchAll(typeof(SubModule).Assembly);
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "VampireOverhaul: Harmony patching failed. " + ex.Message, Colors.Red));
            }

            // Register MCM settings so they persist after closing the game
            try
            {
                Settings settings = Settings.Instance;
                if (settings != null)
                {
                    settings.RegisterSettings();
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "VampireOverhaul: Failed to register MCM settings. " + ex.Message, Colors.Red));
            }
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            base.OnGameStart(game, gameStarter);

            if (gameStarter is CampaignGameStarter campaignStarter)
            {
                campaignStarter.AddBehavior(new VampireComponent());
                campaignStarter.AddBehavior(new PrisonerFeedingBehavior());
            }
        }

        public override void OnGameLoaded(Game game, object initializerObject)
        {
            base.OnGameLoaded(game, initializerObject);

            // Show version only once when loading/continuing
            InformationManager.DisplayMessage(new InformationMessage(
                $"VampireOverhaul {ModVersion} loaded successfully.", Colors.Green));
        }
    }
}