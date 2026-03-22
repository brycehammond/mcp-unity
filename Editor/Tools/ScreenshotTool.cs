using System;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Tools
{
    public class ScreenshotTool : McpToolBase
    {
        private const int MaxDimension = 768;

        public ScreenshotTool()
        {
            Name = "screenshot";
            Description = "Capture a screenshot of the Game or Scene view";
        }

        public override JObject Execute(JObject parameters)
        {
            var view = parameters["view"]?.ToString() ?? "game";
            var width = parameters["width"]?.Value<int>() ?? 512;
            var height = parameters["height"]?.Value<int>() ?? 512;

            width = Mathf.Clamp(width, 64, MaxDimension);
            height = Mathf.Clamp(height, 64, MaxDimension);

            Camera camera;
            string viewLabel;

            if (string.Equals(view, "scene", StringComparison.OrdinalIgnoreCase))
            {
                var sceneView = SceneView.lastActiveSceneView;
                if (sceneView == null)
                {
                    return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                        "No active Scene view found. Open a Scene view in the editor.", "tool_execution_error");
                }
                camera = sceneView.camera;
                viewLabel = "Scene";
            }
            else
            {
                camera = Camera.main;
                if (camera == null)
                    camera = UnityEngine.Object.FindObjectOfType<Camera>();
                if (camera == null)
                {
                    return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                        "No camera found in the scene. Add a Camera to capture a screenshot.", "tool_execution_error");
                }
                viewLabel = "Game";
            }

            RenderTexture rt = null;
            Texture2D tex = null;

            try
            {
                rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                rt.Create();

                var previousTarget = camera.targetTexture;
                camera.targetTexture = rt;
                camera.Render();
                camera.targetTexture = previousTarget;

                var previousActive = RenderTexture.active;
                RenderTexture.active = rt;
                tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                RenderTexture.active = previousActive;

                var pngBytes = tex.EncodeToPNG();
                var base64 = Convert.ToBase64String(pngBytes);

                var sizeKB = pngBytes.Length / 1024;
                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = $"Captured {width}x{height} {viewLabel} view screenshot ({sizeKB} KB).",
                    ["data"] = new JObject
                    {
                        ["imageBase64"] = base64,
                        ["mimeType"] = "image/png",
                        ["width"] = width,
                        ["height"] = height
                    }
                };
            }
            catch (Exception ex)
            {
                return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    $"Screenshot failed: {ex.Message}", "tool_execution_error");
            }
            finally
            {
                if (tex != null)
                    UnityEngine.Object.DestroyImmediate(tex);
                if (rt != null)
                {
                    rt.Release();
                    UnityEngine.Object.DestroyImmediate(rt);
                }
            }
        }
    }
}
