using Autofac.Core.Resolving.Pipeline;

namespace Autofac.Extras.DynamicProxy.Test;

public class ProxyHelperFixture
{
    [Fact]
    public void ApplyProxy_NullResolveRequestContext()
    {
        const ResolveRequestContext ctx = null;
        Assert.Throws<ArgumentNullException>(() => ProxyHelpers.ApplyProxy(ctx!));
    }
}
