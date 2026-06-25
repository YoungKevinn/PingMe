using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PingMe.MvcControllers;

[Route("tasks")]
[Authorize]
public class TasksPageController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View("~/Views/Tasks/Index.cshtml");
}
