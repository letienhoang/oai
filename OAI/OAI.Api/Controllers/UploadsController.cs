using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAI.Api.Contracts.Uploads;
using OAI.Application.Abstractions.Services;
using OAI.Application.Invoices.Dtos;
using OAI.Infrastructure.Identity;

namespace OAI.Api.Controllers;

[Authorize(Policy = ApplicationPolicies.UploadInvoices)]
[ApiController]
[Route("api/uploads")]
[Produces("application/json")]
public sealed class UploadsController : ControllerBase
{
    private const long MaxFileSize = 20 * 1024 * 1024;

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".tif",
        ".tiff"
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".tif",
        ".tiff",
        ".pdf",
        ".zip"
    };

    private readonly IInvoiceProcessingService _invoiceProcessingService;
    private readonly ILogger<UploadsController> _logger;

    public UploadsController(
        IInvoiceProcessingService invoiceProcessingService,
        ILogger<UploadsController> logger)
    {
        _invoiceProcessingService = invoiceProcessingService;
        _logger = logger;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadInvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(UploadUnsupportedFileResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<IActionResult> UploadSingleFile(
        [FromForm] UploadInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var file = request.File;
        if (file is null)
        {
            return BadRequest(new
            {
                message = "File is required."
            });
        }

        if (file.Length <= 0)
        {
            return BadRequest(new
            {
                message = "File is empty."
            });
        }

        if (file.Length > MaxFileSize)
        {
            return StatusCode(
                StatusCodes.Status413PayloadTooLarge,
                new
                {
                    message = "File size exceeds the 20 MB limit."
                });
        }

        if (string.IsNullOrWhiteSpace(file.FileName))
        {
            return BadRequest(new
            {
                message = "File name is required."
            });
        }

        var extension = Path.GetExtension(file.FileName);

        if (string.IsNullOrWhiteSpace(extension) ||
            !AllowedExtensions.Contains(extension))
        {
            return BadRequest(new
            {
                message = "Unsupported file format. Allowed formats: jpg, jpeg, png, tif, tiff, pdf, zip."
            });
        }

        if (!ImageExtensions.Contains(extension))
        {
            var fileType = extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase)
                ? "PDF"
                : "ZIP";

            return Accepted(new UploadUnsupportedFileResponse(
                FileName: file.FileName,
                FileType: fileType,
                Status: "AcceptedButNotProcessed",
                Message: $"{fileType} upload contract is available, but processing will be implemented in the file/batch processing phase."));
        }

        try
        {
            _logger.LogInformation(
                "Uploading invoice through API. FileName: {FileName}, Size: {FileSize}",
                file.FileName,
                file.Length);

            await using var stream = file.OpenReadStream();

            InvoiceUploadResultDto result = await _invoiceProcessingService.UploadInvoiceAsync(
                file.FileName,
                stream,
                cancellationToken);

            var response = new UploadInvoiceResponse(
                InvoiceId: result.InvoiceId,
                FileName: result.FileName,
                Status: result.Status,
                Message: result.Message,
                MessageCode: result.MessageCode);

            if (result.Status.Equals("Processed", StringComparison.OrdinalIgnoreCase))
            {
                return Ok(response);
            }

            return BadRequest(response);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Invoice upload failed through API. FileName: {FileName}",
                file.FileName);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message = "Invoice upload failed."
                });
        }
    }
}