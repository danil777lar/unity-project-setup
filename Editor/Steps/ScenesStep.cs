using System.Collections.Generic;
using System.IO;
using Larje.ProjectSetup.Editor.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using Zenject;

namespace Larje.ProjectSetup.Editor.Steps
{
    public sealed class ScenesStep : IProjectSetupStep
    {
        public string Name => "Scenes";

        public void Run(ProjectSetupContext context)
        {
            SetupAssets.EnsureFolder(ProjectSetupContext.SCENES);

            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
            foreach (string sceneName in new[] { ProjectSetupContext.SPLASH_SCENE, ProjectSetupContext.MAIN_SCENE })
            {
                string path = $"{ProjectSetupContext.SCENES}/{sceneName}.unity";
                bool created = !File.Exists(path);
                Scene scene = created
                    ? EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single)
                    : EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

                bool changed = EnsureComponent<EventSystem>(scene, "EventSystem", typeof(InputSystemUIInputModule));
                changed |= EnsureComponent<SceneContext>(scene, "SceneContext");
                if (created || changed)
                {
                    EditorSceneManager.SaveScene(scene, path);
                    context.Report.Info(Name, (created ? "Created " : "Updated ") + path);
                }

                buildScenes.Add(new EditorBuildSettingsScene(path, true));
            }

            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (!buildScenes.Exists(x => x.path == scene.path))
                {
                    buildScenes.Add(scene);
                }
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();
        }

        private bool EnsureComponent<T>(Scene scene, string objectName, params System.Type[] extraComponents) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<T>(true) != null)
                {
                    return false;
                }
            }

            GameObject target = new GameObject(objectName, typeof(T));
            foreach (System.Type component in extraComponents)
            {
                target.AddComponent(component);
            }

            SceneManager.MoveGameObjectToScene(target, scene);
            return true;
        }
    }
}
