using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace VampireOverhaul
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            // Register MCM Settings
            if (Settings.Instance != null)
            {
                // MCM is initialized
            }
        }

        // OnGameLoaded registration moved to OnGameStart — Campaign.Current is not ready here.
        // public override void OnGameLoaded(Game game, object initializerObject)
        // {
        //     base.OnGameLoaded(game, initializerObject);
        //     Campaign.Current?.CampaignBehaviorManager?.AddBehavior(new VampireComponent());
        // }

        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            base.OnGameStart(game, gameStarter);

            if (gameStarter is CampaignGameStarter campaignStarter)
            {
                campaignStarter.AddBehavior(new VampireComponent());
            }
        }
    }
}