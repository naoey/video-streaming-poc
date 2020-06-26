using System;
using System.IO;
using Newtonsoft.Json;
using video_streaming_service.Configuration;

namespace video_streaming_service.Streams
{
    public class StreamInfo : IEquatable<StreamInfo>
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("startTime")]
        public DateTimeOffset StartTime { get; set; }

        [JsonProperty("segmentLength")]
        public int SegmentLength { get; set; }

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

        [JsonProperty("publicUri")]
        public string PublicUri => $"{EnvironmentConfiguration.StaticServerHost}/{Id}/{StreamBuilder.MANIFEST_FILE_NAME}";

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

        public override string ToString()
        {
            return $"{Name} [{Id}]";
        }
    }
}
