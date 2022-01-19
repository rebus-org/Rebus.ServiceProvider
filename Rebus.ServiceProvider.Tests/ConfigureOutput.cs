using System;
using NUnit.Framework;

namespace Rebus.ServiceProvider.Tests;

[SetUpFixture]
public class ConfigureOutput
{
    [OneTimeSetUp]
    public void ConfigureContinuousOutput() => Console.SetOut(TestContext.Progress);
}