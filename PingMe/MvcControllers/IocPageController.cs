using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PingMe.MvcControllers;

[Route("ioc")]
[Authorize]
public class IocPageController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View("~/Views/Ioc/Index.cshtml");
}
