using System.Text.Json.Serialization;

namespace ReminderApp.Common;

public class AddWorkoutsRequest
{
    [JsonPropertyName("workouts")]
    public List<WorkoutItem> Workouts { get; set; } = new();

    public class WorkoutItem
    {
        [JsonPropertyName("date")]
        public string Date { get; set; } = "";    // yyyy-MM-dd

        [JsonPropertyName("subject")]
        public string Subject { get; set; } = "";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";
    }
}