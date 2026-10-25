using System.Collections.Generic;
using System.IO;
using System.Linq;
using Larje.Core.Input;
using Larje.ProjectSetup.Editor.Utility;
using UnityEditor;
using UnityEngine.InputSystem;

namespace Larje.ProjectSetup.Editor.Steps
{
    public sealed class InputStep : IProjectSetupStep
    {
        public const string ACTIONS_PATH = ProjectSetupContext.SETTINGS + "/GameInput.inputactions";
        public const string INPUT_CONFIG_PATH = ProjectSetupContext.CONFIGS + "/InputConfig.asset";
        public const string POINTER_CONFIG_PATH = ProjectSetupContext.CONFIGS + "/PointerControlConfig.asset";

        private const string TEMPLATE_PATH = "Packages/com.unity.inputsystem/InputSystem/Editor/ProjectWideActions/ProjectWideActionsTemplate.json";
        private const string TEMPLATE_NAME = "\"name\": \"ProjectWideActionsTemplate\"";
        private const string UNITY_DEFAULT_ACTIONS_PATH = "Assets/InputSystem_Actions.inputactions";

        private static readonly string[] Maps = { "Player", "UI" };

        public string Name => "Input";

        public void Run(ProjectSetupContext context)
        {
            CreateActionsAsset(context);

            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ACTIONS_PATH);
            if (actions == null)
            {
                context.Report.Error(Name, "Input actions asset could not be imported from " + ACTIONS_PATH);
                return;
            }

            if (InputSystem.actions == null || AssetDatabase.GetAssetPath(InputSystem.actions) == UNITY_DEFAULT_ACTIONS_PATH)
            {
                InputSystem.actions = actions;
            }

            InputConfig config = SetupAssets.LoadOrCreate<InputConfig>(INPUT_CONFIG_PATH);
            SetupAssets.SetReferenceIfMissing(config, "actions", actions);
            AddMissingMaps(config);
            SetupAssets.SetReferenceIfMissing(config, "back", FindReference("UI/Cancel"));
            SetupAssets.SetReferenceIfMissing(config, "move", FindReference("Player/Move"));
            SetupAssets.SetReferenceIfMissing(config, "run", FindReference("Player/Sprint"));
            SetupAssets.SetReferenceIfMissing(config, "pointer", FindReference("UI/Point"));

            SetupAssets.LoadOrCreate<PointerControlConfig>(POINTER_CONFIG_PATH);
            context.Report.Info(Name, "Input actions (Player, UI) and InputConfig are ready");
        }

        private void CreateActionsAsset(ProjectSetupContext context)
        {
            if (File.Exists(ACTIONS_PATH))
            {
                return;
            }

            string template = Path.GetFullPath(TEMPLATE_PATH);
            if (!File.Exists(template))
            {
                context.Report.Error(Name, "Input System actions template not found at " + TEMPLATE_PATH);
                return;
            }

            SetupAssets.EnsureFolder(ProjectSetupContext.SETTINGS);
            string json = File.ReadAllText(template);
            int nameIndex = json.IndexOf(TEMPLATE_NAME, System.StringComparison.Ordinal);
            if (nameIndex >= 0)
            {
                json = json.Remove(nameIndex, TEMPLATE_NAME.Length).Insert(nameIndex, "\"name\": \"GameInput\"");
            }

            File.WriteAllText(ACTIONS_PATH, json);
            AssetDatabase.ImportAsset(ACTIONS_PATH, ImportAssetOptions.ForceSynchronousImport);
        }

        private void AddMissingMaps(InputConfig config)
        {
            SerializedObject serialized = new SerializedObject(config);
            SerializedProperty maps = SetupAssets.GetProperty(serialized, "maps");
            HashSet<string> existing = new HashSet<string>();
            for (int i = 0; i < maps.arraySize; i++)
            {
                existing.Add(maps.GetArrayElementAtIndex(i).FindPropertyRelative("Name").stringValue);
            }

            foreach (string mapName in Maps.Where(x => !existing.Contains(x)))
            {
                maps.arraySize++;
                SerializedProperty map = maps.GetArrayElementAtIndex(maps.arraySize - 1);
                map.FindPropertyRelative("Name").stringValue = mapName;
                map.FindPropertyRelative("EnabledByDefault").boolValue = true;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private InputActionReference FindReference(string actionPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(ACTIONS_PATH)
                .OfType<InputActionReference>()
                .FirstOrDefault(x => x.action != null && $"{x.action.actionMap.name}/{x.action.name}" == actionPath);
        }
    }
}
