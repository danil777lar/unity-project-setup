using System.Collections.Generic;
using System.IO;
using Larje.Core.Ids;
using UnityEditor;
using UnityEngine;

namespace Larje.ProjectSetup.Editor.Utility
{
    public static class SetupAssets
    {
        public static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            return (T)LoadOrCreate(typeof(T), path);
        }

        public static ScriptableObject LoadOrCreate(System.Type type, string path)
        {
            ScriptableObject asset = (ScriptableObject)AssetDatabase.LoadAssetAtPath(path, type);
            if (asset != null)
            {
                return asset;
            }

            EnsureFolder(Path.GetDirectoryName(path));
            asset = ScriptableObject.CreateInstance(type);
            asset.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        public static T LoadOrCreateId<T>(string idName) where T : LarjeId
        {
            return LoadOrCreate<T>($"{ProjectSetupContext.IDS}/{typeof(T).Name}/{idName}.asset");
        }

        public static void EnsureFolder(string folder)
        {
            string normalized = folder.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(normalized))
            {
                return;
            }

            string parent = Path.GetDirectoryName(normalized).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(normalized));
        }

        public static void SetReference(Object target, string field, Object reference)
        {
            SerializedObject serialized = new SerializedObject(target);
            GetProperty(serialized, field).objectReferenceValue = reference;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetReferences(Object target, string field, IReadOnlyList<Object> references)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = GetProperty(serialized, field);
            property.arraySize = references.Count;
            for (int i = 0; i < references.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = references[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetString(Object target, string field, string text)
        {
            SerializedObject serialized = new SerializedObject(target);
            GetProperty(serialized, field).stringValue = text;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetFloat(Object target, string field, float number)
        {
            SerializedObject serialized = new SerializedObject(target);
            GetProperty(serialized, field).floatValue = number;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetBool(Object target, string field, bool flag)
        {
            SerializedObject serialized = new SerializedObject(target);
            GetProperty(serialized, field).boolValue = flag;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static SerializedProperty GetProperty(SerializedObject serialized, string field)
        {
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                throw new System.InvalidOperationException($"Field '{field}' not found on {serialized.targetObject.GetType().Name}");
            }

            return property;
        }
    }
}
