using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PingMe.MvcControllers;

[Route("search")]
[Authorize]
public class SearchPageController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View("~/Views/Search/Index.cshtml");
}
