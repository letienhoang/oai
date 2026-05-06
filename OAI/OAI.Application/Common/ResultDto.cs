namespace OAI.Application.Common;

public sealed record ResultDto
{
    public bool IsSuccess { get; init; }
    public string? Message { get; init; }

    public static ResultDto Success(string? message = null)
        => new() { IsSuccess = true, Message = message };

    public static ResultDto Failure(string message)
        => new() { IsSuccess = false, Message = message };
}