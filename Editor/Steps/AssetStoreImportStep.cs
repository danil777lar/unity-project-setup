using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace Larje.ProjectSetup.Editor.Steps
{
    public sealed class AssetStoreImportStep : IProjectSetupStep
    {
        private const string IMPORTED_LIST_PATH = "ProjectSettings/LarjeAssetStoreImports.txt";
        private const string CACHE_FOLDER = "Asset Store-5.x";
        private const string PACKAGE_EXTENSION = ".unitypackage";

        public string Name => "Asset Store Import";

        public void Run(ProjectSetupContext context)
        {
            string[] packages = context.Options.AssetStorePackages ?? Array.Empty<string>();
            if (packages.Length == 0)
            {
                return;
            }

            string cache = GetCachePath(context.Options.AssetStoreCachePath);
            HashSet<string> imported = LoadImported();

            foreach (string package in packages.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                if (imported.Contains(package))
                {
                    context.Report.Info(Name, $"'{package}' already imported");
                }
                else if (TryFindPackage(cache, package, out string path))
                {
                    AssetDatabase.ImportPackage(path, false);
                    imported.Add(package);
                    context.Report.Info(Name, $"Imported '{package}'");
                }
                else
                {
                    context.Report.Error(Name, $"'{package}' not found in {cache}. Download it once in Package Manager > My Assets and run setup again");
                }
            }

            File.WriteAllLines(IMPORTED_LIST_PATH, imported.OrderBy(x => x));
        }

        public static string GetCachePath(string overridePath)
        {
            if (!string.IsNullOrEmpty(overridePath))
            {
                return overridePath;
            }

            string environment = Environment.GetEnvironmentVariable("ASSETSTORE_CACHE_PATH");
            if (!string.IsNullOrEmpty(environment))
            {
                return environment;
            }

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Unity", CACHE_FOLDER);
                case PlatformID.MacOSX:
                    return Path.Combine(home, "Library", "Unity", CACHE_FOLDER);
                default:
                    return Directory.Exists(Path.Combine(home, "Library", "Unity", CACHE_FOLDER))
                        ? Path.Combine(home, "Library", "Unity", CACHE_FOLDER)
                        : Path.Combine(home, ".local", "share", "unity3d", CACHE_FOLDER);
            }
        }

        public static bool TryFindPackage(string cache, string package, out string path)
        {
            path = null;
            if (!Directory.Exists(cache))
            {
                return false;
            }

            string fileName = package.EndsWith(PACKAGE_EXTENSION) ? package : package + PACKAGE_EXTENSION;
            string direct = Path.Combine(cache, fileName);
            if (File.Exists(direct))
            {
                path = direct;
                return true;
            }

            path = Directory.EnumerateFiles(cache, "*" + PACKAGE_EXTENSION, SearchOption.AllDirectories)
                .FirstOrDefault(x => string.Equals(Path.GetFileName(x), Path.GetFileName(fileName), StringComparison.OrdinalIgnoreCase));
            return path != null;
        }

        private HashSet<string> LoadImported()
        {
            return File.Exists(IMPORTED_LIST_PATH)
                ? new HashSet<string>(File.ReadAllLines(IMPORTED_LIST_PATH).Where(x => !string.IsNullOrWhiteSpace(x)))
                : new HashSet<string>();
        }
    }
}
