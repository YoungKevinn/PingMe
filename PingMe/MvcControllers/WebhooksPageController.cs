using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PingMe.MvcControllers;

[Route("webhooks")]
[Authorize]
public class WebhooksPageController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View("~/Views/Webhooks/Index.cshtml");
}
