using Newtonsoft.Json.Linq;
using UnityEditor;

namespace McpUnity.Tools
{
    public class DeleteScriptTool : McpToolBase
    {
        public DeleteScriptTool()
        {
            Name = "delete_script";
            Description = "Delete a C# script from the project by path or class name";
        }

        public override JObject Execute(JObject parameters)
        {
            var path = parameters["path"]?.ToString();
            var className = parameters["className"]?.ToString();

            if (string.IsNullOrEmpty(path) && string.IsNullOrEmpty(className))
                return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    "Either 'path' or 'className' must be provided.", "validation_error");

            if (string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(className))
            {
                path = ScriptToolUtils.FindScriptPathByClassName(className);
                if (string.IsNullOrEmpty(path))
                    return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                        $"Could not find a script with class name '{className}'.", "not_found_error");
            }

            if (AssetDatabase.LoadAssetAtPath<MonoScript>(path) == null)
                return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    $"Script not found at '{path}'.", "not_found_error");

            if (!AssetDatabase.DeleteAsset(path))
                return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    $"Failed to delete script at '{path}'.", "tool_execution_error");

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Deleted script at '{path}'."
            };
        }
    }
}
