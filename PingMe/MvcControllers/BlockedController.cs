using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PingMe.MvcControllers;

[Route("blocked")]
[Authorize]
public class BlockedController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View("~/Views/Blocked/Index.cshtml");
}
