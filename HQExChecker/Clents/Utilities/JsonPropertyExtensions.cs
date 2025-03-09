using System.Text.Json;

namespace HQExChecker.Clents.Utilities
{
    public static class JsonPropertyExtensions
    {
        public static string GetStringValueOf(this IEnumerable<JsonProperty> property, string name)
        {
            return property.FirstOrDefault(p => p.Name == name).Value.ToString();
        }

        public static int GetIntValueOf(this IEnumerable<JsonProperty> property, string name)
        {
            return property.FirstOrDefault(p => p.Name == name).Value.GetInt32();
        }
    }
}
