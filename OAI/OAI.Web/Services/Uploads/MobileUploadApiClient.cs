using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;
using OAI.Web.Options;

namespace OAI.Web.Services.Uploads;

public sealed class MobileUploadApiClient : IMobileUploadApiClient
{
    private const string InternalApiKeyHeaderName = "X-OAI-Internal-Api-Key";

    private readonly HttpClient _httpClient;
    private readonly ILogger<MobileUploadApiClient> _logger;
    private readonly InternalApiOptions _internalApiOptions;

    public MobileUploadApiClient(
        HttpClient httpClient,
        ILogger<MobileUploadApiClient> logger,
        IOptions<InternalApiOptions> internalApiOptions)
    {
        _httpClient = httpClient;
        _logger = logger;
        _internalApiOptions = internalApiOptions.Value;
    }

    public async Task<MobileUploadApiResponse> UploadAsync(
        IBrowserFile file,
        long maxFileSize,
        string? uploadedByUserId,
        string? uploadedByUserName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_internalApiOptions.ApiKey))
        {
            throw new InvalidOperationException("InternalApi:ApiKey is required.");
        }

        await using var stream = file.OpenReadStream(maxFileSize);

        using var form = new MultipartFormDataContent();
        using var fileContent = new StreamContent(stream);

        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType);

        form.Add(fileContent, "File", file.Name);

        if (!string.IsNullOrWhiteSpace(uploadedByUserId))
            form.Add(new StringContent(uploadedByUserId), "UploadedByUserId");

        if (!string.IsNullOrWhiteSpace(uploadedByUserName))
            form.Add(new StringContent(uploadedByUserName), "UploadedByUserName");

        _logger.LogInformation(
            "Sending mobile upload to OAI.Api. FileName: {FileName}, Size: {FileSize}",
            file.Name,
            file.Size);

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/uploads")
        {
            Content = form
        };

        request.Headers.Add(InternalApiKeyHeaderName, _internalApiOptions.ApiKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException(
                "The API rejected the upload because the current user is not authenticated.");
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new UnauthorizedAccessException(
                "The API rejected the upload because the current user is not allowed to upload invoices.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(error)
                    ? $"Upload API returned {(int)response.StatusCode}."
                    : error);
        }

        var result = await response.Content.ReadFromJsonAsync<MobileUploadApiResponse>(
            cancellationToken: cancellationToken);

        return result ?? throw new InvalidOperationException("Upload API returned an empty response.");
    }
}
