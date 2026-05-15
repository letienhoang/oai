using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAI.Application.Files;
using OAI.Infrastructure.Identity;
using System.Net.Mime;

namespace OAI.Api.Controllers;

[Authorize(Policy = ApplicationPolicies.ViewInvoices)]
[ApiController]
[Route("api/files")]
public sealed class FilesController : ControllerBase
{
    private readonly IFileDownloadService _fileDownloadService;

    public FilesController(IFileDownloadService fileDownloadService)
    {
        _fileDownloadService = fileDownloadService;
    }

    /// <summary>
    /// Downloads an invoice source file or stored PDF page preview.
    /// </summary>
    [HttpGet("{fileId:guid}/download")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var result = await _fileDownloadService.GetDownloadableFileAsync(
            fileId,
            cancellationToken);

        if (!result.Succeeded)
        {
            return result.ErrorCode switch
            {
                DownloadableFileErrorCode.NotFound => NotFound(new { message = result.ErrorMessage }),
                DownloadableFileErrorCode.PhysicalFileMissing => NotFound(new { message = result.ErrorMessage }),
                DownloadableFileErrorCode.UnsafePath => Forbid(),
                _ => BadRequest(new { message = result.ErrorMessage })
            };
        }

        return PhysicalFile(
            result.PhysicalPath!,
            result.ContentType ?? "application/octet-stream",
            result.FileName ?? "download");
    }

    /// <summary>
    /// Previews an invoice source file or stored PDF page image inline.
    /// </summary>
    [HttpGet("{fileId:guid}/preview")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<IActionResult> Preview(
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var result = await _fileDownloadService.GetPreviewableFileAsync(
            fileId,
            cancellationToken);

        if (!result.Succeeded)
        {
            return result.ErrorCode switch
            {
                DownloadableFileErrorCode.NotFound => NotFound(new { message = result.ErrorMessage }),
                DownloadableFileErrorCode.PhysicalFileMissing => NotFound(new { message = result.ErrorMessage }),
                DownloadableFileErrorCode.UnsafePath => Forbid(),
                DownloadableFileErrorCode.UnsupportedContentType => StatusCode(
                    StatusCodes.Status415UnsupportedMediaType,
                    new { message = result.ErrorMessage }),
                _ => BadRequest(new { message = result.ErrorMessage })
            };
        }

        Response.Headers.ContentDisposition = new ContentDisposition
        {
            Inline = true,
            FileName = result.FileName ?? "preview"
        }.ToString();

        return PhysicalFile(
            result.PhysicalPath!,
            result.ContentType!);
    }
}
