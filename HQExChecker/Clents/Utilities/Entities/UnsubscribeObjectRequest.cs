using System.Text.Json.Serialization;

namespace HQExChecker.Clents.Utilities.Entities
{
    public class UnsubscribeObjectRequest
    {
        /// <summary>
        /// Has property name "event" on json serialization
        /// </summary>
        [JsonPropertyName("event")]
        [JsonInclude]
        public required string eventName;
        [JsonInclude]
        public required int chanId;
    }
}
