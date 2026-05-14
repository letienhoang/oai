namespace OAI.Api.Contracts.Auth;

public sealed class LoginApiRequest
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}