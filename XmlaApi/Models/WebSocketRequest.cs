namespace XmlaApi.Models
{
    public class WebSocketRequest
    {
        public string Type { get; set; }
        public object Payload { get; set; }
        public string BearerToken { get; set; }
    }
}
