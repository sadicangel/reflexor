using Reflexor.Tests.Helpers;

namespace Reflexor.Tests;

public sealed class AccessModifiers
{
    [Fact]
    public Task Verify() => TestHelper.Verify("""
        using Reflexor;

        namespace Test;

        [GenerateProxy]
        public class AccessModifiers
        {
            public int PublicProp { get; set; }
            protected int ProtectedProp { get; set; }
            internal int InternalProp { get; set; }
            private int PrivateProp { get; set; }
        }
        """);
}
