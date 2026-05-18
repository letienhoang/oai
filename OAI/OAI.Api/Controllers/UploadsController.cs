using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAI.Api.Contracts.Uploads;
using OAI.Api.Security;
using OAI.Application.Abstractions.BackgroundJobs;
using OAI.Application.Abstractions.BackgroundJobs.Uploads;
using OAI.Application.Abstractions.Persistence;
using OAI.Application.Abstractions.Services;
using OAI.Application.Invoices.Dtos;
using OAI.Application.Uploads.Dtos;
using OAI.Domain.Entities;
using OAI.Domain.Enums;
using OAI.Infrastructure.Identity;

namespace OAI.Api.Controllers;

[ApiController]
[Route("api/uploads")]
[Produces("application/json")]
public sealed class UploadsController : ControllerBase
{
    private const long MaxFileSize = 20 * 1024 * 1024;

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

    private readonly IUploadPackageService _uploadPackageService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IUploadBatchRepository _uploadBatchRepository;
    private readonly ILogger<UploadsController> _logger;

    public UploadsController(
        IUploadPackageService uploadPackageService,
        IBackgroundJobClient backgroundJobClient,
        IUploadBatchRepository uploadBatchRepository,
        ILogger<UploadsController> logger)
    {
        _uploadPackageService = uploadPackageService;
        _backgroundJobClient = backgroundJobClient;
        _uploadBatchRepository = uploadBatchRepository;
        _logger = logger;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadPackageResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [RequestSizeLimit(MaxFileSize)]
    [AllowAnonymous]
    [InternalApiKeyAuthorize]
    public async Task<IActionResult> UploadPackage(
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

        try
        {
            _logger.LogInformation(
                "Creating upload package through API. FileName: {FileName}, ContentType: {ContentType}, Size: {FileSize}",
                file.FileName,
                file.ContentType,
                file.Length);

            await using var stream = file.OpenReadStream();

            var uploadedByUserId = Guid.TryParse(request.UploadedByUserId, out var userId)
                ? userId
                : (Guid?)null;

            var result = await _uploadPackageService.CreateAsync(
                new CreateUploadPackageRequestDto(
                    FileName: file.FileName,
                    ContentType: string.IsNullOrWhiteSpace(file.ContentType)
                        ? "application/octet-stream"
                        : file.ContentType,
                    FileSizeBytes: file.Length,
                    Content: stream,
                    UploadedByUserId: uploadedByUserId,
                    UploadedByUserName: string.IsNullOrWhiteSpace(request.UploadedByUserName)
                        ? User.Identity?.Name
                        : request.UploadedByUserName),
                cancellationToken);
            
            var backgroundJobId = await _backgroundJobClient.EnqueueAsync<IProcessUploadBatchJob>(
                job => job.ProcessAsync(result.UploadBatchId, CancellationToken.None),
                BackgroundJobQueues.Uploads,
                cancellationToken);
            
            _logger.LogInformation(
                "Upload batch processing job enqueued. UploadBatchId: {UploadBatchId}, BackgroundJobId: {BackgroundJobId}",
                result.UploadBatchId,
                backgroundJobId);

            var response = new UploadPackageResponse(
                result.UploadBatchId,
                result.BatchCode,
                result.TotalFiles,
                result.Status.ToString(),
                backgroundJobId,
                result.Files
                    .Select(fileResult => new UploadPackageFileResponse(
                        fileResult.UploadBatchFileId,
                        fileResult.OriginalFileName,
                        fileResult.StoredFilePath,
                        fileResult.ContentType,
                        fileResult.FileSizeBytes,
                        fileResult.Status.ToString()))
                    .ToArray());

            return Accepted(response);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                ex,
                "Upload package validation failed. FileName: {FileName}",
                file.FileName);

            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(
                ex,
                "Upload package argument validation failed. FileName: {FileName}",
                file.FileName);

            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Upload package creation failed. FileName: {FileName}",
                file.FileName);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message = "Upload package creation failed."
                });
        }
    }
    
    [HttpGet("{batchId:guid}")]
    [Authorize(Policy = ApplicationPolicies.UploadInvoices)]
    [ProducesResponseType(typeof(UploadBatchStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBatchStatus(
        Guid batchId,
        CancellationToken cancellationToken)
    {
        if (batchId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "Batch id is required."
            });
        }

        var uploadBatch = await _uploadBatchRepository.GetByIdWithFilesAsync(
            batchId,
            cancellationToken);

        if (uploadBatch is null)
        {
            return NotFound(new
            {
                message = "Upload batch was not found."
            });
        }

        return Ok(MapBatchStatus(uploadBatch));
    }

    [HttpGet("{batchId:guid}/files")]
    [Authorize(Policy = ApplicationPolicies.UploadInvoices)]
    [ProducesResponseType(typeof(IReadOnlyList<UploadBatchFileStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBatchFiles(
        Guid batchId,
        CancellationToken cancellationToken)
    {
        if (batchId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "Batch id is required."
            });
        }

        var uploadBatch = await _uploadBatchRepository.GetByIdAsync(
            batchId,
            cancellationToken);

        if (uploadBatch is null)
        {
            return NotFound(new
            {
                message = "Upload batch was not found."
            });
        }

        var files = await _uploadBatchRepository.GetFilesAsync(
            batchId,
            cancellationToken);

        return Ok(files
            .Select(MapBatchFileStatus)
            .ToArray());
    }
    
    private static UploadBatchStatusResponse MapBatchStatus(UploadBatch uploadBatch)
    {
        var files = uploadBatch.Files
            .OrderBy(x => x.CreatedAt)
            .Select(MapBatchFileStatus)
            .ToArray();

        var pendingFiles = uploadBatch.Files.Count(x =>
            x.Status is UploadBatchFileStatus.Created or UploadBatchFileStatus.Queued);

        var processingFiles = uploadBatch.Files.Count(x =>
            x.Status == UploadBatchFileStatus.Processing);

        var retryPendingFiles = uploadBatch.Files.Count(x =>
            x.Status == UploadBatchFileStatus.RetryPending);

        var unsupportedFiles = uploadBatch.Files.Count(x =>
            x.Status == UploadBatchFileStatus.Unsupported);

        return new UploadBatchStatusResponse(
            uploadBatch.Id,
            uploadBatch.BatchCode,
            uploadBatch.Status.ToString(),
            uploadBatch.TotalFiles,
            uploadBatch.ProcessedFiles,
            uploadBatch.FailedFiles,
            pendingFiles,
            processingFiles,
            retryPendingFiles,
            unsupportedFiles,
            uploadBatch.UploadedByUserName,
            uploadBatch.OriginalZipFilePath,
            uploadBatch.CreatedAt,
            uploadBatch.StartedAt,
            uploadBatch.CompletedAt,
            files);
    }

    private static UploadBatchFileStatusResponse MapBatchFileStatus(UploadBatchFile file)
    {
        return new UploadBatchFileStatusResponse(
            file.Id,
            file.UploadBatchId,
            file.OriginalFileName,
            file.StoredFilePath,
            file.ContentType,
            file.FileSizeBytes,
            file.Status.ToString(),
            file.InvoiceId,
            file.ErrorMessage,
            file.CreatedAt,
            file.ProcessingStartedAt,
            file.ProcessingCompletedAt);
    }
}
