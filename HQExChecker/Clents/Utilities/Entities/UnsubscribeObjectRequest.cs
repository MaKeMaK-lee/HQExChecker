using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
