using Microsoft.AspNetCore.Mvc;

namespace CampusSecondHand.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet("hello")]
        public string Hello()
        {
            return "Hello, 校园二手交易平台！";
        }
    }
}
