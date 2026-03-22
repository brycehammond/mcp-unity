using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Tools
{
    public class FocusGameObjectTool : McpToolBase
    {
        public FocusGameObjectTool()
        {
            Name = "focus_gameobject";
            Description = "Select a GameObject and frame it in the Scene view";
        }

        public override JObject Execute(JObject parameters)
        {
            var name = parameters["name"]?.ToString();

            if (string.IsNullOrEmpty(name))
            {
                return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    "'name' is required.", "validation_error");
            }

            var go = GameObject.Find(name);
            if (go == null)
            {
                return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    $"GameObject '{name}' not found.", "tool_execution_error");
            }

            Selection.activeGameObject = go;
            SceneView.FrameLastActiveSceneView();

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Focused on '{go.name}'."
            };
        }
    }
}
