namespace Reflexor.IntegrationTests;

public class DefaultProxyTests
{
    [Fact]
    public void Method_call_on_default_proxy_throws()
    {
        var proxy = default(CalculatorProxy);

        Assert.Throws<InvalidOperationException>(() => proxy.Add(1, 2));
    }
}

[Reflexor]
public class Calculator
{
    // ReSharper disable once UnusedMember.Global
    internal int Add(int left, int right) => left + right;
}
