using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.ServiceProvider.Tests.Internals;

#pragma warning disable CS1998

namespace Rebus.ServiceProvider.Tests.Assumptions;

[TestFixture]
public class TestAsyncHelpers
{
    [Test]
    public async Task DoesNotInvokeTaskMultipleTimes()
    {
        var counter = 0;

        var lazy = new Lazy<Task>(async () => Interlocked.Increment(ref counter));
        var task = lazy.Value;

        await task;
        AsyncHelpers.RunSync(async () => await task);

        Assert.That(counter, Is.EqualTo(1));
    }
}