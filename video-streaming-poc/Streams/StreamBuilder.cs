using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace video_streaming_poc.Streams
{
    public class StreamBuilder : IDisposable
    {
        /// <summary>
        /// The duration in seconds of each stream segment.
        /// </summary>
        public const int SEGMENT_DURATION = 10;

        public const string MANIFEST_FILE_NAME = "output.m3u8";

        public const string STREAMS_DIR = "/Users/naoey/Desktop/Poc_Streams";

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

        private FileSystemWatcher framesWatcher;

        private int lastFrameIndex = -1;

        public StreamBuilder(string sourcePath)
        {
            manifest = new StreamSourceManifest(sourcePath);

            string streamPath = Path.Join(STREAMS_DIR, manifest.Id);

            if (!File.Exists(streamPath) || !File.GetAttributes(streamPath).HasFlag(FileAttributes.Directory))
                Directory.CreateDirectory(streamPath);

            manifest.FileSystemOutputPath = streamPath;

            if (!manifest.IsAlive)
                throw new InvalidOperationException(
                    $"Attempted to initialise {nameof(StreamBuilder)} on a closed stream source.");

            manifestRefreshTimer = new Timer(refreshManifest, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

            framesWatcher = new FileSystemWatcher
            {
                Path = sourcePath,
                NotifyFilter = NotifyFilters.CreationTime,
                Filter = "*.png",
                EnableRaisingEvents = true,
            };

            framesWatcher.Changed += onFilesChanged;

            IsAlive = true;
        }

        private void onFilesChanged(object _, FileSystemEventArgs __)
        {
            try
            {
                List<FileInfo> newestFileBatch = new DirectoryInfo(manifest.FileSystemInputPath)
                    .GetFiles("*.png")
                    .Where(f => f.Name != "manifest" &&
                                int.Parse(Path.GetFileNameWithoutExtension(f.Name)) > lastFrameIndex)
                    .ToList();

                if (newestFileBatch.Count >= manifest.Fps * SEGMENT_DURATION)
                {
                    new StreamSegmentBuilder(manifest, newestFileBatch).Build();
                    lastFrameIndex = int.Parse(Path.GetFileNameWithoutExtension(newestFileBatch.Last().Name));
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void refreshManifest(object _)
        {
            manifest.Read();

            IsAlive = manifest.IsAlive;
        }

        public void Dispose()
        {
            manifestRefreshTimer?.Dispose();
            framesWatcher?.Dispose();

            if (File.Exists(StreamInfo.FileSystemOutputManifestPath))
            {
                var writer = File.AppendText(StreamInfo.FileSystemOutputManifestPath);
                writer.WriteLine("#EXT-X-ENDLIST");
            }
        }

        public class StreamSourceManifest : StreamInfo
        {
            public StreamSourceManifest(string sourcePath)
            {
                Id = Guid.NewGuid().ToString();
                FileSystemInputPath = sourcePath;

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
                }
            }
        }
    }
}