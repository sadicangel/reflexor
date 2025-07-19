namespace Reflexor.IntegrationTests;

public class ProxyClassTests
{
    [Fact]
    public void Mutates_class()
    {
        var user = new UserRecord("username", "email");

        var proxy = new UserRecordProxy(user)
        {
            UserName = "john_doe",
            Email = "john_doe@email.com"
        };

        Assert.Equal("john_doe", user.UserName);
        Assert.Equal("john_doe@email.com", user.Email);
    }
}

[GenerateProxy]
public class UserClass
{
    public required string UserName { get; init; }
    public required string Email { get; init; }
}
