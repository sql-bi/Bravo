using Sqlbi.Bravo.Host;
using Xunit;

namespace Bravo.Tests.Host;

public class BravoApplicationBuilderTests
{
    /// <summary>
    /// Composition smoke test: builds the full service graph through the real builder. In DEBUG the
    /// builder enables ValidateOnBuild/ValidateScopes, so this fails on any unresolvable
    /// registration. Building does not bind Kestrel or start the host, so no port is claimed.
    /// </summary>
    [Fact]
    public void Build_ComposesTheFullServiceGraph()
    {
        using var context = FakeInitializationContext.Create();

        using var application = BravoApplication.CreateBuilder(context).Build();
    }
}
