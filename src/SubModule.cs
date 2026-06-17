using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace VampireOverhaul
{
    public class SubModule : MBSubModuleBase
    {
        private const string ModVersion = "v0.3.3";

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

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