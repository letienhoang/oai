using System.Linq.Expressions;

namespace OAI.Application.Abstractions.BackgroundJobs;

public interface IBackgroundJobClient
{
    Task<string> EnqueueAsync<TJob>(
        Expression<Func<TJob, Task>> jobExpression,
        string queue = BackgroundJobQueues.Default,
        CancellationToken cancellationToken = default)
        where TJob : class;
}