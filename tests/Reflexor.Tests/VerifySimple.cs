using Reflexor.Tests.Helpers;

namespace Reflexor.Tests;

public class VerifySimple
{
    [Fact]
    public Task Test1()
    {
        return TestHelper.VerifySourceCode("""
            using Reflexor;

            namespace Test;

            [GenerateProxy]
            public class User
            {
                public string UserName { get; init; }
                public string Email { get; init; }
            }
            """);
    }

    [Fact]
    public Task Test2()
    {
        return TestHelper.VerifySourceCode("""
            using Reflexor;

            namespace Test;

            [GenerateProxy]
            public record User(string UserName, string Email);
            """);
    }
}
