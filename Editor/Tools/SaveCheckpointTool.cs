using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace McpUnity.Tools
{
    public class SaveCheckpointTool : McpToolBase
    {
        public SaveCheckpointTool()
        {
            Name = "save_checkpoint";
            Description = "Save the current scene and project state with a name and optional description. Creates a git commit as a restore point.";
            IsAsync = true;
        }

        public override async void ExecuteAsync(JObject parameters, TaskCompletionSource<JObject> tcs)
        {
            var name = parameters["name"]?.ToString();
            var description = parameters["description"]?.ToString();

            // Load manifest once and reuse throughout
            var manifest = LoadManifest();

            // Determine save name
            if (string.IsNullOrEmpty(name))
            {
                var counter = manifest.Count(e => e["name"]?.ToString()?.StartsWith("Save ") == true);
                name = $"Save {counter + 1}";
            }

            var sanitizedName = SanitizeFileName(name);
            var savePath = $"Assets/_KilnSaves/{sanitizedName}.unity";

            // Save scene on main thread
            try
            {
                // Ensure _KilnSaves directory exists
                var saveDir = Path.Combine(Application.dataPath, "_KilnSaves");
                if (!Directory.Exists(saveDir))
                    Directory.CreateDirectory(saveDir);

                // Save the active scene to the save path
                var scene = SceneManager.GetActiveScene();
                var success = EditorSceneManager.SaveScene(scene, savePath);
                if (!success)
                {
                    tcs.SetResult(McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                        "Failed to save scene.", "tool_execution_error"));
                    return;
                }

                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                tcs.SetResult(McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    $"Scene save failed: {ex.Message}", "tool_execution_error"));
                return;
            }

            // Git commit (runs on background thread via Task.Run)
            string commitHash;
            try
            {
                commitHash = await Task.Run(() =>
                {
                    var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                    RunGit("add -A", projectRoot);
                    RunGit($"commit -m \"MCP Unity checkpoint: {name}\"", projectRoot);
                    return RunGit("rev-parse HEAD", projectRoot).Trim();
                });
            }
            catch (Exception ex)
            {
                McpUnity.Utils.McpLogger.LogWarning($"Git commit failed: {ex.Message}");
                commitHash = "none";
            }

            // Update manifest
            try
            {
                var entry = new JObject
                {
                    ["name"] = name,
                    ["description"] = description ?? "",
                    ["timestamp"] = DateTime.UtcNow.ToString("o"),
                    ["scenePath"] = savePath,
                    ["commitHash"] = commitHash
                };
                manifest.Add(entry);
                SaveManifest(manifest);
            }
            catch (Exception ex)
            {
                tcs.SetResult(McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    $"Saved scene but failed to update manifest: {ex.Message}", "tool_execution_error"));
                return;
            }

            tcs.SetResult(new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Saved '{name}' at {savePath} (commit: {commitHash})",
                ["data"] = new JObject { ["name"] = name, ["commitHash"] = commitHash, ["scenePath"] = savePath }
            });
        }

        internal static JArray LoadManifest()
        {
            var manifestPath = GetManifestPath();
            if (File.Exists(manifestPath))
            {
                var json = File.ReadAllText(manifestPath);
                return JArray.Parse(json);
            }
            return new JArray();
        }

        internal static void SaveManifest(JArray manifest)
        {
            var manifestPath = GetManifestPath();
            var dir = Path.GetDirectoryName(manifestPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(manifestPath, manifest.ToString(Formatting.Indented));
        }

        internal static string GetManifestPath()
        {
            return Path.Combine(Application.dataPath, "_KilnSaves", "manifest.json");
        }

        internal static string RunGit(string arguments, string workingDirectory)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                    throw new Exception($"git {arguments} failed (exit {process.ExitCode}): {error}");

                return output;
            }
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = new string(name.Select(c => Array.IndexOf(invalid, c) >= 0 ? '_' : c).ToArray());
            return sanitized;
        }
    }
}
