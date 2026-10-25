using System;
using UnityEditor;

namespace Larje.ProjectSetup.Editor.Build
{
    public sealed class ProjectBuildOptions
    {
        public bool Development;
        public bool AppBundle;
        public bool Mono;
        public int BuildNumber;
        public string Output;
        public string Version;
        public string WebGLTemplate;
        public string KeystorePath;
        public string KeystorePassword;
        public string KeyAlias;
        public string KeyPassword;
        public BuildTarget Target = BuildTarget.Android;

        public static ProjectBuildOptions FromCommandLine(string[] arguments)
        {
            ProjectBuildOptions options = new ProjectBuildOptions();
            for (int i = 0; i < arguments.Length; i++)
            {
                string next = i + 1 < arguments.Length ? arguments[i + 1] : string.Empty;
                switch (arguments[i])
                {
                    case "-larjeBuildTarget":
                        options.Target = (BuildTarget)Enum.Parse(typeof(BuildTarget), next, true);
                        break;
                    case "-larjeBuildOutput":
                        options.Output = next;
                        break;
                    case "-larjeBuildVersion":
                        options.Version = next;
                        break;
                    case "-larjeBuildNumber":
                        options.BuildNumber = int.Parse(next);
                        break;
                    case "-larjeBuildDevelopment":
                        options.Development = true;
                        break;
                    case "-larjeBuildAab":
                        options.AppBundle = true;
                        break;
                    case "-larjeBuildMono":
                        options.Mono = true;
                        break;
                    case "-larjeWebGLTemplate":
                        options.WebGLTemplate = next;
                        break;
                    case "-larjeKeystore":
                        options.KeystorePath = next;
                        break;
                    case "-larjeKeyAlias":
                        options.KeyAlias = next;
                        break;
                }
            }

            options.KeystorePassword = Environment.GetEnvironmentVariable("LARJE_KEYSTORE_PASS");
            options.KeyPassword = Environment.GetEnvironmentVariable("LARJE_KEY_PASS") ?? options.KeystorePassword;
            return options;
        }
    }
}
