using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PingMe.MvcControllers;

[Route("chat")]
[Authorize]
public class ChatController : Controller
{
    private void SetUserBag(int? peerId, int? groupId)
    {
        ViewBag.PeerId   = peerId;
        ViewBag.GroupId  = groupId;
        ViewBag.MyId     = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        ViewBag.MyName   = User.FindFirstValue("displayName") ?? User.FindFirstValue(ClaimTypes.Name) ?? "";
    }

    [HttpGet("")]
    public IActionResult Index(int? peerId, int? groupId)
    {
        SetUserBag(peerId, groupId);
        return View("~/Views/Chat/Index.cshtml");
    }

    [HttpGet("{peerId:int}")]
    public IActionResult WithPeer(int peerId)
    {
        SetUserBag(peerId, null);
        return View("~/Views/Chat/Index.cshtml");
    }

    [HttpGet("group/{groupId:int}")]
    public IActionResult WithGroup(int groupId)
    {
        SetUserBag(null, groupId);
        return View("~/Views/Chat/Index.cshtml");
    }
}
