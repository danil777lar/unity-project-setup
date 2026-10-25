using System;
using System.Collections.Generic;
using System.Linq;
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
            ScriptableObject core = CreateCoreInstaller();
            ScriptableObject character = context.Options.Character ? CreateCharacterInstaller(context) : null;
            ScriptableObject project = CreateProjectInstaller(context);

            int count = WriteProjectContext(core, character, project);
            context.Report.Info(Name, $"{CONTEXT_PATH} uses {count} installer(s)");
        }

        private LarjeCoreInstaller CreateCoreInstaller()
        {
            LarjeCoreInstaller installer = SetupAssets.LoadOrCreate<LarjeCoreInstaller>(ProjectSetupContext.INSTALLERS + "/LarjeCoreInstaller.asset", out bool created);
            SetupAssets.SetReferenceIfMissing(installer, "saves", Load<SaveConfig>());
            SetupAssets.SetReferenceIfMissing(installer, "gameStates", Load<GameStatesConfig>());
            SetupAssets.SetReferenceIfMissing(installer, "sceneLoader", Load<SceneLoaderConfig>());
            SetupAssets.SetReferenceIfMissing(installer, "appFlow", Load<AppFlowConfig>());
            SetupAssets.SetReferenceIfMissing(installer, "timeScale", Load<TimeScaleConfig>());
            SetupAssets.SetReferenceIfMissing(installer, "input", AssetDatabase.LoadAssetAtPath<InputConfig>(InputStep.INPUT_CONFIG_PATH));
            SetupAssets.SetReferenceIfMissing(installer, "pointerControl", AssetDatabase.LoadAssetAtPath<PointerControlConfig>(InputStep.POINTER_CONFIG_PATH));
            SetupAssets.SetReferenceIfMissing(installer, "ui", AssetDatabase.LoadAssetAtPath<UIConfig>(UIStep.UI_CONFIG_PATH));
            SetupAssets.SetReferenceIfMissing(installer, "sound", Load<SoundConfig>());
            SetupAssets.SetReferenceIfMissing(installer, "currency", Load<CurrencyConfig>());
            SetupAssets.SetReferenceIfMissing(installer, "items", Load<ItemsConfig>());
            SetupAssets.SetReferenceIfMissing(installer, "purchases", Load<PurchasesConfig>());
            SetupAssets.SetReferenceIfMissing(installer, "debugConsole", Load<DebugConsoleConfig>());
            if (created)
            {
                SetupAssets.SetBool(installer, "bindSimulatedMonetization", true);
            }

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
            SetupAssets.SetReferenceIfMissing(installer, "cursor", cursor);
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
            SetupAssets.SetReferenceIfMissing(installer, "_mainMenuScreen", SetupAssets.LoadOrCreateId<UIScreenId>(IdsStep.MAIN_MENU_SCREEN));
            return installer;
        }

        private int WriteProjectContext(Object core, Object character, Object project)
        {
            SetupAssets.EnsureFolder(ProjectSetupContext.RESOURCES);
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(CONTEXT_PATH) != null;
            GameObject root = exists ? PrefabUtility.LoadPrefabContents(CONTEXT_PATH) : new GameObject("ProjectContext");

            ProjectContext projectContext = root.GetComponent<ProjectContext>();
            if (projectContext == null)
            {
                projectContext = root.AddComponent<ProjectContext>();
            }

            List<Object> installers = MergeInstallers(SetupAssets.GetReferences(projectContext, "_scriptableObjectInstallers"), core, character, project);
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

            return installers.Count;
        }

        private List<Object> MergeInstallers(List<Object> existing, Object core, Object character, Object project)
        {
            List<Object> installers = existing.Where(x => x != null).Distinct().ToList();
            if (!installers.Contains(core))
            {
                installers.Insert(0, core);
            }

            if (character != null && !installers.Contains(character))
            {
                installers.Insert(installers.IndexOf(core) + 1, character);
            }

            if (project != null && !installers.Contains(project))
            {
                installers.Add(project);
            }

            return installers;
        }

        private T Load<T>() where T : ScriptableObject
        {
            return AssetDatabase.LoadAssetAtPath<T>(ConfigsStep.GetPath<T>());
        }
    }
}
