using Larje.ProjectSetup.Editor.Utility;
using Larje.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Larje.ProjectSetup.Editor.Steps
{
    public sealed class UIStep : IProjectSetupStep
    {
        public const string UI_CONFIG_PATH = ProjectSetupContext.CONFIGS + "/UIConfig.asset";
        public const string MAIN_MENU_PATH = ProjectSetupContext.UI_PREFABS + "/MainMenuScreen.prefab";

        public string Name => "UI";

        public void Run(ProjectSetupContext context)
        {
            UIScreenId mainMenuId = SetupAssets.LoadOrCreateId<UIScreenId>(IdsStep.MAIN_MENU_SCREEN);
            UIScreen mainMenu = AssetDatabase.LoadAssetAtPath<UIScreen>(MAIN_MENU_PATH);
            if (mainMenu == null)
            {
                mainMenu = CreateScreenPrefab(context, mainMenuId);
                context.Report.Info(Name, "Created " + MAIN_MENU_PATH);
            }

            UIConfig config = SetupAssets.LoadOrCreate<UIConfig>(UI_CONFIG_PATH);
            SerializedObject serialized = new SerializedObject(config);
            SerializedProperty screens = SetupAssets.GetProperty(serialized, "screens");
            bool listed = false;
            for (int i = 0; i < screens.arraySize; i++)
            {
                listed |= screens.GetArrayElementAtIndex(i).objectReferenceValue == mainMenu;
            }

            if (!listed)
            {
                screens.arraySize++;
                screens.GetArrayElementAtIndex(screens.arraySize - 1).objectReferenceValue = mainMenu;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private UIScreen CreateScreenPrefab(ProjectSetupContext context, UIScreenId id)
        {
            SetupAssets.EnsureFolder(ProjectSetupContext.UI_PREFABS);

            int uiLayer = LayerMask.NameToLayer("UI");
            GameObject root = new GameObject("MainMenuScreen", typeof(RectTransform));
            root.layer = uiLayer;
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = context.Options.Orientation == "Landscape"
                ? new Vector2(1920f, 1080f)
                : new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            UIScreen screen = root.AddComponent<UIScreen>();
            SetupAssets.SetReference(screen, "id", id);

            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.layer = uiLayer;
            background.transform.SetParent(root.transform, false);
            RectTransform rect = (RectTransform)background.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            background.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.16f, 1f);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, MAIN_MENU_PATH);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<UIScreen>();
        }
    }
}
