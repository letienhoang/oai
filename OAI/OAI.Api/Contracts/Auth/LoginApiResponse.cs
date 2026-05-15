namespace OAI.Api.Contracts.Auth;

public sealed record LoginApiResponse(
    string Status,
    string Message,
    string? UserName);