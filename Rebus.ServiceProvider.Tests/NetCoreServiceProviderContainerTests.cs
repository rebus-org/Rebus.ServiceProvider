using NUnit.Framework;
using Rebus.Tests.Contracts.Activation;

namespace Rebus.ServiceProvider.Tests;

[TestFixture]
public class NetCoreServiceProviderContainerTests : ContainerTests<NetCoreServiceProviderActivationContext>
{
}