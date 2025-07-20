namespace Reflexor.IntegrationTests;

public class StaticMethodTests
{
    [Fact]
    public void Mutates_class()
    {
        StaticMethodProxy.Version = 100;

        Assert.Equal(100, StaticMethod.Version);
    }
}

[GenerateProxy]
public class StaticMethod
{
    public static int Version { get; private set; } = 42;
    internal static int SetVersion(int version) => Version = version;
}
