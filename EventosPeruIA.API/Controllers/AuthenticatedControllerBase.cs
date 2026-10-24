using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace EventosPeruIA.API.Controllers;

public abstract class AuthenticatedControllerBase : ControllerBase
{
    protected bool TryGetUserId(out int userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        return int.TryParse(value, out userId);
    }
}
