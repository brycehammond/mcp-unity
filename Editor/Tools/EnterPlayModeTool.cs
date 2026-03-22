using Newtonsoft.Json.Linq;
using UnityEditor;

namespace McpUnity.Tools
{
    public class EnterPlayModeTool : McpToolBase
    {
        public EnterPlayModeTool()
        {
            Name = "enter_play_mode";
            Description = "Enter Play Mode in the Unity Editor";
        }

        public override JObject Execute(JObject parameters)
        {
            if (EditorApplication.isPlaying)
            {
                return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    "Already in Play Mode.", "tool_execution_error");
            }

            EditorApplication.EnterPlaymode();

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = "Entered Play Mode."
            };
        }
    }
}
