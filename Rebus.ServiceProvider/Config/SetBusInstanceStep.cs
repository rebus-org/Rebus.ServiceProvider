using System;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Injection;
using Rebus.Pipeline;

namespace Rebus.Config;

class SetBusInstanceStep : IIncomingStep
{
    readonly IResolutionContext _resolutionContext;

    public SetBusInstanceStep(IResolutionContext resolutionContext)
    {
        _resolutionContext = resolutionContext;
    }

    public async Task Process(IncomingStepContext context, Func<Task> next)
    {
        Console.WriteLine("Getting bus from resolution context");
        var bus = _resolutionContext.Get<IBus>();
        Console.WriteLine("Saving bus to incoming step context");
        context.Save(bus);
        Console.WriteLine("Calling the rest of the pipeline");
        await next();
    }
}