using Reflexor.Tests.Helpers;

namespace Reflexor.Tests;

public sealed class RefInOutMethods
{
    [Fact]
    public Task Verify() => TestHelper.Verify("""
        using Reflexor;

        namespace Test;

        [GenerateProxy]
        public class RefInOutMethods
        {
            public void DoWork(ref int x, in int y, out int z)
            {
                z = x + y;
                x++;
            }
        }
        """);
}
