using Reflexor.Tests.Helpers;

namespace Reflexor.Tests;

public sealed class GenericClass
{
    [Fact]
    public Task Verify() => TestHelper.Verify("""
        using Reflexor;

        namespace Test;

        [GenerateProxy]
        public class GenericClass<T>
        {
            private T Value { get; set; }
            private T GetValue() => Value;
        }
        """);
}
