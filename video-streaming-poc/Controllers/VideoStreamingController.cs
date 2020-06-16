using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace video_streaming_poc.Controllers
{
    public class VideoStreamingController : Controller
    {
        private const string video_path = "/Users/naoey/Desktop/FINAL_FANTASY_VII_REMAKE_20200413024342.mp4";

        private const string images_source_dir = "/Users/naoey/Desktop/images";

        private readonly ILogger<VideoStreamingController> logger;

        public VideoStreamingController(ILogger<VideoStreamingController> logger)
        {
            this.logger = logger;
        }

        [HttpGet]
        [Route("/video-stream")]
        public PhysicalFileResult GetVideo()
        {
            return PhysicalFile(video_path, "application/octet-stream", enableRangeProcessing: true);
        }

        [HttpGet]
        [Route("/create-video")]
        public PhysicalFileResult CreateVideo()
        {
            string video = getVideo();

            return PhysicalFile(video, "application/octet-stream", true);
        }

        private string getVideo()
        {
            const string outFile = "output.mp4";
            const string outDir = "/Users/naoey/Desktop";
            const string args = "-y -framerate {0} -f image2pipe -i - -r {0} -c:v libx264 -movflags +faststart -pix_fmt yuv420p -crf 19 -preset veryfast {1}";
            
            DirectoryInfo dir = new DirectoryInfo(images_source_dir);
            
            logger.Log(LogLevel.Information, "Starting ffmpeg...");

            var pInfo = new ProcessStartInfo("ffmpeg", string.Format(args, 1, Path.Join(outDir, outFile)))
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                WorkingDirectory = outDir,
            };
            
            var proc = Process.Start(pInfo);
            
            using (var stream = new BinaryWriter(proc.StandardInput.BaseStream))
            {
                foreach (var file in dir.GetFiles().OrderBy(f => f.Name))
                {
                    stream.Write(System.IO.File.ReadAllBytes($"{file.DirectoryName}/{file.Name}"));
                }
            }

            proc.WaitForExit();

            logger.Log(LogLevel.Information, "ffmpeg closed");

            return Path.Join(outDir, outFile);
        }
    }
}