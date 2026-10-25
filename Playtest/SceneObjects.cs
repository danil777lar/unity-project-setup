using UnityEngine;

namespace Larje.ProjectSetup.Editor.Playtest
{
    internal static class SceneObjects
    {
        public static T[] All<T>() where T : Object
        {
#if UNITY_6000_4_OR_NEWER
            return Object.FindObjectsByType<T>();
#else
            return Object.FindObjectsByType<T>(FindObjectsSortMode.None);
#endif
        }
    }
}
