using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PingMe.Services;

namespace PingMe.Controllers;

[ApiController]
[Route("api/test_conv")]
public class TestConvController : ControllerBase
{
    private readonly IConversationService _conv;
    public TestConvController(IConversationService conv) => _conv = conv;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get()
    {
        try {
            var res = await _conv.GetConversationsAsync(1);
            return Ok(res);
        } catch (System.Exception ex) {
            return StatusCode(500, ex.ToString());
        }
    }
}
