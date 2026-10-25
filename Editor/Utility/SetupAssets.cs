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
            return (T)LoadOrCreate(typeof(T), path, out bool _);
        }

        public static T LoadOrCreate<T>(string path, out bool created) where T : ScriptableObject
        {
            return (T)LoadOrCreate(typeof(T), path, out created);
        }

        public static ScriptableObject LoadOrCreate(System.Type type, string path)
        {
            return LoadOrCreate(type, path, out bool _);
        }

        public static ScriptableObject LoadOrCreate(System.Type type, string path, out bool created)
        {
            ScriptableObject asset = (ScriptableObject)AssetDatabase.LoadAssetAtPath(path, type);
            created = asset == null;
            if (!created)
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

        public static void SetReferenceIfMissing(Object target, string field, Object reference)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = GetProperty(serialized, field);
            if (property.objectReferenceValue == null && reference != null)
            {
                property.objectReferenceValue = reference;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        public static void AddReferences(Object target, string field, IReadOnlyList<Object> references)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = GetProperty(serialized, field);
            HashSet<Object> existing = new HashSet<Object>();
            for (int i = 0; i < property.arraySize; i++)
            {
                existing.Add(property.GetArrayElementAtIndex(i).objectReferenceValue);
            }

            bool changed = false;
            foreach (Object reference in references)
            {
                if (reference != null && existing.Add(reference))
                {
                    property.arraySize++;
                    property.GetArrayElementAtIndex(property.arraySize - 1).objectReferenceValue = reference;
                    changed = true;
                }
            }

            if (changed)
            {
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        public static List<Object> GetReferences(Object target, string field)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = GetProperty(serialized, field);
            List<Object> references = new List<Object>();
            for (int i = 0; i < property.arraySize; i++)
            {
                references.Add(property.GetArrayElementAtIndex(i).objectReferenceValue);
            }

            return references;
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
