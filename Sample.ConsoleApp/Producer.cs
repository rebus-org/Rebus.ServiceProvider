using System;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Bus;

namespace Sample.ConsoleApp;

public class Producer
{
    readonly IBus _bus;

    public Producer(IBus bus)
    {
        _bus = bus;
    }

    public void Produce()
    {
        var keepRunning = true;

        while (keepRunning)
        {
            Console.WriteLine(@"a) Send 100 jobs
q) Quit");
            var key = char.ToLower(Console.ReadKey(true).KeyChar);

            switch (key)
            {
                case 'a':
                    Send(100, _bus);
                    break;
                case 'q':
                    Console.WriteLine("Quitting");
                    keepRunning = false;
                    break;
            }
        }

        Console.WriteLine("Consumer listening - press ENTER to quit");
        Console.ReadLine();
    }

    static void Send(int numberOfMessages, IBus bus)
    {
        Console.WriteLine("Publishing {0} messages", numberOfMessages);

        var sendTasks = Enumerable.Range(0, numberOfMessages)
            .Select(i => new Message1())
            .Select(message => bus.Send(message))
            .ToArray();

        Task.WaitAll(sendTasks);
    }
}