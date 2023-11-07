using System;
using System.Threading.Tasks;
using Rebus.Handlers;

namespace Sample.ConsoleApp;

public class Handler1 : IHandleMessages<Message1>
{
    public Task Handle(Message1 message)
    {
        Console.WriteLine($"Handler1 received : {message}");

        return Task.CompletedTask;
    }
}