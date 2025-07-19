using Reflexor.Tests.Helpers;

namespace Reflexor.Tests;

public sealed class InitOnlyClass
{
    [Fact]
    public Task Verify() => TestHelper.VerifySourceCode("""
        using Reflexor;

        namespace Test;

        [GenerateProxy]
        public class InitOnlyClass
        {
            public string Name { get; init; }
            public int Age { get; init; }
        }
        """);
}
