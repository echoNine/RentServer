using Microsoft.AspNetCore.Mvc;

namespace RentServer.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class HelloController : BaseController
    {
        [HttpGet("world")]
        public JsonResult World()
        {
            return Success("it work!");
        }
    }
}