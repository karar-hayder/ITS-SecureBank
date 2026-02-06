using Microsoft.AspNetCore.Mvc;

namespace API.Common
{
    [ApiController]
    [Route("[controller]")]
    public class BaseController : ControllerBase
    {
        protected int UserId => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
    }
}
