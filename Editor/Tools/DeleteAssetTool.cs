using Newtonsoft.Json.Linq;
using UnityEditor;

namespace McpUnity.Tools
{
    public class DeleteAssetTool : McpToolBase
    {
        public DeleteAssetTool()
        {
            Name = "delete_asset";
            Description = "Delete an asset from the Unity project by its asset path";
        }

        public override JObject Execute(JObject parameters)
        {
            var assetPath = parameters["assetPath"]?.ToString();

            if (string.IsNullOrEmpty(assetPath))
                return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    "'assetPath' is required.", "validation_error");

            if (!assetPath.StartsWith("Assets/"))
                return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    "assetPath must start with 'Assets/'.", "validation_error");

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) == null)
                return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    $"Asset not found at '{assetPath}'.", "not_found_error");

            if (!AssetDatabase.DeleteAsset(assetPath))
                return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    $"Failed to delete asset at '{assetPath}'.", "tool_execution_error");

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Deleted asset at '{assetPath}'."
            };
        }
    }
}
