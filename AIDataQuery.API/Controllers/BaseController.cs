using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace AIDataQuery.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase
{
    protected int CurrentUserId
    {
        get
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }
    }

    protected string CurrentUsername
    {
        get
        {
            var claim = User.FindFirst(ClaimTypes.Name);
            return claim?.Value ?? string.Empty;
        }
    }

    protected bool IsAdmin
    {
        get
        {
            var claim = User.FindFirst(ClaimTypes.Role);
            return claim?.Value == "Admin";
        }
    }

    protected string? ClientIp
    {
        get
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }
    }
}
