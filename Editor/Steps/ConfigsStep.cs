using System.Linq;
using Larje.Core.Audio;
using Larje.Core.Debugging;
using Larje.Core.Economy;
using Larje.Core.GameFlow;
using Larje.Core.Monetization;
using Larje.Core.Saves;
using Larje.Core.TimeScale;
using Larje.ProjectSetup.Editor.Utility;
using UnityEditor;
using UnityEngine;

namespace Larje.ProjectSetup.Editor.Steps
{
    public sealed class ConfigsStep : IProjectSetupStep
    {
        public string Name => "Configs";

        public void Run(ProjectSetupContext context)
        {
            CreateConfig<SaveConfig>();
            CreateConfig<SceneLoaderConfig>();
            CreateConfig<TimeScaleConfig>();
            CreateConfig<ItemsConfig>();
            CreateConfig<PurchasesConfig>();
            CreateConfig<DebugConsoleConfig>();

            SetupGameStates();
            SetupAppFlow();
            SetupSound();
            SetupCurrency();

            context.Report.Info(Name, "Core configs are ready in " + ProjectSetupContext.CONFIGS);
        }

        public static string GetPath<T>() where T : ScriptableObject
        {
            return $"{ProjectSetupContext.CONFIGS}/{typeof(T).Name}.asset";
        }

        private T CreateConfig<T>() where T : ScriptableObject
        {
            return SetupAssets.LoadOrCreate<T>(GetPath<T>());
        }

        private void SetupGameStates()
        {
            GameStatesConfig config = CreateConfig<GameStatesConfig>();
            foreach (string state in IdsStep.GameStates)
            {
                string field = char.ToLowerInvariant(state[0]) + state.Substring(1);
                SetupAssets.SetReference(config, field, SetupAssets.LoadOrCreateId<GameStateId>(state));
            }
        }

        private void SetupAppFlow()
        {
            AppFlowConfig config = CreateConfig<AppFlowConfig>();
            SetupAssets.SetString(config, "splashScene", ProjectSetupContext.SPLASH_SCENE);
            SetupAssets.SetString(config, "menuScene", ProjectSetupContext.MAIN_SCENE);
            SetupAssets.SetFloat(config, "splashDuration", 1.5f);
        }

        private void SetupSound()
        {
            SoundConfig config = CreateConfig<SoundConfig>();
            SoundChannelId[] channels = IdsStep.SoundChannels.Select(x => SetupAssets.LoadOrCreateId<SoundChannelId>(x)).ToArray();
            SetupAssets.SetReferences(config, "channels", channels);
            SetupAssets.SetReference(config, "defaultChannel", channels.Last());
        }

        private void SetupCurrency()
        {
            CurrencyConfig config = CreateConfig<CurrencyConfig>();
            CurrencyPlacementId placement = SetupAssets.LoadOrCreateId<CurrencyPlacementId>(IdsStep.DEFAULT_PLACEMENT);
            SetupAssets.SetReference(config, "defaultPlacement", placement);
            SetupAssets.SetReferences(config, "placements", new Object[] { placement });

            SerializedObject serialized = new SerializedObject(config);
            SerializedProperty currencies = SetupAssets.GetProperty(serialized, "currencies");
            if (currencies.arraySize == 0)
            {
                currencies.arraySize = 1;
                SerializedProperty coins = currencies.GetArrayElementAtIndex(0);
                coins.FindPropertyRelative("currency").objectReferenceValue = SetupAssets.LoadOrCreateId<CurrencyId>(IdsStep.COINS);
                coins.FindPropertyRelative("maxAmount").intValue = 999999;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
