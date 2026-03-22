using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Tools
{
    public class EditScriptTool : McpToolBase
    {
        public EditScriptTool()
        {
            Name = "edit_script";
            Description = "Replace the contents of an existing C# script";
        }

        public override JObject Execute(JObject parameters)
        {
            var path = parameters["path"]?.ToString();
            var className = parameters["className"]?.ToString();
            var code = parameters["code"]?.ToString();

            if (string.IsNullOrEmpty(path) && string.IsNullOrEmpty(className))
            {
                return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    "Either 'path' or 'className' must be provided.", "validation_error");
            }

            if (string.IsNullOrEmpty(code))
            {
                return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    "'code' is required.", "validation_error");
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

            try
            {
                var fullPath = Path.Combine(Application.dataPath, "..", path);
                fullPath = Path.GetFullPath(fullPath);

                if (!File.Exists(fullPath))
                {
                    return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                        $"File not found: {path}", "tool_execution_error");
                }

                File.WriteAllText(fullPath, code);
                AssetDatabase.ImportAsset(path);

                var lineCount = code.Split('\n').Length;

                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = $"Updated {path} ({lineCount} lines)",
                    ["data"] = new JObject
                    {
                        ["path"] = path,
                        ["lineCount"] = lineCount
                    }
                };
            }
            catch (Exception ex)
            {
                return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    $"Error writing file: {ex.Message}", "tool_execution_error");
            }
        }
    }
}
