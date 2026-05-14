using Hangfire;
using OAI.Application;
using OAI.Application.Abstractions.BackgroundJobs;
using OAI.Infrastructure;
using OAI.Infrastructure.Hangfire;
using OAI.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOaiHangfireStorage(builder.Configuration);

builder.Services.AddHangfireServer(options =>
{
    options.ServerName = $"OAI.Worker:{Environment.MachineName}";
    options.WorkerCount = Math.Max(Environment.ProcessorCount, 2);
    options.Queues = [ BackgroundJobQueues.Default, BackgroundJobQueues.Uploads, BackgroundJobQueues.Ocr ];
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
