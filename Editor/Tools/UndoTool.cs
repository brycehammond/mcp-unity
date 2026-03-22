using Newtonsoft.Json.Linq;
using UnityEditor;

namespace McpUnity.Tools
{
    public class UndoTool : McpToolBase
    {
        public UndoTool()
        {
            Name = "undo";
            Description = "Undo one or more recent actions in the Unity Editor";
        }

        public override JObject Execute(JObject parameters)
        {
            var steps = parameters["steps"]?.Value<int>() ?? 1;

            if (steps < 1)
                return McpUnity.Unity.McpUnitySocketHandler.CreateErrorResponse(
                    "Steps must be at least 1.", "validation_error");

            for (int i = 0; i < steps; i++)
                Undo.PerformUndo();

            var groupName = Undo.GetCurrentGroupName();
            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Undid {steps} step(s). Current undo group: '{groupName}'."
            };
        }
    }
}
