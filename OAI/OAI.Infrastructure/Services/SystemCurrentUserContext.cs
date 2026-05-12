using OAI.Application.Abstractions.Services;

namespace OAI.Infrastructure.Services;

public sealed class SystemCurrentUserContext : ICurrentUserContext
{
    public string? UserId => null;

    public string? UserName => "System";

    public bool IsAuthenticated => false;
}
