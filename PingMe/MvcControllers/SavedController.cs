using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PingMe.MvcControllers;

[Route("saved")]
[Authorize]
public class SavedController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View("~/Views/Saved/Index.cshtml");
}
