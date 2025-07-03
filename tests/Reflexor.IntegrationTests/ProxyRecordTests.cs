namespace Reflexor.IntegrationTests;

public class ProxyRecordTests
{
    [Fact]
    public void Mutates_record()
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
public record UserRecord(string UserName, string Email);
