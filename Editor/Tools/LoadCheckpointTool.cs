using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace McpUnity.Tools
{
    public class LoadCheckpointTool : McpToolBase
    {
        public LoadCheckpointTool()
        {
            Name = "load_checkpoint";
            Description = "Load a previously saved project state by name. Auto-saves current state first. If no name is given, loads the most recent checkpoint.";
            IsAsync = true;
        }

        public override async void ExecuteAsync(JObject parameters, TaskCompletionSource<JObject> tcs)
        {
            var name = parameters["name"]?.ToString();
            var manifest = SaveCheckpointTool.LoadManifest();

            if (manifest.Count == 0)
            {
                tcs.SetResult(McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    "No checkpoints found.", "tool_execution_error"));
                return;
            }

            // Find the target save
            JToken targetSave;
            if (string.IsNullOrEmpty(name))
            {
                targetSave = manifest.Last;
            }
            else
            {
                targetSave = manifest.FirstOrDefault(e =>
                    string.Equals(e["name"]?.ToString(), name, StringComparison.OrdinalIgnoreCase));
            }

            if (targetSave == null)
            {
                tcs.SetResult(McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    $"Checkpoint '{name}' not found.", "tool_execution_error"));
                return;
            }

            var targetName = targetSave["name"]?.ToString();
            var commitHash = targetSave["commitHash"]?.ToString();
            var scenePath = targetSave["scenePath"]?.ToString();

            // Auto-save current state first
            try
            {
                var autoSaveTcs = new TaskCompletionSource<JObject>();
                var autoSaveTool = new SaveCheckpointTool();
                var autoSaveParams = new JObject
                {
                    ["name"] = "autosave before load",
                    ["description"] = $"Automatic save before loading '{targetName}'"
                };
                autoSaveTool.ExecuteAsync(autoSaveParams, autoSaveTcs);
                var autoSaveResult = await autoSaveTcs.Task;

                if (autoSaveResult["success"]?.Value<bool>() != true)
                {
                    McpUnity.Utils.McpLogger.LogWarning($"Auto-save before load failed: {autoSaveResult["message"]}");
                    // Continue anyway - loading is more important
                }
            }
            catch (Exception ex)
            {
                McpUnity.Utils.McpLogger.LogWarning($"Auto-save before load failed: {ex.Message}");
            }

            // Restore git state if we have a valid commit hash
            if (!string.IsNullOrEmpty(commitHash) && commitHash != "none")
            {
                try
                {
                    await Task.Run(() =>
                    {
                        var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                        SaveCheckpointTool.RunGit($"checkout {commitHash} -- .", projectRoot);
                    });
                }
                catch (Exception ex)
                {
                    tcs.SetResult(McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                        $"Failed to restore git state: {ex.Message}", "tool_execution_error"));
                    return;
                }
            }

            // Open the saved scene
            try
            {
                AssetDatabase.Refresh();

                if (!string.IsNullOrEmpty(scenePath) && File.Exists(
                    Path.Combine(Application.dataPath, "..", scenePath)))
                {
                    EditorSceneManager.OpenScene(scenePath);
                }
            }
            catch (Exception ex)
            {
                tcs.SetResult(McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    $"Restored files but failed to open scene: {ex.Message}", "tool_execution_error"));
                return;
            }

            tcs.SetResult(new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Loaded checkpoint '{targetName}' (commit: {commitHash})",
                ["data"] = new JObject { ["name"] = targetName, ["commitHash"] = commitHash, ["scenePath"] = scenePath }
            });
        }
    }
}
