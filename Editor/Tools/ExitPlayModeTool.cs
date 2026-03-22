using Newtonsoft.Json.Linq;
using UnityEditor;

namespace McpUnity.Tools
{
    public class ExitPlayModeTool : McpToolBase
    {
        public ExitPlayModeTool()
        {
            Name = "exit_play_mode";
            Description = "Exit Play Mode in the Unity Editor";
        }

        public override JObject Execute(JObject parameters)
        {
            if (!EditorApplication.isPlaying)
            {
                return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    "Not in Play Mode.", "tool_execution_error");
            }

            EditorApplication.ExitPlaymode();

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = "Exited Play Mode."
            };
        }
    }
}
