using System.IO;
using Larje.ProjectSetup.Editor.Utility;
using Larje.UI;
using Larje.UI.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Larje.ProjectSetup.Editor.Steps
{
    public sealed class UIStep : IProjectSetupStep
    {
        public const string UI_ROOT = ProjectSetupContext.ROOT + "/UI";
        public const string UI_CONFIG_PATH = ProjectSetupContext.CONFIGS + "/UIConfig.asset";
        public const string PANEL_SETTINGS_PATH = UI_ROOT + "/LarjePanelSettings.asset";
        public const string THEME_PATH = UI_ROOT + "/LarjeTheme.tss";
        public const string MAIN_MENU_PATH = UI_ROOT + "/Screens/main_menu.uxml";

        private const string THEME_TEXT = "@import url(\"unity-theme://default\");\n";

        public string Name => "UI";

        public void Run(ProjectSetupContext context)
        {
            ThemeStyleSheet theme = CreateTheme();
            PanelSettings panel = CreatePanelSettings(context, theme);
            CreateMainMenuLayout(context);

            UIConfig config = SetupAssets.LoadOrCreate<UIConfig>(UI_CONFIG_PATH);
            SetupAssets.SetReferenceIfMissing(config, "panelSettings", panel);
            AssetDatabase.SaveAssets();
            context.Report.Info(Name, UIViewSync.Sync().TrimEnd().Replace("\n", "; "));
        }

        private ThemeStyleSheet CreateTheme()
        {
            if (!File.Exists(THEME_PATH))
            {
                SetupAssets.EnsureFolder(UI_ROOT);
                File.WriteAllText(THEME_PATH, THEME_TEXT);
                AssetDatabase.ImportAsset(THEME_PATH, ImportAssetOptions.ForceSynchronousImport);
            }

            return AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(THEME_PATH);
        }

        private PanelSettings CreatePanelSettings(ProjectSetupContext context, ThemeStyleSheet theme)
        {
            PanelSettings panel = SetupAssets.LoadOrCreate<PanelSettings>(PANEL_SETTINGS_PATH, out bool created);
            if (created)
            {
                bool landscape = context.Options.Orientation == "Landscape";
                panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panel.referenceResolution = landscape ? new Vector2Int(1920, 1080) : new Vector2Int(1080, 1920);
                panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                panel.match = landscape ? 1f : 0f;
                EditorUtility.SetDirty(panel);
                context.Report.Info(Name, "Created " + PANEL_SETTINGS_PATH);
            }

            if (panel.themeStyleSheet == null && theme != null)
            {
                panel.themeStyleSheet = theme;
                EditorUtility.SetDirty(panel);
            }

            return panel;
        }

        private void CreateMainMenuLayout(ProjectSetupContext context)
        {
            if (File.Exists(MAIN_MENU_PATH))
            {
                return;
            }

            SetupAssets.EnsureFolder(Path.GetDirectoryName(MAIN_MENU_PATH).Replace('\\', '/'));
            string title = System.Security.SecurityElement.Escape(context.Options.ProductName);
            File.WriteAllText(MAIN_MENU_PATH,
                "<ui:UXML xmlns:ui=\"UnityEngine.UIElements\" editor-extension-mode=\"False\">\n" +
                "    <ui:VisualElement name=\"MainMenuScreen\" class=\"screen\" style=\"position: absolute; left: 0; top: 0; right: 0; bottom: 0; " +
                "background-color: rgb(30, 34, 51); align-items: center; justify-content: center;\">\n" +
                $"        <ui:Label name=\"Title\" text=\"{title}\" class=\"anim--pop-in\" style=\"font-size: 96px; color: rgb(255, 255, 255); -unity-font-style: bold;\" />\n" +
                "    </ui:VisualElement>\n" +
                "</ui:UXML>\n");
            AssetDatabase.ImportAsset(MAIN_MENU_PATH, ImportAssetOptions.ForceSynchronousImport);
            context.Report.Info(Name, "Created placeholder " + MAIN_MENU_PATH + " (replaced by design.py ui-export)");
        }
    }
}
