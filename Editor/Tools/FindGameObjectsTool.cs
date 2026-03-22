using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace McpUnity.Tools
{
    public class FindGameObjectsTool : McpToolBase
    {
        public FindGameObjectsTool()
        {
            Name = "find_gameobjects";
            Description = "Find GameObjects in the active scene by name pattern, tag, or component type";
        }

        public override JObject Execute(JObject parameters)
        {
            var namePattern = parameters["namePattern"]?.ToString();
            var tag = parameters["tag"]?.ToString();
            var componentTypeName = parameters["componentType"]?.ToString();
            var includeInactive = parameters["includeInactive"]?.Value<bool>() ?? false;
            var maxResults = parameters["maxResults"]?.Value<int>() ?? 50;
            maxResults = Mathf.Clamp(maxResults, 1, 500);

            if (string.IsNullOrEmpty(namePattern) && string.IsNullOrEmpty(tag) && string.IsNullOrEmpty(componentTypeName))
                return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    "At least one filter criterion (namePattern, tag, or componentType) is required.", "validation_error");

            // Resolve component type once before traversal
            Type componentType = null;
            if (!string.IsNullOrEmpty(componentTypeName))
            {
                componentType = GameObjectToolUtils.FindComponentType(componentTypeName);
                if (componentType == null)
                    return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                        $"Component type '{componentTypeName}' not found.", "not_found_error");
            }

            // Build name matcher
            Regex nameRegex = null;
            if (!string.IsNullOrEmpty(namePattern) && (namePattern.Contains("*") || namePattern.Contains("?")))
            {
                var escaped = Regex.Escape(namePattern).Replace("\\*", ".*").Replace("\\?", ".");
                nameRegex = new Regex($"^{escaped}$", RegexOptions.IgnoreCase);
            }

            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            var results = new List<JObject>();

            foreach (var root in roots)
            {
                if (results.Count >= maxResults) break;
                CollectMatches(root, namePattern, nameRegex, tag, componentType, includeInactive, maxResults, results);
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Found {results.Count} matching GameObject(s).",
                ["data"] = new JObject
                {
                    ["results"] = new JArray(results),
                    ["totalFound"] = results.Count,
                    ["capped"] = results.Count >= maxResults
                }
            };
        }

        private static void CollectMatches(GameObject go, string namePattern, Regex nameRegex, string tag,
            Type componentType, bool includeInactive, int maxResults, List<JObject> results)
        {
            if (results.Count >= maxResults) return;

            if (!includeInactive && !go.activeInHierarchy) return;

            bool matches = true;

            // Name filter
            if (!string.IsNullOrEmpty(namePattern))
            {
                if (nameRegex != null)
                    matches = nameRegex.IsMatch(go.name);
                else
                    matches = go.name.IndexOf(namePattern, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            // Tag filter
            if (matches && !string.IsNullOrEmpty(tag))
            {
                try { matches = go.CompareTag(tag); }
                catch { matches = false; }
            }

            // Component filter
            if (matches && componentType != null)
                matches = go.GetComponent(componentType) != null;

            if (matches)
            {
                var compNames = new List<string>();
                foreach (var comp in go.GetComponents<Component>())
                {
                    if (comp == null) continue;
                    var typeName = comp.GetType().Name;
                    if (typeName != "Transform" && typeName != "RectTransform")
                        compNames.Add(typeName);
                }

                results.Add(new JObject
                {
                    ["name"] = go.name,
                    ["path"] = GameObjectToolUtils.GetGameObjectPath(go),
                    ["instanceId"] = go.GetInstanceID(),
                    ["active"] = go.activeInHierarchy,
                    ["components"] = new JArray(compNames)
                });
            }

            // Recurse children
            for (int i = 0; i < go.transform.childCount; i++)
            {
                if (results.Count >= maxResults) return;
                CollectMatches(go.transform.GetChild(i).gameObject, namePattern, nameRegex, tag,
                    componentType, includeInactive, maxResults, results);
            }
        }
    }
}
