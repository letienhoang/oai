using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;

namespace OAI.Web.Services;

public sealed class CurrentUserAuthorizationService
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly IAuthorizationService _authorizationService;

    public CurrentUserAuthorizationService(
        AuthenticationStateProvider authenticationStateProvider,
        IAuthorizationService authorizationService)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _authorizationService = authorizationService;
    }

    public async Task<bool> IsAuthorizedAsync(string policyName)
    {
        var authenticationState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authenticationState.User;

        if (user.Identity?.IsAuthenticated != true)
            return false;

        var result = await _authorizationService.AuthorizeAsync(user, policyName);
        return result.Succeeded;
    }
}
