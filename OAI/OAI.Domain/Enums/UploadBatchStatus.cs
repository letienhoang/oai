namespace OAI.Domain.Enums;

public enum UploadBatchStatus
{
    Created = 0,
    Queued = 1,
    Processing = 2,
    Completed = 3,
    PartiallyFailed = 4,
    Failed = 5,
    Cancelled = 6
}