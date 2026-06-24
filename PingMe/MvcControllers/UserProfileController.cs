using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PingMe.MvcControllers;

[Route("user")]
[Authorize]
public class UserProfileController : Controller
{
    [HttpGet("{userId:int}")]
    public IActionResult Index(int userId)
    {
        ViewBag.UserId = userId;
        return View("~/Views/UserProfile/Index.cshtml");
    }
}
