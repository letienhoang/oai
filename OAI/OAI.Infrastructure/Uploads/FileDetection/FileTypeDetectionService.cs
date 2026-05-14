using OAI.Application.Uploads.FileDetection;

namespace OAI.Infrastructure.Uploads.FileDetection;

public sealed class FileTypeDetectionService : IFileTypeDetectionService
{
    private const int HeaderLength = 16;

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.Ordinal)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".tif",
        ".tiff"
    };

    private static readonly HashSet<string> PdfExtensions = new(StringComparer.Ordinal)
    {
        ".pdf"
    };

    private static readonly HashSet<string> ZipExtensions = new(StringComparer.Ordinal)
    {
        ".zip"
    };

    private static readonly HashSet<string> ImageContentTypes = new(StringComparer.Ordinal)
    {
        "image/jpeg",
        "image/png",
        "image/tiff"
    };

    private static readonly HashSet<string> PdfContentTypes = new(StringComparer.Ordinal)
    {
        "application/pdf"
    };

    private static readonly HashSet<string> ZipContentTypes = new(StringComparer.Ordinal)
    {
        "application/zip",
        "application/x-zip-compressed"
    };

    public async Task<FileTypeDetectionResult> DetectAsync(
        Stream fileStream,
        string? fileName,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        var normalizedExtension = NormalizeExtension(fileName);
        var normalizedContentType = NormalizeContentType(contentType);
        var originalPosition = fileStream.CanSeek ? fileStream.Position : (long?)null;

        byte[] header = new byte[HeaderLength];
        var bytesRead = 0;

        try
        {
            while (bytesRead < HeaderLength)
            {
                var read = await fileStream.ReadAsync(
                    header.AsMemory(bytesRead, HeaderLength - bytesRead),
                    cancellationToken);

                if (read == 0)
                    break;

                bytesRead += read;
            }
        }
        finally
        {
            if (originalPosition.HasValue)
            {
                fileStream.Position = originalPosition.Value;
            }
        }

        var signatureDetection = DetectBySignature(
            header.AsSpan(0, bytesRead),
            normalizedExtension,
            normalizedContentType);

        if (signatureDetection is not null)
        {
            return signatureDetection;
        }

        return DetectByFallback(normalizedExtension, normalizedContentType);
    }

    private static FileTypeDetectionResult? DetectBySignature(
        ReadOnlySpan<byte> header,
        string? normalizedExtension,
        string? normalizedContentType)
    {
        if (StartsWith(header, [0x25, 0x50, 0x44, 0x46]))
        {
            return new FileTypeDetectionResult(
                DetectedUploadFileType.Pdf,
                normalizedExtension,
                normalizedContentType,
                "Detected by PDF signature.");
        }

        if (StartsWith(header, [0x50, 0x4B, 0x03, 0x04])
            || StartsWith(header, [0x50, 0x4B, 0x05, 0x06])
            || StartsWith(header, [0x50, 0x4B, 0x07, 0x08]))
        {
            return new FileTypeDetectionResult(
                DetectedUploadFileType.Zip,
                normalizedExtension,
                normalizedContentType,
                "Detected by ZIP signature.");
        }

        if (StartsWith(header, [0xFF, 0xD8, 0xFF]))
        {
            return new FileTypeDetectionResult(
                DetectedUploadFileType.Image,
                normalizedExtension,
                normalizedContentType,
                "Detected by JPEG signature.");
        }

        if (StartsWith(header, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]))
        {
            return new FileTypeDetectionResult(
                DetectedUploadFileType.Image,
                normalizedExtension,
                normalizedContentType,
                "Detected by PNG signature.");
        }

        if (StartsWith(header, [0x49, 0x49, 0x2A, 0x00])
            || StartsWith(header, [0x4D, 0x4D, 0x00, 0x2A]))
        {
            return new FileTypeDetectionResult(
                DetectedUploadFileType.Image,
                normalizedExtension,
                normalizedContentType,
                "Detected by TIFF signature.");
        }

        return null;
    }

    private static FileTypeDetectionResult DetectByFallback(
        string? normalizedExtension,
        string? normalizedContentType)
    {
        if (IsImageFallback(normalizedExtension, normalizedContentType))
        {
            return new FileTypeDetectionResult(
                DetectedUploadFileType.Image,
                normalizedExtension,
                normalizedContentType,
                "Detected by extension/content type fallback.");
        }

        if (IsPdfFallback(normalizedExtension, normalizedContentType))
        {
            return new FileTypeDetectionResult(
                DetectedUploadFileType.Pdf,
                normalizedExtension,
                normalizedContentType,
                "Detected by extension/content type fallback.");
        }

        if (IsZipFallback(normalizedExtension, normalizedContentType))
        {
            return new FileTypeDetectionResult(
                DetectedUploadFileType.Zip,
                normalizedExtension,
                normalizedContentType,
                "Detected by extension/content type fallback.");
        }

        return new FileTypeDetectionResult(
            DetectedUploadFileType.Unsupported,
            normalizedExtension,
            normalizedContentType,
            "Unsupported file type.");
    }

    private static bool IsImageFallback(string? normalizedExtension, string? normalizedContentType) =>
        (normalizedExtension is not null && ImageExtensions.Contains(normalizedExtension))
        || (normalizedContentType is not null && ImageContentTypes.Contains(normalizedContentType));

    private static bool IsPdfFallback(string? normalizedExtension, string? normalizedContentType) =>
        (normalizedExtension is not null && PdfExtensions.Contains(normalizedExtension))
        || (normalizedContentType is not null && PdfContentTypes.Contains(normalizedContentType));

    private static bool IsZipFallback(string? normalizedExtension, string? normalizedContentType) =>
        (normalizedExtension is not null && ZipExtensions.Contains(normalizedExtension))
        || (normalizedContentType is not null && ZipContentTypes.Contains(normalizedContentType));

    private static bool StartsWith(ReadOnlySpan<byte> header, ReadOnlySpan<byte> signature) =>
        header.Length >= signature.Length && header[..signature.Length].SequenceEqual(signature);

    private static string? NormalizeExtension(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        var extension = Path.GetExtension(fileName);
        return string.IsNullOrWhiteSpace(extension)
            ? null
            : extension.ToLowerInvariant();
    }

    private static string? NormalizeContentType(string? contentType)
    {
        return string.IsNullOrWhiteSpace(contentType)
            ? null
            : contentType.Trim().ToLowerInvariant();
    }
}
