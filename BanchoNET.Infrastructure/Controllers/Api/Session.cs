using BanchoNET.Core.Models.Auth;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api;

public partial class ApiController
{
    [HttpPost("session/verify")]
    public async Task<IActionResult> Verify(
        [FromForm] SessionVerifyDto dto
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        if (string.IsNullOrEmpty(dto.code)) return BadRequest(new { error = "missing_code" });

        var ok = await auth.VerifySessionCode(uid, dto.code);
        if (!ok) return BadRequest(new { error = "invalid_or_expired_code" });
        
        return Ok(new { verified = true });
    }
}