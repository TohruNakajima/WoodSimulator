using UnityEditor;
using UnityEngine;

namespace SmartCreator.ProceduralTrees.Editor
{
    public static class ProceduralTreeMenu
    {
        [MenuItem("GameObject/Procedural Trees/New Tree", false, 10)]
        public static void NewTree()
        {
            var go = new GameObject("Procedural Tree");
            var generator = go.AddComponent<TreeGenerator>();

            // Prevent immediate live updates from causing regen
            generator.liveUpdate = false;

            // Defer Regenerate to avoid undo stack overflow
            EditorApplication.delayCall += () =>
            {
                if (generator != null && generator.profile != null)
                {
                    generator.Regenerate();
                }
            };

            Selection.activeGameObject = go;
        }
    }
}
