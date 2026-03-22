using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Tools
{
    public class ReadScriptTool : McpToolBase
    {
        public ReadScriptTool()
        {
            Name = "read_script";
            Description = "Read the contents of a C# script by path or class name";
        }

        public override JObject Execute(JObject parameters)
        {
            var path = parameters["path"]?.ToString();
            var className = parameters["className"]?.ToString();

            if (string.IsNullOrEmpty(path) && string.IsNullOrEmpty(className))
            {
                return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    "Either 'path' or 'className' must be provided.", "validation_error");
            }

            // Find by class name if no path given
            if (string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(className))
            {
                path = ScriptToolUtils.FindScriptPathByClassName(className);
                if (string.IsNullOrEmpty(path))
                {
                    return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                        $"Could not find a script with class name '{className}'.", "tool_execution_error");
                }
            }

            // Read the file
            try
            {
                var fullPath = Path.Combine(Application.dataPath, "..", path);
                fullPath = Path.GetFullPath(fullPath);

                if (!File.Exists(fullPath))
                {
                    return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                        $"File not found: {path}", "tool_execution_error");
                }

                var contents = File.ReadAllText(fullPath);
                var lineCount = contents.Split('\n').Length;

                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = $"Read {path} ({lineCount} lines)",
                    ["data"] = new JObject
                    {
                        ["path"] = path,
                        ["contents"] = contents,
                        ["lineCount"] = lineCount
                    }
                };
            }
            catch (Exception ex)
            {
                return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    $"Error reading file: {ex.Message}", "tool_execution_error");
            }
        }
    }
}
