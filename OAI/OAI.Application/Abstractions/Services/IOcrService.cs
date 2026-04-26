using OAI.Application.Ocr.Dtos;

namespace OAI.Application.Abstractions.Services;

public interface IOcrService
{
    Task<OcrResultDto> ExtractTextAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default);
}