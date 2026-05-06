using System.Security.Claims;
using OAI.Application.Abstractions.Services;

namespace OAI.Web.Services;

public sealed class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => IsAuthenticated
        ? User?.FindFirstValue(ClaimTypes.NameIdentifier)
        : null;

    public string? UserName
    {
        get
        {
            if (!IsAuthenticated)
                return null;

            return User?.Identity?.Name
                ?? User?.FindFirstValue(ClaimTypes.Email)
                ?? User?.FindFirstValue("name");
        }
    }

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;
}
