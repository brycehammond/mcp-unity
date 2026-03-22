using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Tools
{
    public class CreateGameObjectTool : McpToolBase
    {
        private static readonly Dictionary<string, Color> NamedColors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
        {
            {"red", Color.red}, {"blue", Color.blue}, {"green", Color.green},
            {"yellow", Color.yellow}, {"white", Color.white}, {"black", Color.black},
            {"cyan", Color.cyan}, {"magenta", Color.magenta}, {"gray", Color.gray},
            {"grey", Color.gray},
            {"orange", new Color(1f, 0.647f, 0f)},
            {"purple", new Color(0.5f, 0f, 0.5f)},
            {"brown", new Color(0.647f, 0.165f, 0.165f)},
            {"pink", new Color(1f, 0.753f, 0.796f)}
        };

        public CreateGameObjectTool()
        {
            Name = "create_gameobject";
            Description = "Create a new GameObject in the scene (supports 3D primitives, 2D sprites, components, and colors)";
        }

        public override JObject Execute(JObject parameters)
        {
            var name = parameters["name"]?.ToString() ?? "GameObject";
            var primitiveType = parameters["primitiveType"]?.ToString();
            var colorStr = parameters["color"]?.ToString();
            var parentPath = parameters["parentPath"]?.ToString();
            var sortingLayer = parameters["sortingLayer"]?.ToString();
            var sortingOrder = parameters["sortingOrder"]?.Value<int>() ?? 0;

            GameObject go;

            if (string.Equals(primitiveType, "Sprite", StringComparison.OrdinalIgnoreCase))
            {
                go = new GameObject(name);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = CreateDefaultSprite();

                if (!string.IsNullOrEmpty(sortingLayer))
                    sr.sortingLayerName = sortingLayer;
                sr.sortingOrder = sortingOrder;
            }
            else if (!string.IsNullOrEmpty(primitiveType) && Enum.TryParse<PrimitiveType>(primitiveType, true, out var pt))
            {
                go = GameObject.CreatePrimitive(pt);
                go.name = name;
            }
            else
            {
                go = new GameObject(name);
            }

            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");

            if (parameters["position"] is JObject pos)
            {
                go.transform.position = new Vector3(
                    pos["x"]?.Value<float>() ?? 0,
                    pos["y"]?.Value<float>() ?? 0,
                    pos["z"]?.Value<float>() ?? 0
                );
            }

            if (parameters["rotation"] is JObject rot)
            {
                go.transform.eulerAngles = new Vector3(
                    rot["x"]?.Value<float>() ?? 0,
                    rot["y"]?.Value<float>() ?? 0,
                    rot["z"]?.Value<float>() ?? 0
                );
            }

            if (parameters["scale"] is JObject scl)
            {
                go.transform.localScale = new Vector3(
                    scl["x"]?.Value<float>() ?? 1,
                    scl["y"]?.Value<float>() ?? 1,
                    scl["z"]?.Value<float>() ?? 1
                );
            }

            if (!string.IsNullOrEmpty(parentPath))
            {
                var parent = GameObject.Find(parentPath);
                if (parent != null)
                {
                    Undo.SetTransformParent(go.transform, parent.transform, $"Parent {name}");
                }
            }

            if (!string.IsNullOrEmpty(colorStr))
            {
                var color = ParseColor(colorStr);
                ApplyColor(go, color);
            }

            if (parameters["components"] is JArray components)
            {
                foreach (var comp in components)
                {
                    var typeName = comp["type"]?.ToString();
                    if (string.IsNullOrEmpty(typeName)) continue;

                    var componentType = FindComponentType(typeName);
                    if (componentType != null)
                    {
                        Undo.AddComponent(go, componentType);
                    }
                }
            }

            var posStr = $"{go.transform.position.x}, {go.transform.position.y}, {go.transform.position.z}";
            var typeStr = !string.IsNullOrEmpty(primitiveType) ? primitiveType.ToLower() : "empty object";
            var colorPart = !string.IsNullOrEmpty(colorStr) ? $", color: {colorStr}" : "";

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Created GameObject '{name}' (type: {typeStr}) at ({posStr}){colorPart}"
            };
        }

        private static Color ParseColor(string colorStr)
        {
            if (NamedColors.TryGetValue(colorStr, out var namedColor))
                return namedColor;

            if (ColorUtility.TryParseHtmlString(colorStr, out var parsed))
                return parsed;
            if (ColorUtility.TryParseHtmlString("#" + colorStr, out var parsed2))
                return parsed2;

            return Color.white;
        }

        private static void ApplyColor(GameObject go, Color color)
        {
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Undo.RecordObject(sr, "Set sprite color");
                sr.color = color;
                return;
            }

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                Undo.RecordObject(mr, "Set material color");
                mr.sharedMaterial = mat;
            }
        }

        private static Sprite CreateDefaultSprite()
        {
            var tex = new Texture2D(4, 4);
            var pixels = new Color[16];
            for (int i = 0; i < 16; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        }

        private static Type FindComponentType(string typeName)
        {
            var type = Type.GetType($"UnityEngine.{typeName}, UnityEngine");
            if (type != null) return type;

            type = Type.GetType($"UnityEngine.UI.{typeName}, UnityEngine.UI");
            if (type != null) return type;

            type = Type.GetType(typeName);
            if (type != null) return type;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(typeName);
                if (type != null) return type;

                type = asm.GetType($"UnityEngine.{typeName}");
                if (type != null) return type;
            }

            return null;
        }
    }
}
