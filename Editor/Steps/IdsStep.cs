using Larje.Core.Audio;
using Larje.Core.Economy;
using Larje.Core.GameFlow;
using Larje.Core.UI;
using Larje.ProjectSetup.Editor.Utility;

namespace Larje.ProjectSetup.Editor.Steps
{
    public sealed class IdsStep : IProjectSetupStep
    {
        public const string MAIN_MENU_SCREEN = "MainMenu";
        public const string COINS = "Coins";
        public const string DEFAULT_PLACEMENT = "Default";

        public static readonly string[] GameStates = { "Splash", "Loading", "Transition", "Menu", "Playing", "Paused", "Win", "Fail" };
        public static readonly string[] SoundChannels = { "Music", "Sfx" };

        public string Name => "Ids";

        public void Run(ProjectSetupContext context)
        {
            foreach (string state in GameStates)
            {
                SetupAssets.LoadOrCreateId<GameStateId>(state);
            }

            foreach (string channel in SoundChannels)
            {
                SetupAssets.LoadOrCreateId<SoundChannelId>(channel);
            }

            SetupAssets.LoadOrCreateId<UIScreenId>(MAIN_MENU_SCREEN);
            SetupAssets.LoadOrCreateId<CurrencyId>(COINS);
            SetupAssets.LoadOrCreateId<CurrencyPlacementId>(DEFAULT_PLACEMENT);

            context.Report.Info(Name, "Game states, sound channels, main menu screen and currency ids are ready in " + ProjectSetupContext.IDS);
        }
    }
}
