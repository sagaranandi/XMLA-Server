using System.Text.Json;
using System.Text.Json.Serialization;

namespace XmlaApi.Models
{
    public class TokenResponse
    {
        [JsonPropertyName("statusCode")]
        public int StatusCode { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("payload")]
        public string Payload { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }
    }
}
