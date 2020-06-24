using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace video_streaming_service.Streams
{
    public interface IImageStreamSegmentBuilder
    {
        /// <summary>
        /// A list of files representing frames in the video in the form of images.
        /// </summary>
        public IEnumerable<FileInfo> Frames { get; set; }

        /// <summary>
        /// Builds a stream segment video from the given <see cref="Frames"/>.
        /// </summary>
        public void Build();
    }
}
