using System;
using Larje.ProjectSetup.Editor.Steps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Larje.ProjectSetup.Editor
{
    public static class ProjectSetupRunner
    {
        public const string OPTIONS_PATH = "ProjectSettings/LarjeProjectSetup.json";
        public const string REPORT_PATH = "Logs/LarjeProjectSetup.txt";

        private const string OPTIONS_ARGUMENT = "-larjeSetupOptions";
        private const int EXIT_DONE = 0;
        private const int EXIT_FAILED = 1;
        private const int EXIT_PENDING = 2;

        [MenuItem("Tools/Larje/Project Setup/Run")]
        public static void RunFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            ProjectSetupReport report = Run(ProjectSetupOptions.Load(OPTIONS_PATH));
            report.Save(REPORT_PATH);
            report.Log();
        }

        [MenuItem("Tools/Larje/Project Setup/Create Options File")]
        public static void CreateOptionsFile()
        {
            ProjectSetupOptions options = ProjectSetupOptions.Load(OPTIONS_PATH);
            options.Save(OPTIONS_PATH);
            EditorUtility.RevealInFinder(OPTIONS_PATH);
        }

        public static void RunFromCommandLine()
        {
            int exitCode = EXIT_FAILED;
            try
            {
                string optionsPath = GetArgument(OPTIONS_ARGUMENT) ?? OPTIONS_PATH;
                ProjectSetupOptions options = ProjectSetupOptions.Load(optionsPath);
                options.Save(OPTIONS_PATH);

                ProjectSetupReport report = Run(options);
                report.Save(REPORT_PATH);
                report.Log();
                exitCode = report.HasErrors ? EXIT_FAILED : report.HasPending ? EXIT_PENDING : EXIT_DONE;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            EditorApplication.Exit(exitCode);
        }

        public static ProjectSetupReport Run(ProjectSetupOptions options)
        {
            ProjectSetupContext context = new ProjectSetupContext(options);
            IProjectSetupStep[] steps =
            {
                new PlayerSettingsStep(),
                new RenderPipelineStep(),
                new AssetStoreImportStep(),
                new IdsStep(),
                new InputStep(),
                new ConfigsStep(),
                new UIStep(),
                new ScenesStep(),
                new CodeStep(),
                new ContextStep(),
                new ValidationStep()
            };

            foreach (IProjectSetupStep step in steps)
            {
                try
                {
                    step.Run(context);
                    AssetDatabase.SaveAssets();
                }
                catch (Exception exception)
                {
                    context.Report.Error(step.Name, exception.Message);
                    Debug.LogException(exception);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return context.Report;
        }

        private static string GetArgument(string name)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length - 1; i++)
            {
                if (arguments[i] == name)
                {
                    return arguments[i + 1];
                }
            }

            return null;
        }
    }
}
