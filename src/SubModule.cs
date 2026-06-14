using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace VampireOverhaul
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            base.OnGameStart(game, gameStarter);

            InformationManager.DisplayMessage(new InformationMessage(
                "VampireOverhaul v0.1.0 loaded successfully!", 
                Colors.Green));
        }
    }
}