using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using video_streaming_service.Streams;

namespace video_streaming_service.Controllers
{
    public class StreamsController : Controller
    {
        private readonly ILogger<StreamsController> logger;

        private StreamManager manager;

        public StreamsController(ILogger<StreamsController> logger, StreamManager manager)
        {
            this.logger = logger;
            this.manager = manager;
        }

        [HttpGet("/Streams")]
        public List<StreamInfo> GetAllStreams()
        {
            return manager.Streams.Select(s => s.StreamInfo).ToList();
        }

        [HttpPost("/Stream")]
        public IActionResult CreateStream([FromBody] StreamInfo info)
        {
            logger.Log(LogLevel.Debug, $"Attempting to create new stream at path {info.FileSystemInputPath}.");
            
            try
            {
                return Ok(manager.CreateStream(info.FileSystemInputPath));
            }
            catch (FileNotFoundException e)
            {
                logger.Log(LogLevel.Error, $"Failed to create stream for path {info.FileSystemInputPath}. Manifest file is missing.");
                return BadRequest(new {message = "Input path does not contain a manifest file."});
            }
        }

        [HttpGet("/Streams/{id}")]
        public IActionResult GetStream(string id)
        {
            var stream = manager.Streams.Find(s => s.StreamInfo.Id == id);

            if (stream == null)
                return NotFound();

            return Ok(stream);
        }

        [HttpDelete("/Streams/{id}")]
        public void DestroyStream(string id)
        {
            manager.CloseStream(id);
        }
    }
}