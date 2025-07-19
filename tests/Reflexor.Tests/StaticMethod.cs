using Reflexor.Tests.Helpers;

namespace Reflexor.Tests;

public sealed class StaticMethod
{
    [Fact]
    public Task Verify() => TestHelper.VerifySourceCode("""
        using Reflexor;

        namespace Test;

        [GenerateProxy]
        public class StaticMethod
        {
            public static int Version { get; private set; } = 42;
        }
        """);
}
