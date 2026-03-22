using UnityEditor;

namespace McpUnity.Tools
{
    internal static class ScriptToolUtils
    {
        /// <summary>
        /// Resolve a class name to its asset path via AssetDatabase.
        /// Returns null if no matching MonoScript is found.
        /// </summary>
        internal static string FindScriptPathByClassName(string className)
        {
            var guids = AssetDatabase.FindAssets($"t:MonoScript {className}");
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
                if (script != null && script.name == className)
                {
                    return assetPath;
                }
            }
            return null;
        }
    }
}
