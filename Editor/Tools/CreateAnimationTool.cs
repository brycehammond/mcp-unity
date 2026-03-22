using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Tools
{
    public class CreateAnimationTool : McpToolBase
    {
        // Maps friendly names to (Unity property path prefix, component type, channel names)
        private static readonly Dictionary<string, (string pathPrefix, Type componentType, string[] channels)> PropertyMap =
            new Dictionary<string, (string, Type, string[])>(StringComparer.OrdinalIgnoreCase)
            {
                { "position", ("localPosition", typeof(Transform), new[] { "x", "y", "z" }) },
                { "rotation", ("localEulerAnglesRaw", typeof(Transform), new[] { "x", "y", "z" }) },
                { "scale",    ("localScale", typeof(Transform), new[] { "x", "y", "z" }) },
            };

        public CreateAnimationTool()
        {
            Name = "create_animation";
            Description = "Create a keyframe animation clip with friendly property names";
        }

        public override JObject Execute(JObject parameters)
        {
            var clipName = parameters["name"]?.ToString() ?? "Animation";
            var gameObjectName = parameters["gameObjectName"]?.ToString();
            var loop = parameters["loop"]?.Value<bool>() ?? true;
            var properties = parameters["properties"] as JArray;

            if (properties == null || properties.Count == 0)
                return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    "No properties provided.", "validation_error");

            // Look up target GameObject once for reuse
            GameObject targetGo = !string.IsNullOrEmpty(gameObjectName) ? GameObject.Find(gameObjectName) : null;

            // Create clip
            var clip = new AnimationClip();
            clip.legacy = true;
            clip.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;

            foreach (var prop in properties)
            {
                var propertyName = prop["property"]?.ToString();
                var keyframes = prop["keyframes"] as JArray;

                if (string.IsNullOrEmpty(propertyName) || keyframes == null || keyframes.Count == 0)
                    continue;

                if (string.Equals(propertyName, "color", StringComparison.OrdinalIgnoreCase))
                {
                    SetColorCurves(clip, keyframes, targetGo);
                }
                else if (PropertyMap.TryGetValue(propertyName, out var mapping))
                {
                    SetTransformCurves(clip, keyframes, mapping.pathPrefix, mapping.componentType, mapping.channels);
                }
                else
                {
                    McpUnity.Utils.McpLogger.LogWarning($"Unknown animation property: {propertyName}");
                }
            }

            // Save clip asset
            var dir = "Assets/Animations";
            if (!AssetDatabase.IsValidFolder(dir))
            {
                AssetDatabase.CreateFolder("Assets", "Animations");
            }

            var assetPath = $"{dir}/{clipName}.anim";
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            AssetDatabase.CreateAsset(clip, assetPath);
            AssetDatabase.SaveAssets();

            // Attach to GameObject if specified
            string attachMessage = "";
            if (!string.IsNullOrEmpty(gameObjectName))
            {
                if (targetGo != null)
                {
                    var anim = targetGo.GetComponent<Animation>();
                    if (anim == null)
                    {
                        anim = Undo.AddComponent<Animation>(targetGo);
                    }

                    anim.clip = clip;
                    anim.AddClip(clip, clipName);
                    attachMessage = $" and attached to '{gameObjectName}'";
                }
                else
                {
                    attachMessage = $" (could not find GameObject '{gameObjectName}' to attach)";
                }
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Animation clip saved at {assetPath}{attachMessage}. Loop: {loop}.",
                ["data"] = new JObject { ["assetPath"] = assetPath }
            };
        }

        private static void SetTransformCurves(AnimationClip clip, JArray keyframes, string pathPrefix, Type componentType, string[] channels)
        {
            // Build one curve per channel (x, y, z)
            var curves = new AnimationCurve[channels.Length];
            for (int i = 0; i < channels.Length; i++)
                curves[i] = new AnimationCurve();

            foreach (var kf in keyframes)
            {
                var time = kf["time"]?.Value<float>() ?? 0f;
                var value = kf["value"] as JObject;
                if (value == null) continue;

                for (int i = 0; i < channels.Length; i++)
                {
                    var v = value[channels[i]]?.Value<float>() ?? 0f;
                    curves[i].AddKey(new Keyframe(time, v));
                }
            }

            for (int i = 0; i < channels.Length; i++)
            {
                clip.SetCurve("", componentType, $"{pathPrefix}.{channels[i]}", curves[i]);
            }
        }

        private static void SetColorCurves(AnimationClip clip, JArray keyframes, GameObject targetGo)
        {
            // Determine component type based on what the target has
            Type componentType = typeof(MeshRenderer);
            string propertyPrefix = "material._Color";

            if (targetGo != null && targetGo.GetComponent<MeshRenderer>() == null && targetGo.GetComponent<SpriteRenderer>() != null)
            {
                componentType = typeof(SpriteRenderer);
                propertyPrefix = "m_Color";
            }

            var channels = new[] { "r", "g", "b", "a" };
            var curves = new AnimationCurve[4];
            for (int i = 0; i < 4; i++)
                curves[i] = new AnimationCurve();

            foreach (var kf in keyframes)
            {
                var time = kf["time"]?.Value<float>() ?? 0f;
                var value = kf["value"] as JObject;
                if (value == null) continue;

                for (int i = 0; i < channels.Length; i++)
                {
                    var defaultVal = channels[i] == "a" ? 1f : 0f;
                    var v = value[channels[i]]?.Value<float>() ?? defaultVal;
                    curves[i].AddKey(new Keyframe(time, v));
                }
            }

            for (int i = 0; i < channels.Length; i++)
            {
                clip.SetCurve("", componentType, $"{propertyPrefix}.{channels[i]}", curves[i]);
            }
        }
    }
}
