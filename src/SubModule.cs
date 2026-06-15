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

        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            base.OnGameStart(game, gameStarter);

            InformationManager.DisplayMessage(new InformationMessage(
                "VampireOverhaul v0.2.0 loaded successfully!", 
                Colors.Green));
        }
    }
}