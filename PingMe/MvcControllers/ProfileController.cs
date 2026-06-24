using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PingMe.MvcControllers;

[Route("profile")]
[Authorize]
public class ProfileController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View("~/Views/Profile/Index.cshtml");
}
