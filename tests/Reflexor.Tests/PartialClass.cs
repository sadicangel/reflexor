using Reflexor.Tests.Helpers;

namespace Reflexor.Tests;

public sealed class PartialClass
{
    [Fact]
    public Task Verify() => TestHelper.VerifySourceCode("""
        using Reflexor;

        namespace Test;

        [GenerateProxy]
        public partial class PartialClass
        {
            public int Part1 { get; set; }
        }

        public partial class PartialClass
        {
            public string Part2 { get; set; }
        }
        """);
}
