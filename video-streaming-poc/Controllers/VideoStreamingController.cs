using Microsoft.AspNetCore.Mvc;

namespace video_streaming_poc.Controllers
{
    public class VideoStreamingController : Controller
    {
        // GET
        public IActionResult Index()
        {
            return View();
        }
    }
}