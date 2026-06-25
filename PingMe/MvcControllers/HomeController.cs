using Microsoft.AspNetCore.Mvc;

namespace PingMe.MvcControllers;

[Route("")]
public class HomeController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => Redirect("/chat");
}
