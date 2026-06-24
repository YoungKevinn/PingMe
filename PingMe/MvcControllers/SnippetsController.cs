using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PingMe.MvcControllers;

[Route("snippets")]
[Authorize]
public class SnippetsController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View("~/Views/Snippets/Index.cshtml");

    [AllowAnonymous]
    [HttpGet("share/{token}")]
    public IActionResult Share(string token) => View("~/Views/Snippets/Share.cshtml", model: token);
}
