using Reflexor.Tests.Helpers;

namespace Reflexor.Tests;

public class VerifyEdgeCases
{
    [Fact]
    public Task Nullable_members()
    {
        return TestHelper.VerifySourceCode("""
            using Reflexor;

            namespace Test;

            [Reflexor]
            public class NullableUser
            {
                public string? Name { get; init; }
                internal string? Echo(string? value) => value;
            }
            """);
    }

    [Fact]
    public Task Static_class()
    {
        return TestHelper.VerifySourceCode("""
            using Reflexor;

            namespace Test;

            [Reflexor]
            public static class StaticConfig
            {
                public static int Version { get; private set; }
                internal static int SetVersion(int version) => Version = version;
            }
            """);
    }

    [Fact]
    public Task Static_members_on_class()
    {
        return TestHelper.VerifySourceCode("""
            using Reflexor;

            namespace Test;

            [Reflexor]
            public class StaticMemberContainer
            {
                public static int Version { get; private set; }
                internal static int SetVersion(int version) => Version = version;
            }
            """);
    }

    [Fact]
    public Task Generic_and_ref_members()
    {
        return TestHelper.VerifySourceCode("""
            using Reflexor;
            using System.Numerics;

            namespace Test;

            [Reflexor]
            public class GenericRefMembers<T>
                where T : class
            {
                public T? Value { get; init; }
                internal void RefInOut(ref int x, in int y, out int z) => z = x + y;
                internal TItem? Echo<TItem>(TItem? item) where TItem : class? => item;
                internal TNumber Add<TNumber>(TNumber left, TNumber right) where TNumber : INumber<TNumber> => left + right;
            }
            """);
    }

    [Fact]
    public Task Access_modifiers_and_unsafe_members()
    {
        return TestHelper.VerifySourceCode("""
            using Reflexor;

            namespace Test;

            [Reflexor]
            public unsafe class AccessModifiers
            {
                public int PublicProp { get; private set; }
                protected int ProtectedProp { get; set; }
                internal int InternalProp { get; set; }
                private int PrivateProp { get; set; }
                private int* Pointer { get; set; }
            }
            """);
    }

    [Fact]
    public Task Nested_type()
    {
        return TestHelper.VerifySourceCode("""
            using Reflexor;

            namespace Test;

            public class Outer
            {
                [Reflexor]
                public class Inner
                {
                    public string Name { get; init; } = "";
                }
            }
            """);
    }
}
