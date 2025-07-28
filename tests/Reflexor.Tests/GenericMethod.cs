using Reflexor.Tests.Helpers;

namespace Reflexor.Tests;

public sealed class GenericMethod
{
    [Fact]
    public Task Verify() => TestHelper.Verify("""
        using Reflexor;
        using System.Numerics;
        
        namespace Test;

        [GenerateProxy]
        public class GenericMethod
        {
            internal void ClassConstraint<TClass>(TClass item) where TClass : class => _ = item;
            internal void NullableClassConstraint<TClass>(TClass item) where TClass : class? => _ = item;
            internal void StructConstraint<TStruct>(TStruct item) where TStruct : struct => _ = item;
            internal void UnmanagedConstraint<TUnmanaged>(TUnmanaged item) where TUnmanaged : unmanaged => _ = item;
            internal void NotNullConstraint<TNotNull>(TNotNull item) where TNotNull : notnull => _ = item;
            internal void BaseConstraint<TBase>(TBase item) where TBase : INumber<TBase> => _ = item;
            internal void NullableBaseConstraint<TBase>(TBase item) where TBase : INumber<TBase>? => _ = item;
            internal void NewConstraint<TNew>(TNew item) where TNew : new() => _ = item;
            //internal void DefaultConstraint<TDefault>(TDefault item) where TDefault : default => _ = item;
            //internal void AllowsRefLikeConstraint<TRefLike>(TRefLike item) where TRefLike : allows ref struct => _ = item;
        }
        """);
}
