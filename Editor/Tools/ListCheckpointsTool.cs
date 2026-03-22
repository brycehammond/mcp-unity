using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    public class ListCheckpointsTool : McpToolBase
    {
        public ListCheckpointsTool()
        {
            Name = "list_checkpoints";
            Description = "List all saved project states with names, descriptions, and timestamps.";
            IsAsync = true;
        }

        public override async void ExecuteAsync(JObject parameters, TaskCompletionSource<JObject> tcs)
        {
            await Task.Yield(); // Allow async context

            var manifest = SaveCheckpointTool.LoadManifest();

            if (manifest.Count == 0)
            {
                tcs.SetResult(new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = "No checkpoints found.",
                    ["data"] = new JObject { ["saves"] = new JArray() }
                });
                return;
            }

            // Build response with relative time descriptions
            var saves = new JArray();
            JToken mostRecent = null;

            foreach (var entry in manifest)
            {
                var save = new JObject
                {
                    ["name"] = entry["name"],
                    ["description"] = entry["description"],
                    ["timestamp"] = entry["timestamp"],
                    ["scenePath"] = entry["scenePath"],
                    ["commitHash"] = entry["commitHash"]
                };

                if (DateTime.TryParse(entry["timestamp"]?.ToString(), out var ts))
                {
                    save["relativeTime"] = GetRelativeTime(ts.ToUniversalTime());
                }

                saves.Add(save);
                mostRecent = entry;
            }

            tcs.SetResult(new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"{manifest.Count} checkpoint(s) found.",
                ["data"] = new JObject { ["saves"] = saves }
            });
        }

        private static string GetRelativeTime(DateTime utcTime)
        {
            var diff = DateTime.UtcNow - utcTime;

            if (diff.TotalSeconds < 60)
                return "just now";
            if (diff.TotalMinutes < 60)
            {
                var mins = (int)diff.TotalMinutes;
                return $"{mins} minute{(mins == 1 ? "" : "s")} ago";
            }
            if (diff.TotalHours < 24)
            {
                var hours = (int)diff.TotalHours;
                return $"{hours} hour{(hours == 1 ? "" : "s")} ago";
            }
            var days = (int)diff.TotalDays;
            return $"{days} day{(days == 1 ? "" : "s")} ago";
        }
    }
}
