using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Tools
{
    public class CreateScriptTool : McpToolBase
    {
        public CreateScriptTool()
        {
            Name = "create_script";
            Description = "Create a new C# script file and optionally attach it to a GameObject";
        }

        public override JObject Execute(JObject parameters)
        {
            var scriptName = parameters["scriptName"]?.ToString();
            var scriptType = parameters["scriptType"]?.ToString() ?? "MonoBehaviour";
            var code = parameters["code"]?.ToString();
            var directory = parameters["directory"]?.ToString() ?? "Assets/Scripts";
            var attachTo = parameters["attachTo"]?.ToString();

            if (string.IsNullOrEmpty(scriptName))
                return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse("Script name is required.", "validation_error");

            // Generate template code if none provided
            if (string.IsNullOrEmpty(code))
                code = GenerateTemplate(scriptName, scriptType);

            try
            {
                // Ensure directory exists
                var fullDir = Path.Combine(Application.dataPath, "..", directory);
                fullDir = Path.GetFullPath(fullDir);
                if (!Directory.Exists(fullDir))
                    Directory.CreateDirectory(fullDir);

                var filePath = Path.Combine(directory, $"{scriptName}.cs");
                var fullPath = Path.Combine(Application.dataPath, "..", filePath);
                fullPath = Path.GetFullPath(fullPath);

                File.WriteAllText(fullPath, code);
                AssetDatabase.ImportAsset(filePath);

                var detail = $"Created script '{scriptName}.cs' at {filePath}";

                // Attach to GameObject if specified
                if (!string.IsNullOrEmpty(attachTo))
                {
                    var go = GameObject.Find(attachTo);
                    if (go != null)
                    {
                        // We need to wait for compilation, so we'll try to add it
                        // Note: The script may not be compiled yet right after creation
                        var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(filePath);
                        if (monoScript != null)
                        {
                            var scriptClass = monoScript.GetClass();
                            if (scriptClass != null && typeof(MonoBehaviour).IsAssignableFrom(scriptClass))
                            {
                                Undo.AddComponent(go, scriptClass);
                                detail += $", attached to '{attachTo}'";
                            }
                            else
                            {
                                detail += $". Note: could not attach yet — Unity may still be compiling.";
                            }
                        }
                        else
                        {
                            detail += $". Note: Script needs to compile before attaching to '{attachTo}'.";
                        }
                    }
                    else
                    {
                        detail += $". Warning: could not find GameObject '{attachTo}'";
                    }
                }

                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = detail
                };
            }
            catch (Exception ex)
            {
                return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    $"Failed to create script: {ex.Message}", "tool_execution_error");
            }
        }

        private string GenerateTemplate(string className, string scriptType)
        {
            switch (scriptType)
            {
                case "ScriptableObject":
                    return $@"using UnityEngine;

[CreateAssetMenu(fileName = ""{className}"", menuName = ""Custom/{className}"")]
public class {className} : ScriptableObject
{{

}}
";
                case "EditorWindow":
                    return $@"using UnityEditor;
using UnityEngine;

public class {className} : EditorWindow
{{
    [MenuItem(""Window/{className}"")]
    public static void ShowWindow()
    {{
        GetWindow<{className}>(""{className}"");
    }}

    private void OnGUI()
    {{

    }}
}}
";
                case "Plain":
                    return $@"using System;

public class {className}
{{

}}
";
                case "MonoBehaviour":
                default:
                    return $@"using UnityEngine;

public class {className} : MonoBehaviour
{{
    void Start()
    {{

    }}

    void Update()
    {{

    }}
}}
";
            }
        }
    }
}
