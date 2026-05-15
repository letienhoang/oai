using OAI.Application.Uploads.FileDetection;
using OAI.Infrastructure.Uploads.FileDetection;

namespace OAI.Infrastructure.Tests.Uploads.FileDetection;

public sealed class FileTypeDetectionServiceTests
{
    private readonly FileTypeDetectionService _service = new();

    [Theory]
    [InlineData(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }, DetectedUploadFileType.Pdf)]
    [InlineData(new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14 }, DetectedUploadFileType.Zip)]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, DetectedUploadFileType.Image)]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, DetectedUploadFileType.Image)]
    [InlineData(new byte[] { 0x49, 0x49, 0x2A, 0x00 }, DetectedUploadFileType.Image)]
    [InlineData(new byte[] { 0x4D, 0x4D, 0x00, 0x2A }, DetectedUploadFileType.Image)]
    public async Task DetectAsync_DetectsKnownSignatures(byte[] header, DetectedUploadFileType expectedFileType)
    {
        await using var stream = new MemoryStream(header);

        var result = await _service.DetectAsync(stream, "upload.bin", "application/octet-stream");

        Assert.Equal(expectedFileType, result.FileType);
        Assert.True(result.IsSupported);
    }

    [Fact]
    public async Task DetectAsync_DetectsUnsupportedRandomBytes()
    {
        await using var stream = new MemoryStream([0x01, 0x02, 0x03, 0x04]);

        var result = await _service.DetectAsync(stream, "upload.bin", "application/octet-stream");

        Assert.Equal(DetectedUploadFileType.Unsupported, result.FileType);
        Assert.False(result.IsSupported);
        Assert.Equal("Unsupported file type.", result.Reason);
    }

    [Theory]
    [InlineData("invoice.pdf", "application/octet-stream", DetectedUploadFileType.Pdf)]
    [InlineData("invoice.bin", "application/pdf", DetectedUploadFileType.Pdf)]
    [InlineData("archive.zip", "application/octet-stream", DetectedUploadFileType.Zip)]
    [InlineData("archive.bin", "application/x-zip-compressed", DetectedUploadFileType.Zip)]
    [InlineData("photo.jpg", "application/octet-stream", DetectedUploadFileType.Image)]
    [InlineData("photo.bin", "image/tiff", DetectedUploadFileType.Image)]
    public async Task DetectAsync_DetectsByExtensionOrContentTypeFallback(
        string fileName,
        string contentType,
        DetectedUploadFileType expectedFileType)
    {
        await using var stream = new MemoryStream();

        var result = await _service.DetectAsync(stream, fileName, contentType);

        Assert.Equal(expectedFileType, result.FileType);
        Assert.Equal("Detected by extension/content type fallback.", result.Reason);
    }

    [Fact]
    public async Task DetectAsync_SignatureWinsOverWrongExtensionAndContentType()
    {
        await using var stream = new MemoryStream([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        var result = await _service.DetectAsync(stream, "invoice.pdf", "application/pdf");

        Assert.Equal(DetectedUploadFileType.Image, result.FileType);
        Assert.Equal(".pdf", result.NormalizedExtension);
        Assert.Equal("application/pdf", result.NormalizedContentType);
        Assert.Equal("Detected by PNG signature.", result.Reason);
    }

    [Fact]
    public async Task DetectAsync_RestoresStreamPositionWhenStreamIsSeekable()
    {
        await using var stream = new MemoryStream([0x00, 0x00, 0x25, 0x50, 0x44, 0x46]);
        stream.Position = 2;

        var result = await _service.DetectAsync(stream, "invoice.pdf", "application/pdf");

        Assert.Equal(DetectedUploadFileType.Pdf, result.FileType);
        Assert.Equal(2, stream.Position);
    }
}
