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
            get => Manifest?.Id;
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
            get => Manifest?.Name;
        }

        private readonly string sourcePath;

        public readonly StreamSourceManifest Manifest;

        private Timer manifestRefreshTimer;

        private FileSystemWatcher framesWatcher;

        private int lastFrameIndex = -1;

        public StreamBuilder(string sourcePath)
        {
            this.sourcePath = sourcePath;

            Manifest = new StreamSourceManifest(Path.Join(sourcePath, "manifest.txt"));

            string streamPath = Path.Join(STREAMS_DIR, Manifest.Id);

            if (!File.Exists(streamPath) || !File.GetAttributes(streamPath).HasFlag(FileAttributes.Directory))
                Directory.CreateDirectory(streamPath);

            Manifest.FileSystemPath = streamPath;

            if (!Manifest.IsAlive)
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
            List<FileInfo> newestFileBatch = new DirectoryInfo(sourcePath)
                .GetFiles()
                .Where(f => int.Parse(Path.GetFileNameWithoutExtension(f.Name)) > lastFrameIndex)
                .ToList();

            if (newestFileBatch.Count >= Manifest.Fps * SEGMENT_DURATION)
            {
                new StreamSegmentBuilder(Manifest, newestFileBatch).Build();
            }
        }

        private void refreshManifest(object _)
        {
            Manifest.Read();

            IsAlive = Manifest.IsAlive;
        }

        public void Dispose()
        {
            manifestRefreshTimer?.Dispose();
            framesWatcher?.Dispose();
        }

        public class StreamSourceManifest : StreamInfo
        {
            private readonly string manifestPath;

            public StreamSourceManifest(string manifestPath)
            {
                this.manifestPath = manifestPath;

                Id = Guid.NewGuid().ToString();

                Read();
            }

            public void Read()
            {
                try
                {
                    StreamReader reader = new StreamReader(manifestPath);

                    string line;

                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] parts = line.Split("=").Select(p => p.Trim()).ToArray();

                        parseLine(parts[0], parts[1]);
                    }
                }
                catch (FileNotFoundException e)
                {
                    Console.WriteLine($"Manifest file {manifestPath} not found.");
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