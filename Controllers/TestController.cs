using Microsoft.AspNetCore.Mvc;

namespace SqlMonitor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Test controller works!");
        }
    }
} 