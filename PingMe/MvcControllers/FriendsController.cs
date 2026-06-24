using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PingMe.MvcControllers;

[Route("friends")]
[Authorize]
public class FriendsController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View("~/Views/Friends/Index.cshtml");
}
