using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Tools
{
    public class ImportAssetTool : McpToolBase
    {
        public ImportAssetTool()
        {
            Name = "import_asset";
            Description = "Copy a downloaded file into the Unity project and import it via AssetDatabase";
        }

        public override JObject Execute(JObject parameters)
        {
            var sourcePath = parameters["sourcePath"]?.ToString();
            var targetDirectory = parameters["targetDirectory"]?.ToString() ?? "Assets/Imports";
            var fileName = parameters["fileName"]?.ToString();

            if (string.IsNullOrEmpty(sourcePath))
                return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    "sourcePath is required.", "validation_error");

            if (!File.Exists(sourcePath))
                return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    $"Source file not found: {sourcePath}", "tool_execution_error");

            if (string.IsNullOrEmpty(fileName))
                fileName = Path.GetFileName(sourcePath);

            try
            {
                // Ensure target directory exists
                var fullDir = Path.Combine(Application.dataPath, "..", targetDirectory);
                fullDir = Path.GetFullPath(fullDir);
                if (!Directory.Exists(fullDir))
                    Directory.CreateDirectory(fullDir);

                var assetPath = Path.Combine(targetDirectory, fileName);
                var fullPath = Path.Combine(Application.dataPath, "..", assetPath);
                fullPath = Path.GetFullPath(fullPath);

                // Copy file from temp to project
                File.Copy(sourcePath, fullPath, overwrite: true);

                // Import via AssetDatabase
                AssetDatabase.ImportAsset(assetPath);

                // Check if glTF support is needed
                var ext = Path.GetExtension(fileName).ToLowerInvariant();
                var warning = "";
                if (ext == ".gltf" || ext == ".glb")
                {
                    if (!IsPackageInstalled("com.unity.cloud.gltfast"))
                    {
                        warning = "\n\nNote: This is a glTF file. You may need to install the glTFast package " +
                                  "for Unity to import it correctly. Use add_package(identifier=\"com.unity.cloud.gltfast\").";
                    }
                }

                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = $"Imported '{fileName}' to {assetPath}{warning}"
                };
            }
            catch (Exception ex)
            {
                return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    $"Failed to import asset: {ex.Message}", "tool_execution_error");
            }
        }

        private static bool IsPackageInstalled(string packageName)
        {
            var manifestPath = Path.Combine(Application.dataPath, "..", "Packages", "manifest.json");
            if (!File.Exists(manifestPath)) return false;

            try
            {
                var json = File.ReadAllText(manifestPath);
                return json.Contains($"\"{packageName}\"");
            }
            catch
            {
                return false;
            }
        }
    }
}
