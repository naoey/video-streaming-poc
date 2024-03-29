using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using video_streaming_service.Configuration;

namespace video_streaming_service.Streams
{
    public class StreamBuilder : IDisposable
    {
        /// <summary>
        /// Playlist file name for outputted HLS video entry.
        /// </summary>
        public const string MANIFEST_FILE_NAME = "output.m3u8";

        /// <summary>
        /// The frequency at which fames input directories are polled for new files
        /// </summary>
        public const int INPUT_POLL_DURATION = 2;

        public string Id
        {
            get => manifest?.Id;
        }

        private bool isAlive;

        public bool IsAlive
        {
            get => isAlive;
            private set
            {
                isAlive = value;

                if (!isAlive)
                    Dispose();
            }
        }

        public string Name
        {
            get => manifest?.Name;
        }

        private readonly StreamSourceManifest manifest;

        public StreamInfo StreamInfo => manifest;

        private Timer manifestRefreshTimer;

        private Timer inputFilesPoll;

        private int lastFrameIndex = -1;

        public StreamBuilder(string sourcePath)
        {
            manifest = new StreamSourceManifest(sourcePath);

            string streamPath = Path.Join(EnvironmentConfiguration.VideoOutputRoot, manifest.Id);

            if (!File.Exists(streamPath) || !File.GetAttributes(streamPath).HasFlag(FileAttributes.Directory))
                Directory.CreateDirectory(streamPath);

            manifest.FileSystemOutputPath = streamPath;

            if (!manifest.IsAlive)
                throw new InvalidOperationException(
                    $"Attempted to initialise {nameof(StreamBuilder)} on a closed stream source.");

            manifestRefreshTimer = new Timer(refreshManifest, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
            inputFilesPoll = new Timer((object o) => checkFilesChanged(o), null, TimeSpan.Zero, TimeSpan.FromSeconds(2));

            IsAlive = true;
        }

        private object lockObject = new object();

        private void checkFilesChanged(object _, bool flushPendingFrames = false)
        {
            lock (lockObject)
            {
                try
                {
                    List<FileInfo> newestFileBatch = new DirectoryInfo(manifest.FileSystemInputPath)
                        .GetFiles("*.png")
                        .Where(f => f.Name != "manifest" &&
                                    int.Parse(Path.GetFileNameWithoutExtension(f.Name)) > lastFrameIndex)
                        .OrderBy(f => int.Parse(Path.GetFileNameWithoutExtension(f.Name)))
                        .ToList();

                    Log.Information(
                        "Processing new frames batch for {@StreamInfo}. Last processed frame is {lastFrameIndex}. {newCount} frames in new batch. Need {fps} * {segmentLength} frames.",
                        StreamInfo,
                        lastFrameIndex,
                        newestFileBatch.Count(),
                        StreamInfo.Fps,
                        StreamInfo.SegmentLength
                    );

                    if (flushPendingFrames || newestFileBatch.Count >= manifest.Fps * StreamInfo.SegmentLength)
                    {
                        Log.Information("Minimum new frames available; creating new segment for {@StreamInfo}", StreamInfo);

                        new StreamSegmentBuilder(manifest, newestFileBatch).Build();

                        lastFrameIndex = int.Parse(Path.GetFileNameWithoutExtension(newestFileBatch.Last().Name));

                        Log.Debug("Batch processing completed!");
                    }

                }
                catch (Exception e)
                {
                    Log.Error(e, "Failed to create new segment for {@StreamInfo}", StreamInfo);
                }
            }
        }

        private void refreshManifest(object _)
        {
            manifest.Read();

            IsAlive = manifest.IsAlive;
        }

        public void Dispose()
        {
            checkFilesChanged(null, flushPendingFrames: true);

            Log.Debug("Finalising and closing stream {@StreamInfo}", StreamInfo);

            manifestRefreshTimer?.Dispose();
            inputFilesPoll?.Dispose();

            if (File.Exists(StreamInfo.FileSystemOutputManifestPath))
            {
                File.AppendAllText(StreamInfo.FileSystemOutputManifestPath, "\n#EXT-X-ENDLIST");
            }
        }

        public class StreamSourceManifest : StreamInfo
        {
            public StreamSourceManifest(string sourcePath)
            {
                Id = Guid.NewGuid().ToString();
                FileSystemInputPath = sourcePath;
                StartTime = DateTimeOffset.Now;

                Read();
            }

            public void Read()
            {
                StreamReader reader = new StreamReader(FileSystemInputManifestPath);

                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split("=").Select(p => p.Trim()).ToArray();

                    parseLine(parts[0], parts[1]);
                }
            }

            private void parseLine(string key, string value)
            {
                switch (key)
                {
                    case "Name":
                        Name = value;
                        break;

                    case "Fps":
                        Fps = int.Parse(value);
                        break;

                    case "IsAlive":
                        IsAlive = bool.Parse(value);
                        break;

                    case "SegmentLength":
                        SegmentLength = int.Parse(value);
                        break;
                }
            }
        }
    }
}
