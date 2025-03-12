using System.Text.Json.Serialization;

namespace HQExChecker.Clents.Utilities.Entities
{
    public class SubscribeCandlesObjectRequest
    {
        /// <summary>
        /// Has property name "event" on json serialization
        /// </summary>
        [JsonPropertyName("event")]
        [JsonInclude]
        public required string eventName;
        [JsonInclude]
        public required string channel;
        [JsonInclude]
        public required string key;
    }
}
