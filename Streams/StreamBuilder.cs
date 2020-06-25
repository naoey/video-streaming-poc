using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using video_streaming_service.Configuration;

namespace video_streaming_service.Streams
{
    public class StreamBuilder : IDisposable
    {
        public const string MANIFEST_FILE_NAME = "output.m3u8";

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

            string streamPath = Path.Join(EnvironmentConfiguration.VideoOutputRoot, manifest.Id);

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
                NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite,
                Filter = "*.png",
                EnableRaisingEvents = true,
            };

            framesWatcher.Changed += onFilesChanged;

            IsAlive = true;
        }

        private bool isProcessingBatch = false;

        private void onFilesChanged(object _, FileSystemEventArgs __)
        {
            if (isProcessingBatch)
                return;

            isProcessingBatch = true;

            try
            {
                List<FileInfo> newestFileBatch = new DirectoryInfo(manifest.FileSystemInputPath)
                    .GetFiles("*.png")
                    .Where(f => f.Name != "manifest" &&
                                int.Parse(Path.GetFileNameWithoutExtension(f.Name)) > lastFrameIndex)
                    .OrderBy(f => int.Parse(Path.GetFileNameWithoutExtension(f.Name)))
                    .ToList();

                if (newestFileBatch.Count >= manifest.Fps * EnvironmentConfiguration.StreamSegmentDuration)
                {
                    new StreamSegmentBuilder(manifest, newestFileBatch).Build();
                    lastFrameIndex = int.Parse(Path.GetFileNameWithoutExtension(newestFileBatch.Last().Name));
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            isProcessingBatch = false;
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
