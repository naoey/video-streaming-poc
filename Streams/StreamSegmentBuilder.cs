using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace video_streaming_service.Streams
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
                $"-hls_time 4 " +
                $"-hls_list_size 0 " +
                $"-hls_flags append_list+omit_endlist+round_durations " +
                $"-b:v 240k " +
                $"-maxrate 240k " +
                $"-bufsize 480k " +
                $"{StreamInfo.FileSystemOutputManifestPath}";

            Console.WriteLine($"Starting ffmpeg process with working directory {StreamInfo.FileSystemOutputPath}");

            var pInfo = new ProcessStartInfo("ffmpeg", ffmpegArgs)
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                WorkingDirectory = StreamInfo.FileSystemOutputPath
            };

            var proc = Process.Start(pInfo);

            if (proc == null)
                throw new ApplicationException(
                    $"Error occurred when starting ffmpeg process for building stream segment for stream {StreamInfo.Id}");

            using (var stream = new BinaryWriter(proc.StandardInput.BaseStream))
            {
                Console.WriteLine($"Opened writer to ffmpeg stdin; Writing {Frames.Count()} images.");

                foreach (var frame in Frames)
                {
                    stream.Write(File.ReadAllBytes(frame.FullName));
                }
            }

            proc.WaitForExit();

            Console.WriteLine("ffmpeg closed.");
        }
    }
}