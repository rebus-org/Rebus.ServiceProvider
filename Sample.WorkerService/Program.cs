using Rebus.Config;
using Rebus.Transport.InMem;
using Sample.WorkerService;
using Sample.WorkerService.Handlers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<Worker>();

builder.Services.AddRebus(
    configure => configure
        .Transport(t => t.UseInMemoryTransport(new(), "just an in-mem queue with a wonky name"))
);

builder.Services.AddRebusHandler<CurrentTimeHandler>();

var host = builder.Build();
host.Run();
