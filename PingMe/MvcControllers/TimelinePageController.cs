using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PingMe.MvcControllers;

[Route("timeline")]
[Authorize]
public class TimelinePageController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View("~/Views/Timeline/Index.cshtml");
}
