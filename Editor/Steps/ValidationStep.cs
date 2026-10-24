using System.Collections.Generic;
using Larje.Core;
using UnityEditor;
using UnityEngine;

namespace Larje.ProjectSetup.Editor.Steps
{
    public sealed class ValidationStep : IProjectSetupStep
    {
        public string Name => "Validation";

        public void Run(ProjectSetupContext context)
        {
            int checkedConfigs = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { ProjectSetupContext.CONFIGS }))
            {
                ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset is IConfigValidatable validatable)
                {
                    checkedConfigs++;
                    List<string> problems = new List<string>();
                    validatable.Validate(problems);
                    foreach (string problem in problems)
                    {
                        context.Report.Warning(Name, $"{asset.name}: {problem}");
                    }
                }
            }

            context.Report.Info(Name, $"Checked {checkedConfigs} validatable config(s)");
        }
    }
}
