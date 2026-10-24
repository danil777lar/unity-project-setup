using System;
using System.IO;
using UnityEngine;

namespace Larje.ProjectSetup.Editor
{
    [Serializable]
    public sealed class ProjectSetupOptions
    {
        public bool Character;
        public string ProductName = "Game";
        public string CompanyName = "Larje";
        public string RootNamespace = "Game";
        public string BundleId;
        public string Platform = "Android";
        public string Orientation = "Portrait";
        public string AssetStoreCachePath;
        public string[] AssetStorePackages = new string[0];

        public static ProjectSetupOptions Load(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return new ProjectSetupOptions();
            }

            ProjectSetupOptions options = JsonUtility.FromJson<ProjectSetupOptions>(File.ReadAllText(path));
            return options ?? new ProjectSetupOptions();
        }

        public void Save(string path)
        {
            File.WriteAllText(path, JsonUtility.ToJson(this, true));
        }
    }
}
