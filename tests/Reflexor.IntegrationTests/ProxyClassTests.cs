namespace Reflexor.IntegrationTests;

public class ProxyClassTests
{
    [Fact]
    public void Mutates_class()
    {
        var user = new UserClass
        {
            UserName = "username",
            Email = "email"
        };

        _ = new UserClassProxy(user)
        {
            UserName = "john_doe",
            Email = "john_doe@email.com"
        };

        Assert.Equal("john_doe", user.UserName);
        Assert.Equal("john_doe@email.com", user.Email);
    }
}

[Reflexor]
public class UserClass
{
    public required string UserName { get; init; }
    public required string Email { get; init; }
}
