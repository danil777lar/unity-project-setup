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

            InputSystem.actions = actions;

            InputConfig config = SetupAssets.LoadOrCreate<InputConfig>(INPUT_CONFIG_PATH);
            SetupAssets.SetReference(config, "actions", actions);
            SetMaps(config);
            SetupAssets.SetReference(config, "back", FindReference("UI/Cancel"));
            SetupAssets.SetReference(config, "move", FindReference("Player/Move"));
            SetupAssets.SetReference(config, "run", FindReference("Player/Sprint"));
            SetupAssets.SetReference(config, "pointer", FindReference("UI/Point"));

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

        private void SetMaps(InputConfig config)
        {
            SerializedObject serialized = new SerializedObject(config);
            SerializedProperty maps = SetupAssets.GetProperty(serialized, "maps");
            maps.arraySize = Maps.Length;
            for (int i = 0; i < Maps.Length; i++)
            {
                SerializedProperty map = maps.GetArrayElementAtIndex(i);
                map.FindPropertyRelative("Name").stringValue = Maps[i];
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
