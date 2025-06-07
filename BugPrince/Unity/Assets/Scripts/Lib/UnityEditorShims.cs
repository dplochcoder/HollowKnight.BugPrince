using UnityEngine;

namespace BugPrince.Scripts.Lib
{
    public static class UnityEditorShims
    {
#if UNITY_EDITOR
        [UnityEditor.MenuItem("CONTEXT/BoxCollider2D/Snap1")]
        public static void SnapBox(UnityEditor.MenuCommand command) => MathExt.Snap(command.context as BoxCollider2D, 1f);

        [UnityEditor.MenuItem("CONTEXT/PolygonCollider2D/Snap1")]
        public static void SnapPolygon(UnityEditor.MenuCommand command) => MathExt.Snap(command.context as PolygonCollider2D, 1f);

        [UnityEditor.MenuItem("CONTEXT/BoxCollider2D/Snap0.5")]
        public static void SnapBoxHalf(UnityEditor.MenuCommand command) => MathExt.Snap(command.context as BoxCollider2D, 0.5f);

        [UnityEditor.MenuItem("CONTEXT/PolygonCollider2D/Snap0.5")]
        public static void SnapPolygonHalf(UnityEditor.MenuCommand command) => MathExt.Snap(command.context as PolygonCollider2D, 0.5f);

        [UnityEditor.MenuItem("CONTEXT/Transform/Reset Zero")]
        public static void ResetZero(UnityEditor.MenuCommand command) => MathExt.ResetZero(command.context as Transform);
#endif

        public static string GetAssetPath(Object obj)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.GetAssetPath(obj);
#else
            return "";
#endif
        }

        public static T LoadAssetAtPath<T>(string path) where T : class
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(T)) as T;
#else
            return null;
#endif
        }

        public static void MarkActiveSceneDirty()
        {
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
#endif
        }

        public static void MarkDirty(Object obj)
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(obj);
#endif
        }
    }
}