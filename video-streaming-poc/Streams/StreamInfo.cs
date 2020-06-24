using System;
using System.IO;
using Newtonsoft.Json;

namespace video_streaming_poc.Streams
{
    public class StreamInfo : IEquatable<StreamInfo>
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("fps")]
        public int Fps { get; set; }
            
        [JsonProperty("isAlive")]
        public bool IsAlive { get; set; }
        
        [JsonRequired]
        [JsonProperty("fileSystemInputPath")]
        public string FileSystemInputPath { get; set; }

        [JsonIgnore]
        public string FileSystemInputManifestPath =>
            Path.Join(FileSystemInputPath, "manifest");
        
        [JsonIgnore]
        public string FileSystemOutputPath { get; set; }

        public string PublicUri => $"{StreamManager.STREAM_HOST}/{Id}/{StreamBuilder.MANIFEST_FILE_NAME}";

        
        [JsonIgnore]
        public string FileSystemOutputManifestPath =>
            Path.Join(FileSystemOutputPath, StreamBuilder.MANIFEST_FILE_NAME);

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