namespace OAI.Domain.Enums;

public enum UploadBatchFileStatus
{
    Created = 0,
    Queued = 1,
    Processing = 2,
    Processed = 3,
    Failed = 4,
    Skipped = 5,
    Unsupported = 6
}