using Larje.ProjectSetup.Editor.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Larje.ProjectSetup.Editor.Steps
{
    public sealed class RenderPipelineStep : IProjectSetupStep
    {
        private const string PIPELINE_PATH = ProjectSetupContext.SETTINGS + "/URP.asset";
        private const string RENDERER_PATH = ProjectSetupContext.SETTINGS + "/URP_Renderer.asset";

        public string Name => "Render Pipeline";

        public void Run(ProjectSetupContext context)
        {
            UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PIPELINE_PATH);
            if (pipeline == null)
            {
                SetupAssets.EnsureFolder(ProjectSetupContext.SETTINGS);
                UniversalRendererData renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RENDERER_PATH);
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PIPELINE_PATH);
                context.Report.Info(Name, "Created URP asset " + PIPELINE_PATH);
            }

            if (GraphicsSettings.defaultRenderPipeline == null)
            {
                GraphicsSettings.defaultRenderPipeline = pipeline;
            }

            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.count; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                if (QualitySettings.renderPipeline == null)
                {
                    QualitySettings.renderPipeline = GraphicsSettings.defaultRenderPipeline;
                }
            }

            QualitySettings.SetQualityLevel(current, false);
            if (!(GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset))
            {
                context.Report.Warning(Name, "Default render pipeline is not URP: " + GraphicsSettings.defaultRenderPipeline.name);
            }
        }
    }
}
