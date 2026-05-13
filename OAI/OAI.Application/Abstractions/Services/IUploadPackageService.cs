using OAI.Application.Uploads.Dtos;

namespace OAI.Application.Abstractions.Services;

public interface IUploadPackageService
{
    Task<CreateUploadPackageResultDto> CreateAsync(
        CreateUploadPackageRequestDto request,
        CancellationToken cancellationToken = default);
}