using System.Net.Mime;
using OAI.Application.Files;
using OAI.Infrastructure.Identity;

namespace OAI.Web.Endpoints;

public static class FileEndpointExtensions
{
    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/files")
            .RequireAuthorization(ApplicationPolicies.ViewInvoices);

        group.MapGet("/{fileId:guid}/preview", async Task<IResult> (
            Guid fileId,
            IFileDownloadService fileDownloadService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await fileDownloadService.GetPreviewableFileAsync(
                fileId,
                cancellationToken);

            if (!result.Succeeded)
            {
                return ToErrorResult(result);
            }

            httpContext.Response.Headers.ContentDisposition = new ContentDisposition
            {
                Inline = true,
                FileName = result.FileName ?? "preview"
            }.ToString();

            var stream = File.OpenRead(result.PhysicalPath!);

            return Results.File(
                stream,
                result.ContentType!,
                enableRangeProcessing: true);
        });

        group.MapGet("/{fileId:guid}/download", async Task<IResult> (
            Guid fileId,
            IFileDownloadService fileDownloadService,
            CancellationToken cancellationToken) =>
        {
            var result = await fileDownloadService.GetDownloadableFileAsync(
                fileId,
                cancellationToken);

            if (!result.Succeeded)
            {
                return ToErrorResult(result);
            }

            var stream = File.OpenRead(result.PhysicalPath!);

            return Results.File(
                stream,
                result.ContentType ?? "application/octet-stream",
                result.FileName ?? "download",
                enableRangeProcessing: true);
        });

        return endpoints;
    }

    private static IResult ToErrorResult(DownloadableFileResult result)
    {
        var message = result.ErrorMessage ?? "File is not available.";

        return result.ErrorCode switch
        {
            DownloadableFileErrorCode.NotFound => Results.NotFound(new { message }),
            DownloadableFileErrorCode.PhysicalFileMissing => Results.NotFound(new { message }),
            DownloadableFileErrorCode.UnsafePath => Results.Forbid(),
            DownloadableFileErrorCode.UnsupportedContentType => Results.Json(
                new { message },
                statusCode: StatusCodes.Status415UnsupportedMediaType),
            _ => Results.BadRequest(new { message })
        };
    }
}
