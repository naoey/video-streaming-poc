using System;
using System.Collections.Generic;

namespace video_streaming_service.Streams
{
    public class StreamManager : IDisposable
    {
        public readonly List<StreamBuilder> Streams = new List<StreamBuilder>();

        /// <summary>
        /// Instantiates a new live stream with the given directory path being used as the source directory for images
        /// to be used as a frame source for the stream.
        /// </summary>
        /// <param name="source">Path to a directory where source frames will be added.</param>
        /// <returns>The <see cref="StreamInfo"/> for the newly created stream.</returns>
        public StreamInfo CreateStream(string source)
        {
            StreamBuilder stream;

            Streams.Add(stream = new StreamBuilder(source));

            return stream.StreamInfo;
        }

        /// <summary>
        /// Terminates a stream identified by its <see cref="StreamInfo"/>
        /// </summary>
        /// <param name="streamInfo">The <see cref="StreamInfo"/> of the stream to terminate.</param>
        /// <returns>Returns true if the stream was successfully terminated, false if it was not found.</returns>
        public bool CloseStream(StreamInfo streamInfo)
        {
            var stream = Streams.Find(s => streamInfo.Equals(s.StreamInfo));

            if (stream == null)
                return false;

            stream.Dispose();

            Streams.Remove(stream);

            return true;
        }

        public bool CloseStream(string id)
        {
            return CloseStream(Streams.Find(s => s.StreamInfo.Id == id)?.StreamInfo);
        }

        public void Dispose()
        {
            foreach (var stream in Streams)
                stream.Dispose();

            Streams.Clear();
        }
    }
}
