using Rebus.Bus;
using Rebus.Config;
using Rebus.Persistence.InMem;
using Rebus.Transport.InMem;
using Sample.MvcWebApp.Handlers;
#pragma warning disable CS4014

var network = new InMemNetwork();
var subscriptions = new InMemorySubscriberStore();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// There's two basic ways of adding Rebus to your app:
// 1) Adding to the host's container
// 2) Adding as independent background service

// Here's (1): Adding it to the host's container
builder.Services.AddRebus(
    configure => configure
        .Transport(t => t.UseInMemoryTransport(network, "bus1"))
        .Subscriptions(s => s.StoreInMemory(subscriptions))
);

// multiple bus instances can be added by setting isDefaultBus:false
builder.Services.AddRebus(
    configure => configure
        .Transport(t => t.UseInMemoryTransport(network, "bus2"))
        .Subscriptions(s => s.StoreInMemory(subscriptions)),

    isDefaultBus: false
);

// the two Rebus instances above will be sharing the host's container - they need this handler
builder.Services.AddRebusHandler<PrintReceivedMessageTogetherWithBusName>();

// Here's (2): Adding as independent background service
// (which means that it will have its own container instance, thus
// requring that you configure a separate service collection for it):
builder.Host.AddRebusService(
    // make the necessary registrations here
    services =>
    {
        services.AddRebusHandler<PrintReceivedMessageTogetherWithBusName>();

        services.AddRebus(
            configure => configure
                .Transport(t => t.UseInMemoryTransport(network, "bus3"))
                .Subscriptions(s => s.StoreInMemory(subscriptions)),

            onCreated: async bus => await bus.Subscribe<string>()
        );
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

// start the app asynchronously (thus starting all of the necessary background services)
var appTask = app.RunAsync();

Task.Run(async () =>
{
    // resolving IBus from the host's container will yield its default bus
    var bus = app.Services.GetRequiredService<IBus>();

    await Task.Delay(TimeSpan.FromSeconds(1));
    await bus.Advanced.Routing.Send("bus1", "Hello Bus 1 - this message want sent with .SendLocal");

    await Task.Delay(TimeSpan.FromSeconds(1));
    await bus.Advanced.Routing.Send("bus1", "Hello Bus 1 - this message was sent to queue 'bus1'");

    await Task.Delay(TimeSpan.FromSeconds(1));
    await bus.Advanced.Routing.Send("bus2", "Hello Bus 2 - this message was sent to queue 'bus2'");

    await Task.Delay(TimeSpan.FromSeconds(1));
    await bus.Publish("Hello Bus 3 - this message was published to default topic for System.String");
});




await appTask;