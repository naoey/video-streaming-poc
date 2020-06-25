using System;

namespace video_streaming_service.Configuration
{
    public static class EnvironmentConfiguration
    {
        public static string VideoOutputRoot { get; private set; }

        public static string StaticServerHost { get; private set; }

        public static int StreamSegmentDuration { get; private set; } = 10;

        public static void Build()
        {
            VideoOutputRoot = Environment.GetEnvironmentVariable("VAP_VIDEO_OUTPUT_ROOT");
            StaticServerHost = Environment.GetEnvironmentVariable("VAP_STATIC_SERVER_HOST");

            if (Environment.GetEnvironmentVariable("VAP_STREAM_SEGMENT_DURATION") != null)
                StreamSegmentDuration = int.Parse(Environment.GetEnvironmentVariable("VAP_STREAM_SEGMENT_DURATION"));
        }
    }
}
