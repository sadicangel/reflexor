using Reflexor.Tests.Helpers;

namespace Reflexor.Tests;

public sealed class ExternMethod
{
    [Fact]
    public Task Verify() => TestHelper.VerifySourceCode("""
        using Reflexor;
        using System.Runtime.InteropServices;

        namespace Test;

        [GenerateProxy]
        public partial class ExternMethod
        {
            [LibraryImport("NativeLibrary")]
            public extern void NativeCall();
        }
        """);
}
