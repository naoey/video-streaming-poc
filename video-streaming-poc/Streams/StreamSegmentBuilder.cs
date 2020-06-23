using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace video_streaming_poc.Streams
{
    public class StreamSegmentBuilder : IImageStreamSegmentBuilder
    {
        public readonly IEnumerable<FileInfo> Frames;

        public readonly StreamInfo StreamInfo;

        private string segmentNamePattern =>
            $"{Path.GetFileNameWithoutExtension(StreamBuilder.MANIFEST_FILE_NAME)}%d.ts";

        public StreamSegmentBuilder(StreamInfo streamInfo, IEnumerable<FileInfo> frames)
        {
            Frames = frames;
            StreamInfo = streamInfo;
        }
        
        public void Build()
        {
            string ffmpegArgs = $@"-framerate {StreamInfo.Fps}
 -f image2pipe
 -movflags +faststart
 -pix_fmt yuv420p
 -preset veryfast
 -c:v libx264
 -profile:v main
 -crf 20
 -sc_threshold 0
 -g 48
 -keyint_min 48
 -hls_time 4
 -b:v 240k
 -maxrate 240k
 -bufsize 480k
 -vf
 -i -
 -map 0
 -f segment
 -segment_time {StreamBuilder.SEGMENT_DURATION}
 -segment_format mpegts
 -segment_list {StreamInfo.FileSystemOutputManifestPath}
 -segment_list_type m3u8
 -hls_flags append_list+omit_endlist 
 {Path.Join(StreamInfo.FileSystemOutputPath, segmentNamePattern)}";
            
            var pInfo = new ProcessStartInfo("ffmpg", ffmpegArgs)
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                WorkingDirectory = StreamInfo.FileSystemOutputPath,
            };

            var proc = Process.Start(pInfo);
            
            if (proc == null)
                throw new ApplicationException($"Error occurred when starting ffmpeg process for building stream segment for stream {StreamInfo.Id}");

            using (var stream = new BinaryWriter(proc.StandardInput.BaseStream))
            {
                foreach (var frame in Frames)
                {
                    stream.Write(File.ReadAllBytes(frame.FullName));
                }
            }

            proc.WaitForExit();
        }
    }
}