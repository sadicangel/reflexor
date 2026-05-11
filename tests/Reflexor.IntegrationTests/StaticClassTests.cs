namespace Reflexor.IntegrationTests;

public class StaticClassTests
{
    [Fact]
    public void Mutates_static_class_members()
    {
        StaticConfigProxy.Version = 200;

        Assert.Equal(200, StaticConfig.Version);
        Assert.Equal(201, StaticConfigProxy.SetVersion(201));
        Assert.Equal(201, StaticConfigProxy.Version);
    }
}

[Reflexor]
public static class StaticConfig
{
    public static int Version { get; private set; } = 42;

    // ReSharper disable once UnusedMember.Global
    internal static int SetVersion(int version) => Version = version;
}
