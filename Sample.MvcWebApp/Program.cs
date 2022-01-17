using System.Text;
using System.Text.Json;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Transport.InMem;
using Sample.MvcWebApp.Handlers;

InMemTransportMessage GetMessage(string message)
{
    var headers = new Dictionary<string, string>
    {
        [Headers.MessageId] = Guid.NewGuid().ToString(),
        [Headers.ContentType] = "application/json; chartset=utf-8"
    };
    var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
    var transportMessage = new TransportMessage(headers, body);
    return new InMemTransportMessage(transportMessage);
}

var network = new InMemNetwork();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

//builder.Services.AddRebusNew(
//    configure => configure
//        .Transport(t => t.UseInMemoryTransport(network, "bus1"))
//        .Options(o => o.SetBusName("bus1"))
//);

//builder.Services.AddRebusNew(
//    configure => configure
//        .Transport(t => t.UseInMemoryTransport(network, "bus2"))
//        .Options(o => o.SetBusName("bus2"))
//);

//builder.Services.AddRebusHandler<PrintBusName>();

//builder.Host.AddRebusService(
//    services => services
//        .AddRebus(configure => configure.Transport(t => t.UseInMemoryTransport(network, "bus1")))
//        .AddRebusHandler<PrintBusName>()
//);

builder.Host.AddRebusService(
    services => services
        .AddRebusHandler<PrintBusName>()
        .AddRebus(
            configure => configure
                .Transport(t => t.UseInMemoryTransport(network, "who-cares"))
        )
        .AddRebus(
            isDefaultBus: false,

            configure: configure => configure
                .Transport(t => t.UseInMemoryTransport(network, "who-cares2"))
        )
);

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

Task.Run(async () =>
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    network.Deliver("bus1", GetMessage("hej bus1"));
    await Task.Delay(TimeSpan.FromSeconds(1));
    network.Deliver("bus2", GetMessage("hej bus2"));
});

app.Run();
