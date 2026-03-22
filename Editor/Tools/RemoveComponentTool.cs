using System;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Tools
{
    public class RemoveComponentTool : McpToolBase
    {
        public RemoveComponentTool()
        {
            Name = "remove_component";
            Description = "Remove a component from a GameObject by type name";
        }

        public override JObject Execute(JObject parameters)
        {
            int? instanceId = parameters["instanceId"]?.ToObject<int?>();
            string objectPath = parameters["objectPath"]?.ToObject<string>();
            string componentName = parameters["componentName"]?.ToObject<string>();

            if (string.IsNullOrEmpty(componentName))
                return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    "'componentName' is required.", "validation_error");

            if (componentName == "Transform" || componentName == "RectTransform")
                return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    "Transform components cannot be removed.", "validation_error");

            JObject error = GameObjectToolUtils.FindGameObject(instanceId, objectPath, out GameObject gameObject, out string identifierInfo);
            if (error != null) return error;

            Type componentType = GameObjectToolUtils.FindComponentType(componentName);
            if (componentType == null)
                return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    $"Component type '{componentName}' not found.", "not_found_error");

            Component component = gameObject.GetComponent(componentType);
            if (component == null)
                return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    $"GameObject '{gameObject.name}' does not have a '{componentName}' component.", "not_found_error");

            try
            {
                Undo.DestroyObjectImmediate(component);
                EditorUtility.SetDirty(gameObject);
            }
            catch (Exception ex)
            {
                return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    $"Cannot remove '{componentName}': {ex.Message}", "tool_execution_error");
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Removed '{componentName}' from '{gameObject.name}'."
            };
        }
    }
}
