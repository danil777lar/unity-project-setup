using System;
using System.Collections.Generic;
using Larje.Core.Audio;
using Larje.Core.Debugging;
using Larje.Core.Economy;
using Larje.Core.GameFlow;
using Larje.Core.Input;
using Larje.Core.Monetization;
using Larje.Core.Saves;
using Larje.Core.TimeScale;
using Larje.Core.UI;
using Larje.Core.Zenject;
using Larje.ProjectSetup.Editor.Utility;
using UnityEditor;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace Larje.ProjectSetup.Editor.Steps
{
    public sealed class ContextStep : IProjectSetupStep
    {
        public const string CONTEXT_PATH = ProjectSetupContext.RESOURCES + "/ProjectContext.prefab";

        private const string CHARACTER_INSTALLER_TYPE = "Larje.Character.Zenject.LarjeCharacterInstaller, Larje.Character.Zenject";
        private const string CURSOR_CONFIG_TYPE = "Larje.Character.CursorConfig, Larje.Character";

        public string Name => "Project Context";

        public void Run(ProjectSetupContext context)
        {
            List<Object> installers = new List<Object> { CreateCoreInstaller() };

            if (context.Options.Character)
            {
                ScriptableObject character = CreateCharacterInstaller(context);
                if (character != null)
                {
                    installers.Add(character);
                }
            }

            ScriptableObject project = CreateProjectInstaller(context);
            if (project != null)
            {
                installers.Add(project);
            }

            WriteProjectContext(installers);
            context.Report.Info(Name, $"{CONTEXT_PATH} uses {installers.Count} installer(s)");
        }

        private LarjeCoreInstaller CreateCoreInstaller()
        {
            LarjeCoreInstaller installer = SetupAssets.LoadOrCreate<LarjeCoreInstaller>(ProjectSetupContext.INSTALLERS + "/LarjeCoreInstaller.asset");
            SetupAssets.SetReference(installer, "saves", Load<SaveConfig>());
            SetupAssets.SetReference(installer, "gameStates", Load<GameStatesConfig>());
            SetupAssets.SetReference(installer, "sceneLoader", Load<SceneLoaderConfig>());
            SetupAssets.SetReference(installer, "appFlow", Load<AppFlowConfig>());
            SetupAssets.SetReference(installer, "timeScale", Load<TimeScaleConfig>());
            SetupAssets.SetReference(installer, "input", AssetDatabase.LoadAssetAtPath<InputConfig>(InputStep.INPUT_CONFIG_PATH));
            SetupAssets.SetReference(installer, "pointerControl", AssetDatabase.LoadAssetAtPath<PointerControlConfig>(InputStep.POINTER_CONFIG_PATH));
            SetupAssets.SetReference(installer, "ui", AssetDatabase.LoadAssetAtPath<UIConfig>(UIStep.UI_CONFIG_PATH));
            SetupAssets.SetReference(installer, "sound", Load<SoundConfig>());
            SetupAssets.SetReference(installer, "currency", Load<CurrencyConfig>());
            SetupAssets.SetReference(installer, "items", Load<ItemsConfig>());
            SetupAssets.SetReference(installer, "purchases", Load<PurchasesConfig>());
            SetupAssets.SetReference(installer, "debugConsole", Load<DebugConsoleConfig>());
            SetupAssets.SetBool(installer, "bindSimulatedMonetization", true);
            return installer;
        }

        private ScriptableObject CreateCharacterInstaller(ProjectSetupContext context)
        {
            Type installerType = Type.GetType(CHARACTER_INSTALLER_TYPE);
            Type cursorType = Type.GetType(CURSOR_CONFIG_TYPE);
            if (installerType == null || cursorType == null)
            {
                context.Report.Error(Name, "Character module requested but com.larje.character with Zenject support is not compiled in this project");
                return null;
            }

            ScriptableObject cursor = SetupAssets.LoadOrCreate(cursorType, ProjectSetupContext.CONFIGS + "/CursorConfig.asset");
            ScriptableObject installer = SetupAssets.LoadOrCreate(installerType, ProjectSetupContext.INSTALLERS + "/LarjeCharacterInstaller.asset");
            SetupAssets.SetReference(installer, "cursor", cursor);
            return installer;
        }

        private ScriptableObject CreateProjectInstaller(ProjectSetupContext context)
        {
            string namespaceName = context.Options.RootNamespace;
            Type installerType = Type.GetType($"{namespaceName}.ProjectInstaller, {namespaceName}");
            if (installerType == null)
            {
                context.Report.Pending(Name, "Project code is not compiled yet. Run setup again after compilation to add ProjectInstaller");
                return null;
            }

            ScriptableObject installer = SetupAssets.LoadOrCreate(installerType, ProjectSetupContext.INSTALLERS + "/ProjectInstaller.asset");
            SetupAssets.SetReference(installer, "_mainMenuScreen", SetupAssets.LoadOrCreateId<UIScreenId>(IdsStep.MAIN_MENU_SCREEN));
            return installer;
        }

        private void WriteProjectContext(List<Object> installers)
        {
            SetupAssets.EnsureFolder(ProjectSetupContext.RESOURCES);
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(CONTEXT_PATH) != null;
            GameObject root = exists ? PrefabUtility.LoadPrefabContents(CONTEXT_PATH) : new GameObject("ProjectContext");

            ProjectContext projectContext = root.GetComponent<ProjectContext>();
            if (projectContext == null)
            {
                projectContext = root.AddComponent<ProjectContext>();
            }

            SetupAssets.SetReferences(projectContext, "_scriptableObjectInstallers", installers);
            PrefabUtility.SaveAsPrefabAsset(root, CONTEXT_PATH);

            if (exists)
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            else
            {
                Object.DestroyImmediate(root);
            }
        }

        private T Load<T>() where T : ScriptableObject
        {
            return AssetDatabase.LoadAssetAtPath<T>(ConfigsStep.GetPath<T>());
        }
    }
}
