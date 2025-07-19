using Reflexor.Tests.Helpers;

namespace Reflexor.Tests;

public sealed class StaticClass
{
    [Fact]
    public Task Verify() => TestHelper.Verify("""
        using Reflexor;

        namespace Test;

        [GenerateProxy]
        public static class StaticClass
        {
            public static int Version { get; private set; } = 42;
        }
        """);
}
