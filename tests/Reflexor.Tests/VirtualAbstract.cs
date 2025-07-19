using Reflexor.Tests.Helpers;

namespace Reflexor.Tests;

public sealed class VirtualAbstract
{
    [Fact]
    public Task Verify() => TestHelper.Verify("""
        using Reflexor;

        namespace Test;

        [GenerateProxy]
        public abstract class VirtualAbstractBase
        {
            protected abstract string AbstractProp { get; set; }
            protected virtual int VirtualProp { get; set; }
        }
        
        [GenerateProxy]
        public class VirtualAbstractDerived : VirtualAbstractBase
        {
            protected override string AbstractProp { get; set; }
            protected override int VirtualProp { get; set; }
        }
        """);
}
