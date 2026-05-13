using OAI.Application;
using OAI.Infrastructure;
using OAI.Infrastructure.Hangfire;
using OAI.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOaiHangfireStorage(builder.Configuration);

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
