namespace Reflexor.IntegrationTests;

public class StaticMemberTests
{
    [Fact]
    public void Mutates_static_members_on_class()
    {
        StaticMemberContainerProxy.Version = 100;

        Assert.Equal(100, StaticMemberContainer.Version);
        Assert.Equal(101, StaticMemberContainerProxy.SetVersion(101));
        Assert.Equal(101, StaticMemberContainerProxy.Version);
    }
}

[Reflexor]
public class StaticMemberContainer
{
    public static int Version { get; private set; } = 42;

    // ReSharper disable once UnusedMember.Global
    internal static int SetVersion(int version) => Version = version;
}
