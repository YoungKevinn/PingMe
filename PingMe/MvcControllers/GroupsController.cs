using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PingMe.MvcControllers;

[Route("groups")]
[Authorize]
public class GroupsController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View("~/Views/Groups/Index.cshtml");

    [HttpGet("{id:int}")]
    public IActionResult Detail(int id)
    {
        ViewBag.GroupId = id;
        return View("~/Views/Groups/Detail.cshtml");
    }
}
