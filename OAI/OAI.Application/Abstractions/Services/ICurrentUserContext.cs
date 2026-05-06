namespace OAI.Application.Abstractions.Services;

public interface ICurrentUserContext
{
    string? UserId { get; }

    string? UserName { get; }

    bool IsAuthenticated { get; }
}
