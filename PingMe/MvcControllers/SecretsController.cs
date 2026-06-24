using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PingMe.MvcControllers;

[Route("secrets")]
[Authorize]
public class SecretsController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View("~/Views/Secrets/Index.cshtml");
}

[Route("secret")]
public class SecretOpenController : Controller
{
    [HttpGet("{token}")]
    [AllowAnonymous]
    public IActionResult Open(string token)
    {
        return View("~/Views/Secrets/Open.cshtml", token);
    }
}
