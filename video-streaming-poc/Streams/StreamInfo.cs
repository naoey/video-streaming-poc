using Newtonsoft.Json;

namespace video_streaming_poc.Streams
{
    public class StreamInfo
    {
        [JsonProperty("id")]
        public string Id { get; protected set; }
        
        [JsonProperty("name")]
        public string Name { get; protected set; }
        
        [JsonProperty("fps")]
        public int Fps { get; protected set; }
            
        [JsonProperty("isAlive")]
        public bool IsAlive { get; protected set; }
        
        [JsonIgnore]
        public string FileSystemPath { get; set; }
    }
}