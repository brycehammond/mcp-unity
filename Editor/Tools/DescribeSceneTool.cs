using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace McpUnity.Tools
{
    public class DescribeSceneTool : McpToolBase
    {
        public DescribeSceneTool()
        {
            Name = "describe_scene";
            Description = "Describe the current scene hierarchy in natural language";
        }

        public override JObject Execute(JObject parameters)
        {
            var maxDepth = parameters["maxDepth"]?.Value<int>() ?? 3;
            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();

            if (roots.Length == 0)
            {
                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = "The scene is empty."
                };
            }

            var sb = new StringBuilder();

            sb.AppendLine($"Scene: {scene.name}");
            sb.AppendLine($"Root objects: {roots.Length}");
            sb.AppendLine();

            foreach (var root in roots)
            {
                DescribeObject(root, sb, 0, maxDepth);
                sb.AppendLine();
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = sb.ToString()
            };
        }

        private void DescribeObject(GameObject go, StringBuilder sb, int depth, int maxDepth)
        {
            var indent = new string(' ', depth * 2);
            var activeStr = go.activeSelf ? "" : " [inactive]";
            sb.Append($"{indent}- {go.name}{activeStr}");

            // List key components (skip Transform as everything has it)
            var components = go.GetComponents<Component>();
            var compNames = new List<string>();
            foreach (var comp in components)
            {
                if (comp == null) continue;
                var typeName = comp.GetType().Name;
                if (typeName == "Transform" || typeName == "RectTransform") continue;
                compNames.Add(typeName);
            }

            if (compNames.Count > 0)
                sb.Append($" ({string.Join(", ", compNames)})");

            // Position info
            var pos = go.transform.position;
            sb.AppendLine($" at ({pos.x:F1}, {pos.y:F1}, {pos.z:F1})");

            // Recurse children
            if (depth < maxDepth)
            {
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    DescribeObject(go.transform.GetChild(i).gameObject, sb, depth + 1, maxDepth);
                }
            }
            else if (go.transform.childCount > 0)
            {
                sb.AppendLine($"{indent}  ... {go.transform.childCount} children");
            }
        }

    }
}
