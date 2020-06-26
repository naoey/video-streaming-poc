using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Serilog;

namespace video_streaming_service.Streams
{
    public class StreamSegmentBuilder : IImageStreamSegmentBuilder
    {
        public IEnumerable<FileInfo> Frames { get; set; }

        public readonly StreamInfo StreamInfo;

        public StreamSegmentBuilder(StreamInfo streamInfo, IEnumerable<FileInfo> frames)
        {
            Frames = frames;
            StreamInfo = streamInfo;
        }

        public void Build()
        {
            string ffmpegArgs =
                $"-framerate {StreamInfo.Fps} " +
                $"-f image2pipe " +
                $"-i - " +
                $"-pix_fmt yuv420p " +
                $"-preset veryfast " +
                $"-c:v libx264 " +
                $"-profile:v main " +
                $"-crf 20 " +
                $"-sc_threshold 0 " +
                $"-g 48 " +
                $"-keyint_min 48 " +
                $"-hls_time {StreamInfo.SegmentLength} " +
                $"-hls_list_size 0 " +
                $"-hls_flags append_list+omit_endlist+round_durations " +
                $"-b:v 240k " +
                $"-maxrate 240k " +
                $"-bufsize 480k " +
                $"{StreamInfo.FileSystemOutputManifestPath} ";

            Log.Debug("Starting ffmpeg for {@StreamInfo} segment with arguments {ffmpegArgs}", StreamInfo, ffmpegArgs);

            var pInfo = new ProcessStartInfo("ffmpeg", ffmpegArgs)
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = StreamInfo.FileSystemOutputPath,
            };

            var proc = Process.Start(pInfo);

            if (proc == null)
                throw new ApplicationException(
                    $"Error occurred when starting ffmpeg process for building stream segment for stream {StreamInfo.Id}");

            using (var stream = new BinaryWriter(proc.StandardInput.BaseStream))
            {
                Log.Debug("Opened ffmpeg stream; writing {count} frames to stream.", Frames.Count());

                foreach (var frame in Frames)
                {
                    stream.Write(File.ReadAllBytes(frame.FullName));
                }
            }

            proc.WaitForExit();

            Log.Debug("Completed building segment for {@StreamInfo}", StreamInfo);
        }
    }
}
