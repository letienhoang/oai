using System.Linq.Expressions;
using Hangfire.Common;
using Hangfire.States;
using OAI.Application.Abstractions.BackgroundJobs;
using HangfireBackgroundJobClientContract = Hangfire.IBackgroundJobClient;
using OaiBackgroundJobClientContract = OAI.Application.Abstractions.BackgroundJobs.IBackgroundJobClient;

namespace OAI.Infrastructure.BackgroundJobs;

public sealed class HangfireBackgroundJobClient : OaiBackgroundJobClientContract
{
    private readonly HangfireBackgroundJobClientContract _hangfireClient;

    public HangfireBackgroundJobClient(
        HangfireBackgroundJobClientContract hangfireClient)
    {
        _hangfireClient = hangfireClient;
    }

    public Task<string> EnqueueAsync<TJob>(
        Expression<Func<TJob, Task>> jobExpression,
        string queue = BackgroundJobQueues.Default,
        CancellationToken cancellationToken = default)
        where TJob : class
    {
        ArgumentNullException.ThrowIfNull(jobExpression);

        var effectiveQueue = string.IsNullOrWhiteSpace(queue)
            ? BackgroundJobQueues.Default
            : queue;
        
        var job = Job.FromExpression(jobExpression);

        var jobId = _hangfireClient.Create(
            job,
            new EnqueuedState(effectiveQueue));

        return Task.FromResult(jobId);
    }
}