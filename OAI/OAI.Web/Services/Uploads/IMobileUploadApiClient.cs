using Microsoft.AspNetCore.Components.Forms;

namespace OAI.Web.Services.Uploads;

public interface IMobileUploadApiClient
{
    Task<MobileUploadApiResponse> UploadAsync(
        IBrowserFile file,
        long maxFileSize,
        string? uploadedByUserId,
        string? uploadedByUserName,
        CancellationToken cancellationToken);
}
