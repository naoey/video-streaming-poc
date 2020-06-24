using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace video_streaming_service.Streams
{
    public interface IImageStreamSegmentBuilder
    {
        /// <summary>
        /// Builds a stream segment video from a given list of input frames.
        /// </summary>
        public void Build();
    }
}