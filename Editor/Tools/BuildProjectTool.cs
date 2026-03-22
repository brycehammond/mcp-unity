using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace McpUnity.Tools
{
    public class BuildProjectTool : McpToolBase
    {
        private static readonly Dictionary<string, BuildTarget> TargetMap = new Dictionary<string, BuildTarget>(StringComparer.OrdinalIgnoreCase)
        {
            {"windows", BuildTarget.StandaloneWindows64},
            {"win", BuildTarget.StandaloneWindows64},
            {"win64", BuildTarget.StandaloneWindows64},
            {"mac", BuildTarget.StandaloneOSX},
            {"osx", BuildTarget.StandaloneOSX},
            {"macos", BuildTarget.StandaloneOSX},
            {"linux", BuildTarget.StandaloneLinux64},
            {"webgl", BuildTarget.WebGL},
            {"android", BuildTarget.Android},
            {"ios", BuildTarget.iOS},
        };

        public BuildProjectTool()
        {
            Name = "build_project";
            Description = "Build the Unity project for a target platform";
        }

        public override JObject Execute(JObject parameters)
        {
            // Resolve build target
            var targetStr = parameters["target"]?.ToString();
            BuildTarget target;

            if (!string.IsNullOrEmpty(targetStr))
            {
                if (!TargetMap.TryGetValue(targetStr, out target))
                    return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                        $"Unknown build target '{targetStr}'. Use: Windows, Mac, Linux, WebGL, Android, or iOS.",
                        "validation_error");
            }
            else
            {
                target = EditorUserBuildSettings.activeBuildTarget;
            }

            // Gather enabled scenes
            var scenePaths = new List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                    scenePaths.Add(scene.path);
            }

            if (scenePaths.Count == 0)
                return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    "No scenes are enabled in Build Settings.", "tool_execution_error");

            // Resolve output path
            var outputPath = parameters["outputPath"]?.ToString();
            if (string.IsNullOrEmpty(outputPath))
            {
                var projectRoot = Path.GetDirectoryName(Application.dataPath);
                outputPath = Path.Combine(projectRoot, "Builds", target.ToString());
            }

            Directory.CreateDirectory(outputPath);

            // Build
            var report = BuildPipeline.BuildPlayer(scenePaths.ToArray(), outputPath, target, BuildOptions.None);
            var summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                var detail = $"Build succeeded for {target}. Output: {outputPath}. " +
                             $"Duration: {summary.totalTime.TotalSeconds:F1}s, Size: {summary.totalSize / (1024 * 1024):F1} MB, " +
                             $"Warnings: {summary.totalWarnings}, Errors: {summary.totalErrors}.";
                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = detail
                };
            }
            else
            {
                var detail = $"Build failed for {target}. Result: {summary.result}. " +
                             $"Errors: {summary.totalErrors}, Warnings: {summary.totalWarnings}.";
                return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(detail, "tool_execution_error");
            }
        }
    }
}
