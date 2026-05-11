namespace Reflexor.IntegrationTests;

public class NullableMemberTests
{
    [Fact]
    public void Mutates_nullable_members()
    {
        var user = new NullableUser { Name = "Ada" };

        var proxy = new NullableUserProxy(user) { Name = null };

        Assert.Null(user.Name);
        Assert.Null(proxy.Echo(null));
        Assert.Equal("Grace", proxy.Echo("Grace"));
    }
}

[Reflexor]
public class NullableUser
{
    public string? Name { get; init; }

    // ReSharper disable once UnusedMember.Global
    public string? Echo(string? value) => value;
}
