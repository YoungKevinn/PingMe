using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PingMe.MvcControllers;

[Route("sessions")]
[Authorize]
public class SessionsController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View("~/Views/Sessions/Index.cshtml");
}
