using Reflexor.Tests.Helpers;

namespace Reflexor.Tests;

public sealed class StaticProperty
{
    [Fact]
    public Task Verify() => TestHelper.Verify("""
        using Reflexor;

        namespace Test;

        [GenerateProxy]
        public class StaticProperty
        {
            public static int Version { get; private set; } = 42;
        }
        """);
}
