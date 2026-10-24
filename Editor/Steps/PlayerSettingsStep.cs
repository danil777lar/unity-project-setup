using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Larje.ProjectSetup.Editor.Steps
{
    public sealed class PlayerSettingsStep : IProjectSetupStep
    {
        private const int INPUT_SYSTEM_ONLY = 1;

        public string Name => "Player Settings";

        public void Run(ProjectSetupContext context)
        {
            ProjectSetupOptions options = context.Options;
            PlayerSettings.companyName = options.CompanyName;
            PlayerSettings.productName = options.ProductName;
            PlayerSettings.colorSpace = ColorSpace.Linear;

            string bundleId = string.IsNullOrEmpty(options.BundleId)
                ? $"com.{Sanitize(options.CompanyName)}.{Sanitize(options.ProductName)}"
                : options.BundleId;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, bundleId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, bundleId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, bundleId);

            PlayerSettings.defaultInterfaceOrientation = GetOrientation(options.Orientation);
            SetInputHandler(context);
            SwitchPlatform(context);

            context.Report.Info(Name, $"{options.CompanyName} / {options.ProductName} ({bundleId}), {options.Orientation}");
        }

        private string Sanitize(string text)
        {
            string letters = new string(text.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
            return string.IsNullOrEmpty(letters) ? "game" : letters;
        }

        private UIOrientation GetOrientation(string orientation)
        {
            switch (orientation)
            {
                case "Landscape":
                    return UIOrientation.LandscapeLeft;
                case "Auto":
                    return UIOrientation.AutoRotation;
                default:
                    return UIOrientation.Portrait;
            }
        }

        private void SetInputHandler(ProjectSetupContext context)
        {
            UnityEngine.Object[] settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (settings.Length == 0)
            {
                context.Report.Warning(Name, "ProjectSettings.asset not found, input handler not changed");
                return;
            }

            SerializedObject serialized = new SerializedObject(settings[0]);
            SerializedProperty handler = serialized.FindProperty("activeInputHandler");
            if (handler != null && handler.intValue != INPUT_SYSTEM_ONLY)
            {
                handler.intValue = INPUT_SYSTEM_ONLY;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                context.Report.Info(Name, "Active input handler set to Input System (applies after editor restart)");
            }
        }

        private void SwitchPlatform(ProjectSetupContext context)
        {
            if (string.IsNullOrEmpty(context.Options.Platform))
            {
                return;
            }

            if (!Enum.TryParse(context.Options.Platform, out BuildTarget target))
            {
                context.Report.Warning(Name, $"Unknown platform '{context.Options.Platform}'");
                return;
            }

            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);
            if (EditorUserBuildSettings.activeBuildTarget == target)
            {
                return;
            }

            if (!BuildPipeline.IsBuildTargetSupported(group, target))
            {
                context.Report.Warning(Name, $"Platform {target} module is not installed, build target not switched");
                return;
            }

            EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);
            context.Report.Info(Name, $"Build target switched to {target}");
        }
    }
}
