using System.Collections.Generic;
using System.IO;
using UnityEditor;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Larje.ProjectSetup.Editor.Steps
{
    public sealed class CodeStep : IProjectSetupStep
    {
        private const string TEMPLATES_FOLDER = "Editor/Templates~";

        public string Name => "Project Code";

        public void Run(ProjectSetupContext context)
        {
            string templates = Path.Combine(PackageInfo.FindForAssembly(typeof(CodeStep).Assembly).resolvedPath, TEMPLATES_FOLDER);
            Dictionary<string, string> tokens = new Dictionary<string, string>
            {
                { "{{Namespace}}", context.Options.RootNamespace },
                { "{{ProductName}}", context.Options.ProductName }
            };

            string scripts = ProjectSetupContext.SCRIPTS;
            bool created = false;
            created |= WriteIfMissing($"{scripts}/{context.Options.RootNamespace}.asmdef", CreateAssemblyDefinition(context.Options));
            created |= WriteTemplate(templates, "ProjectInstaller.cs.txt", $"{scripts}/Installers/ProjectInstaller.cs", tokens);
            created |= WriteTemplate(templates, "MenuScreenController.cs.txt", $"{scripts}/Flow/MenuScreenController.cs", tokens);
            created |= WriteTemplate(templates, "EditorAssembly.asmdef.txt", $"{ProjectSetupContext.ROOT}/Editor/{context.Options.RootNamespace}.Editor.asmdef", tokens);
            created |= WriteTemplate(templates, "Playtests.asmdef.txt", $"{ProjectSetupContext.ROOT}/Tests/PlayMode/{context.Options.RootNamespace}.Playtests.asmdef", tokens);
            created |= WriteTemplate(templates, "SmokePlaytest.cs.txt", $"{ProjectSetupContext.ROOT}/Tests/PlayMode/SmokePlaytest.cs", tokens);
            WriteTemplate(templates, "CLAUDE.md.txt", "CLAUDE.md", tokens);

            if (created)
            {
                AssetDatabase.Refresh();
                context.Report.Info(Name, $"Generated assembly '{context.Options.RootNamespace}' with ProjectInstaller and MenuScreenController, and the smoke playtest");
            }
        }

        private bool WriteTemplate(string templates, string template, string target, Dictionary<string, string> tokens)
        {
            string text = File.ReadAllText(Path.Combine(templates, template));
            foreach (KeyValuePair<string, string> token in tokens)
            {
                text = text.Replace(token.Key, token.Value);
            }

            return WriteIfMissing(target, text);
        }

        private bool WriteIfMissing(string path, string text)
        {
            if (File.Exists(path))
            {
                return false;
            }

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, text);
            return true;
        }

        private string CreateAssemblyDefinition(ProjectSetupOptions options)
        {
            List<string> references = new List<string> { "Larje.Core", "UniTask", "Zenject", "Unity.InputSystem" };
            if (options.Character)
            {
                references.Add("Larje.Character");
            }

            AssemblyDefinition definition = new AssemblyDefinition
            {
                name = options.RootNamespace,
                rootNamespace = options.RootNamespace,
                references = references.ToArray(),
                autoReferenced = true
            };

            return UnityEngine.JsonUtility.ToJson(definition, true);
        }

        [System.Serializable]
        private sealed class AssemblyDefinition
        {
            public string name;
            public string rootNamespace;
            public string[] references;
            public bool autoReferenced;
        }
    }
}
