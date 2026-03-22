using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace McpUnity.Tools
{
    public class ExplainSceneTool : McpToolBase
    {
        public ExplainSceneTool()
        {
            Name = "explain_scene";
            Description = "Explain what the current scene would do at runtime";
        }

        public override JObject Execute(JObject parameters)
        {
            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();

            if (roots.Length == 0)
            {
                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = "The scene is empty — nothing would happen on play."
                };
            }

            int totalObjects = 0;
            int rigidbodyCount = 0;
            int colliderCount = 0;
            int customScriptCount = 0;
            var customScriptNames = new List<string>();
            var rigidbodyNames = new List<string>();
            var colliderNames = new List<string>();

            foreach (var root in roots)
            {
                AnalyzeRecursive(root, ref totalObjects, ref rigidbodyCount, ref colliderCount,
                    ref customScriptCount, customScriptNames, rigidbodyNames, colliderNames);
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Scene: {scene.name}");
            sb.AppendLine($"Total objects: {totalObjects}");
            sb.AppendLine();

            // Physics
            sb.AppendLine($"Objects with Rigidbody ({rigidbodyCount}): {(rigidbodyCount > 0 ? string.Join(", ", rigidbodyNames) : "none")}");
            if (rigidbodyCount > 0)
                sb.AppendLine("  → These objects will be affected by gravity and physics forces on play.");
            sb.AppendLine();

            // Colliders
            sb.AppendLine($"Objects with Colliders ({colliderCount}): {(colliderCount > 0 ? string.Join(", ", colliderNames) : "none")}");
            if (colliderCount > 0)
                sb.AppendLine("  → These objects can interact with physics and detect collisions.");
            sb.AppendLine();

            // Custom scripts
            sb.AppendLine($"Custom scripts ({customScriptCount}):");
            if (customScriptNames.Count > 0)
            {
                foreach (var scriptName in customScriptNames)
                    sb.AppendLine($"  - {scriptName}");
                sb.AppendLine("  → These scripts contain custom behavior that will run on play.");
            }
            else
            {
                sb.AppendLine("  none");
            }

            // Gameplay summary
            sb.AppendLine();
            sb.Append("Gameplay summary: ");
            if (rigidbodyCount > 0 && customScriptCount > 0)
                sb.AppendLine("On play, physics objects will fall/move under gravity, colliders will detect interactions, and custom scripts will drive gameplay behavior.");
            else if (rigidbodyCount > 0)
                sb.AppendLine("On play, physics objects will fall/move under gravity and colliders will handle interactions. No custom scripts are present, so there is no scripted behavior.");
            else if (customScriptCount > 0)
                sb.AppendLine("On play, custom scripts will drive behavior. No physics objects are present, so nothing will move unless scripts move it.");
            else
                sb.AppendLine("On play, nothing would move or happen — there are no physics objects or custom scripts.");

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = sb.ToString()
            };
        }

        private void AnalyzeRecursive(GameObject go, ref int totalObjects, ref int rigidbodyCount,
            ref int colliderCount, ref int customScriptCount, List<string> customScriptNames,
            List<string> rigidbodyNames, List<string> colliderNames)
        {
            totalObjects++;

            if (go.GetComponent<Rigidbody>() != null || go.GetComponent<Rigidbody2D>() != null)
            {
                rigidbodyCount++;
                rigidbodyNames.Add(go.name);
            }

            if (go.GetComponent<Collider>() != null || go.GetComponent<Collider2D>() != null)
            {
                colliderCount++;
                colliderNames.Add(go.name);
            }

            foreach (var comp in go.GetComponents<MonoBehaviour>())
            {
                if (comp == null) continue;
                var typeName = comp.GetType().Name;
                // Skip known Unity/plugin behaviours
                if (typeName.StartsWith("UnityEngine.")) continue;
                customScriptCount++;
                var entry = $"{typeName} (on {go.name})";
                if (!customScriptNames.Contains(entry))
                    customScriptNames.Add(entry);
            }

            for (int i = 0; i < go.transform.childCount; i++)
            {
                AnalyzeRecursive(go.transform.GetChild(i).gameObject, ref totalObjects,
                    ref rigidbodyCount, ref colliderCount, ref customScriptCount,
                    customScriptNames, rigidbodyNames, colliderNames);
            }
        }
    }
}
