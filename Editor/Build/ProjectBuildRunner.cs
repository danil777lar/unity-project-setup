using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Larje.ProjectSetup.Editor.Build
{
    public static class ProjectBuildRunner
    {
        public const string REPORT_PATH = "Logs/LarjeBuild.txt";

        private const string TEMPLATES_FOLDER = "Assets/WebGLTemplates";
        private const string PACKAGE_TEMPLATES = "WebGLTemplates~";
        private const int EXIT_DONE = 0;
        private const int EXIT_FAILED = 1;

        public static void RunFromCommandLine()
        {
            int exitCode = EXIT_FAILED;
            try
            {
                BuildReport report = Build(ProjectBuildOptions.FromCommandLine(Environment.GetCommandLineArgs()));
                exitCode = report.summary.result == BuildResult.Succeeded ? EXIT_DONE : EXIT_FAILED;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                File.WriteAllText(REPORT_PATH, "Result: Failed\nError: " + exception.Message + "\n");
            }

            EditorApplication.Exit(exitCode);
        }

        public static BuildReport Build(ProjectBuildOptions options)
        {
            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(options.Target);
            NamedBuildTarget namedTarget = NamedBuildTarget.FromBuildTargetGroup(group);
            if (!BuildPipeline.IsBuildTargetSupported(group, options.Target))
            {
                throw new InvalidOperationException($"Build target {options.Target} is not installed. Install its module with Unity Hub.");
            }

            if (EditorUserBuildSettings.activeBuildTarget != options.Target)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(group, options.Target);
            }

            ApplyVersion(options);
            switch (options.Target)
            {
                case BuildTarget.Android:
                    ConfigureAndroid(options, namedTarget);
                    break;
                case BuildTarget.WebGL:
                    ConfigureWebGL(options);
                    break;
            }

            string[] scenes = EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray();
            if (scenes.Length == 0)
            {
                throw new InvalidOperationException("No enabled scenes in Build Settings.");
            }

            BuildPlayerOptions playerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                target = options.Target,
                targetGroup = group,
                locationPathName = GetOutputPath(options),
                options = options.Development ? BuildOptions.Development : BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(playerOptions);
            WriteReport(report, playerOptions.locationPathName);
            return report;
        }

        private static void ApplyVersion(ProjectBuildOptions options)
        {
            if (!string.IsNullOrEmpty(options.Version))
            {
                PlayerSettings.bundleVersion = options.Version;
            }

            if (options.BuildNumber > 0)
            {
                PlayerSettings.Android.bundleVersionCode = options.BuildNumber;
                PlayerSettings.iOS.buildNumber = options.BuildNumber.ToString();
            }
        }

        private static void ConfigureAndroid(ProjectBuildOptions options, NamedBuildTarget namedTarget)
        {
            EditorUserBuildSettings.buildAppBundle = options.AppBundle;
            if (options.Mono)
            {
                PlayerSettings.SetScriptingBackend(namedTarget, ScriptingImplementation.Mono2x);
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
            }
            else
            {
                PlayerSettings.SetScriptingBackend(namedTarget, ScriptingImplementation.IL2CPP);
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            }

            bool customKeystore = !string.IsNullOrEmpty(options.KeystorePath);
            PlayerSettings.Android.useCustomKeystore = customKeystore;
            if (customKeystore)
            {
                PlayerSettings.Android.keystoreName = Path.GetFullPath(options.KeystorePath);
                PlayerSettings.Android.keystorePass = options.KeystorePassword;
                PlayerSettings.Android.keyaliasName = options.KeyAlias;
                PlayerSettings.Android.keyaliasPass = options.KeyPassword;
            }
        }

        private static void ConfigureWebGL(ProjectBuildOptions options)
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            if (string.IsNullOrEmpty(options.WebGLTemplate))
            {
                return;
            }

            UnityEditor.PackageManager.PackageInfo package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(ProjectBuildRunner).Assembly);
            string source = package != null ? Path.Combine(package.resolvedPath, PACKAGE_TEMPLATES, options.WebGLTemplate) : string.Empty;
            string target = Path.Combine(TEMPLATES_FOLDER, options.WebGLTemplate);
            if (Directory.Exists(source) && !Directory.Exists(target))
            {
                CopyDirectory(source, target);
                AssetDatabase.Refresh();
            }
            else if (!Directory.Exists(target))
            {
                throw new DirectoryNotFoundException($"WebGL template '{options.WebGLTemplate}' not found in {TEMPLATES_FOLDER} or in the projectsetup package.");
            }

            PlayerSettings.WebGL.template = "PROJECT:" + options.WebGLTemplate;
        }

        private static string GetOutputPath(ProjectBuildOptions options)
        {
            if (!string.IsNullOrEmpty(options.Output))
            {
                return options.Output;
            }

            string name = PlayerSettings.productName.Replace(" ", string.Empty);
            string folder = Path.Combine("Builds", options.Target.ToString());
            switch (options.Target)
            {
                case BuildTarget.Android:
                    return Path.Combine(folder, $"{name}-{PlayerSettings.bundleVersion}.{(options.AppBundle ? "aab" : "apk")}");
                case BuildTarget.StandaloneLinux64:
                    return Path.Combine(folder, name + ".x86_64");
                case BuildTarget.StandaloneWindows64:
                    return Path.Combine(folder, name + ".exe");
                default:
                    return folder;
            }
        }

        private static void WriteReport(BuildReport report, string output)
        {
            BuildSummary summary = report.summary;
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Result: {summary.result}");
            builder.AppendLine($"Target: {summary.platform}");
            builder.AppendLine($"Output: {Path.GetFullPath(output)}");
            builder.AppendLine($"Size: {summary.totalSize / (1024f * 1024f):F1} MB");
            builder.AppendLine($"Time: {summary.totalTime.TotalSeconds:F0} s");
            builder.AppendLine($"Errors: {summary.totalErrors}, warnings: {summary.totalWarnings}");
            foreach (BuildStep step in report.steps)
            {
                foreach (BuildStepMessage message in step.messages.Where(x => x.type == LogType.Error || x.type == LogType.Exception))
                {
                    builder.AppendLine($"[Error] {step.name}: {message.content}");
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(REPORT_PATH));
            File.WriteAllText(REPORT_PATH, builder.ToString());
            Debug.Log(builder.ToString());
        }

        private static void CopyDirectory(string source, string target)
        {
            Directory.CreateDirectory(target);
            foreach (string file in Directory.GetFiles(source))
            {
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
            }

            foreach (string directory in Directory.GetDirectories(source))
            {
                CopyDirectory(directory, Path.Combine(target, Path.GetFileName(directory)));
            }
        }
    }
}
