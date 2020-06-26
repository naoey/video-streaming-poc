using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Serilog;
using video_streaming_service.Streams;

namespace video_streaming_service.Controllers
{
    public class StreamsController : Controller
    {
        private StreamManager manager;

        public StreamsController(StreamManager manager)
        {
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
            try
            {
                return Ok(manager.CreateStream(info.FileSystemInputPath));
            }
            catch (FileNotFoundException)
            {
                Log.Error("Failed to create stream for {@StreamInfo}. Manifest file is missing.");
                return BadRequest(new {message = "Input path does not contain a manifest file."});
            }
            catch (IOException e)
            {
                Log.Error(e, "Failed to initialise new {@StreamInfo}.", info);
                return BadRequest(new {message = "Manifest is not readable"});
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
