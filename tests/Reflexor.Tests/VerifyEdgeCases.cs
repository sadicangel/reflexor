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
    public Task Safe_parameter_names()
    {
        return TestHelper.VerifySourceCode("""
            using Reflexor;

            namespace @namespace;

            public class Event;

            [Reflexor]
            public class @class<@event>
                where @event : class
            {
                public int @params { get; private set; }
                public int _target { get; private set; }
                internal @event @return<@while>(@event @event, int target, int _target, int Callreturn)
                    where @while : class
                    => @event;
                internal void @while(Event @event) { }
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

    [Fact]
    public Task Inherited_members()
    {
        return TestHelper.VerifySourceCode("""
            using Reflexor;

            namespace Test;

            public class BaseMembers
            {
                private int PrivateBase { get; set; }
                protected int ProtectedBase { get; set; }
                internal int InternalBase { get; set; }
                public int PublicBase { get; set; }
                public int Shadowed { get; private set; }
                internal int Echo(int value) => value;
                internal int Overload(int value) => value;
                internal int Hidden(int value) => value;
            }

            [Reflexor]
            public class DerivedMembers : BaseMembers
            {
                public string Own { get; init; } = "";
                public new int Shadowed { get; private set; }
                internal string Overload(string value) => value;
                internal new int Hidden(int value) => value + 1;
            }
            """);
    }

    [Fact]
    public Task Overridden_members()
    {
        return TestHelper.VerifySourceCode("""
            using Reflexor;

            namespace Test;

            public class OverrideBase
            {
                public virtual int Value { get; init; }
                internal virtual int Echo(int value) => value;
            }

            [Reflexor]
            public class OverrideDerived : OverrideBase
            {
                public override int Value { get; init; }
                internal override int Echo(int value) => value + 1;
            }
            """);
    }
}
