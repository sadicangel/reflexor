using Reflexor.Tests.Helpers;

namespace Reflexor.Tests;

public sealed class UnsafeClass
{
    [Fact]
    public Task Verify() => TestHelper.Verify("""
        using Reflexor;

        namespace Test;

        [GenerateProxy]
        public unsafe class UnsafeClass
        {
            private int* Pointer { get; set; }
            private NonPublicStruct* StructPointer { get; set; }

            private struct NonPublicStruct;
        }
        """);
}
