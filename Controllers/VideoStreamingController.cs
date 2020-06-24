using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using video_streaming_service.Responses;

namespace video_streaming_service.Controllers
{
    public class VideoStreamingController : Controller
    {
        private const string video_path = "/Users/naoey/Desktop/FINAL_FANTASY_VII_REMAKE_20200413024342.mp4";

        private const string images_source_dir = "/Users/naoey/Desktop/images";

        private const string out_dir = "/Users/naoey/Desktop";

        private const string file_name = "output.m3u8";

        private readonly ILogger<VideoStreamingController> logger;

        public VideoStreamingController(ILogger<VideoStreamingController> logger)
        {
            this.logger = logger;
        }

        [HttpPost]
        [Route("/old/stream")]
        public PhysicalFileResult CreateVideo()
        {
            logger.Log(LogLevel.Information, "Beginning video creation...");

            string path = Path.Join(out_dir, file_name);

            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
            
            System.IO.File.Create(path).Dispose();
            
            Task.Run(() =>
            {
                string conversionArgs =
                    "-y" +
                    $" -framerate 1 -f image2pipe -i - -movflags +faststart -pix_fmt yuv420p -preset veryfast -vf scale=w=426:h=240:force_original_aspect_ratio=decrease -c:v libx264 -profile:v main -crf 20 -sc_threshold 0 -g 48 -keyint_min 48 -hls_time 4 -hls_playlist_type vod -b:v 240k -maxrate 240k -bufsize 480k {path}";

                // string conversionArgs = string.Format(
                //     "-y" +
                //     " -framerate 1 -f image2pipe -i - -movflags +faststart -pix_fmt yuv420p -preset veryfast -vf scale=w=426:h=240:force_original_aspect_ratio=decrease -c:v libx264 -profile:v main -crf 20 -sc_threshold 0 -g 48 -keyint_min 48 -hls_time 4 -hls_playlist_type vod -b:v 240k -maxrate 240k -bufsize 480k -hls_segment_filename {0}240p_%d.ts {0}240p.m3u8" +  
                //     " -framerate 1 -f image2pipe -i - -movflags +faststart -pix_fmt yuv420p -preset veryfast -vf scale=w=640:h=360:force_original_aspect_ratio=decrease -c:v libx264 -profile:v main -crf 20 -sc_threshold 0 -g 48 -keyint_min 48 -hls_time 4 -hls_playlist_type vod -b:v 800k -maxrate 856k -bufsize 1200k -hls_segment_filename {0}360p_%d.ts {0}360p.m3u8" +  
                //     " -framerate 1 -f image2pipe -i - -movflags +faststart -pix_fmt yuv420p -preset veryfast -vf scale=w=842:h=480:force_original_aspect_ratio=decrease -c:v libx264 -profile:v main -crf 20 -sc_threshold 0 -g 48 -keyint_min 48 -hls_time 4 -hls_playlist_type vod -b:v 1400k -maxrate 1498k -bufsize 2100k -hls_segment_filename {0}480p_%d.ts {0}480p.m3u8" +  
                //     " -framerate 1 -f image2pipe -i - -movflags +faststart -pix_fmt yuv420p -preset veryfast -vf scale=w=1280:h=720:force_original_aspect_ratio=decrease -c:v libx264 -profile:v main -crf 20 -sc_threshold 0 -g 48 -keyint_min 48 -hls_time 4 -hls_playlist_type vod -b:v 2800k -maxrate 2996k -bufsize 4200k -hls_segment_filename {0}720p_%d.ts {0}720p.m3u8" +  
                //     " -framerate 1 -f image2pipe -i - -movflags +faststart -pix_fmt yuv420p -preset veryfast -vf scale=w=1920:h=1080:force_original_aspect_ratio=decrease -c:v libx264 -profile:v main -crf 20 -sc_threshold 0 -g 48 -keyint_min 48 -hls_time 4 -hls_playlist_type vod -b:v 5000k -maxrate 5350k -bufsize 7500k -hls_segment_filename {0}1080p_%d.ts {0}1080p.m3u8",
                //     outputFile
                // );

                FileInfo[] files = new DirectoryInfo(images_source_dir).GetFiles();

                logger.Log(LogLevel.Information, "Starting ffmpeg");

                var pInfo = new ProcessStartInfo("ffmpeg", conversionArgs)
                {
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    WorkingDirectory = out_dir,
                };

                var proc = Process.Start(pInfo);

                if (proc == null)
                    throw new ApplicationException($"An error occurred while creating ffmpeg process");

                using (var stream = new BinaryWriter(proc.StandardInput.BaseStream))
                {
                    logger.Log(LogLevel.Information, $"Writing {files.Length} files to stream...");

                    foreach (var file in files)
                    {
                        string filePath = $"{file.DirectoryName}/{file.Name}";
                        logger.Log(LogLevel.Debug, $"Writing file {filePath} to stream");
                        stream.Write(System.IO.File.ReadAllBytes(filePath));
                    }
                }

                proc.WaitForExit();

                logger.Log(LogLevel.Information, "ffmpeg closed");
            });

            logger.Log(LogLevel.Information, "Began video building task");
            
            return PhysicalFile(path, "application/vnd.apple.mpegurl");
        }

        [HttpGet]
        [Route("/old/stream")]
        public PhysicalFileResult LoadVideo()
        {
            string videoFile = Path.Join(out_dir, file_name);

            return PhysicalFile(videoFile, "application/octet-stream", enableRangeProcessing: true);
        }

        [HttpGet]
        [Route("/old/poll")]
        public PollResult Poll()
        {
            string file = Path.Join(out_dir, file_name);

            string probeArgs =
                $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 {file}";

            var pInfo = new ProcessStartInfo("ffprobe", probeArgs)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };

            var proc = Process.Start(pInfo);
            
            if (proc == null)
                throw new ApplicationException($"Encountered an error running ffprobe");

            string result = proc.StandardOutput.ReadToEnd().Trim();

            proc.WaitForExit();
            
            float.TryParse(result, out float duration);

            logger.Log(LogLevel.Information, $"Got ffprobe result s'{result}; f'{duration}");

            return new PollResult
            {
                Length = duration,
            };
        }
    }
}