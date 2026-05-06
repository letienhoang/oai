namespace OAI.Application.Ocr.Dtos;

public sealed record OcrResultDto
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }

    public string SourceFileName { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;

    public float Confidence { get; init; }

    public IReadOnlyList<string> Lines { get; init; } = Array.Empty<string>();
}