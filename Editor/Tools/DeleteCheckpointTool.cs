using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace McpUnity.Tools
{
    public class DeleteCheckpointTool : McpToolBase
    {
        public DeleteCheckpointTool()
        {
            Name = "delete_checkpoint";
            Description = "Delete a saved checkpoint by name, optionally removing its scene file";
        }

        public override JObject Execute(JObject parameters)
        {
            var name = parameters["name"]?.ToString();
            var deleteSceneFile = parameters["deleteSceneFile"]?.Value<bool>() ?? true;

            if (string.IsNullOrEmpty(name))
                return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    "'name' is required.", "validation_error");

            var manifest = SaveCheckpointTool.LoadManifest();

            var target = manifest.FirstOrDefault(e =>
                string.Equals(e["name"]?.ToString(), name, StringComparison.OrdinalIgnoreCase));

            if (target == null)
                return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    $"Checkpoint '{name}' not found.", "not_found_error");

            var scenePath = target["scenePath"]?.ToString();

            // Remove manifest entry first (more important for correctness)
            target.Remove();
            SaveCheckpointTool.SaveManifest(manifest);

            // Optionally delete the scene file
            bool sceneFileDeleted = false;
            if (deleteSceneFile && !string.IsNullOrEmpty(scenePath))
            {
                sceneFileDeleted = AssetDatabase.DeleteAsset(scenePath);
                if (!sceneFileDeleted)
                    McpUnity.Utils.McpLogger.LogWarning($"Checkpoint removed from manifest but scene file could not be deleted: {scenePath}");
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Deleted checkpoint '{name}'." + (sceneFileDeleted ? $" Scene file removed: {scenePath}" : ""),
                ["data"] = new JObject
                {
                    ["name"] = name,
                    ["sceneFileDeleted"] = sceneFileDeleted
                }
            };
        }
    }
}
