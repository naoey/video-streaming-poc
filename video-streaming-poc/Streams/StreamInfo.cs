using System;
using Newtonsoft.Json;

namespace video_streaming_poc.Streams
{
    public class StreamInfo : IEquatable<StreamInfo>
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

        public bool Equals(StreamInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((StreamInfo) obj);
        }

        public override int GetHashCode()
        {
            return Id?.GetHashCode() ?? 0;
        }
    }
}