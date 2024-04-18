using System.Web;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.Map
{
    [ApiController]
    public class MapController : ControllerBase
    {
        [HttpGet]
        [Route("/thumb/{id}.jpg")]
        public IActionResult RedirectThumb(string id)
        { 
            return Redirect($"https://b.ppy.sh/thumb/{id}.jpg");
        }

        [HttpGet]
        [Route("/preview/{id}.mp3")]
        public IActionResult RedirectPreview(string id)
        {
            return Redirect($"https://b.ppy.sh/preview/{id}.mp3");
        }
        
    }
}