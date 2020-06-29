using System;
using Serilog;

namespace video_streaming_service.Configuration
{
    public static class EnvironmentConfiguration
    {
        public static string VideoOutputRoot { get; private set; }

        public static string StaticServerHost { get; private set; }


        public static void Build()
        {
            VideoOutputRoot = Environment.GetEnvironmentVariable("VAP_VIDEO_OUTPUT_ROOT");
            StaticServerHost = Environment.GetEnvironmentVariable("VAP_STATIC_SERVER_HOST");

            Log.Debug("Initialised environment configuration");
        }
    }
}
