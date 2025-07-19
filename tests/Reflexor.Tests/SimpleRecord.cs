using Reflexor.Tests.Helpers;

namespace Reflexor.Tests;

public sealed class SimpleRecord
{
    [Fact]
    public Task Verify() => TestHelper.Verify("""
        using Reflexor;

        namespace Test;

        [GenerateProxy]
        public record SimpleRecord(string Name, int Age);
        """);
}
