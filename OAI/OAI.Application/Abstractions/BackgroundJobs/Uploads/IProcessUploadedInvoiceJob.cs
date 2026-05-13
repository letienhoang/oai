namespace OAI.Application.Abstractions.BackgroundJobs.Uploads;

public interface IProcessUploadedInvoiceJob
{
    Task ProcessAsync(
        Guid uploadId,
        CancellationToken cancellationToken = default);
}