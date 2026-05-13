namespace Reflexor.IntegrationTests;

public class InheritanceAndMutateTests
{
    [Fact]
    public void Mutates_inherited_members()
    {
        var derived = new DerivedMutable { Name = "before" };
        var proxy = new DerivedMutableProxy(derived);

        proxy.Hidden = 42;
        proxy.Count = 7;
        proxy.Name = "after";

        Assert.Equal(42, proxy.GetHidden());
        Assert.Equal(7, derived.Count);
        Assert.Equal("after", derived.Name);
    }

    [Fact]
    public void Mutate_extension_mutates_in_place_and_returns_original()
    {
        var derived = new DerivedMutable { Name = "before" };

        var returned = derived.Mutate(proxy =>
        {
            proxy.Hidden = 100;
            proxy.Count = 10;
            proxy.Name = "after";
        });

        Assert.Same(derived, returned);
        Assert.Equal(100, new DerivedMutableProxy(derived).GetHidden());
        Assert.Equal(10, derived.Count);
        Assert.Equal("after", derived.Name);
    }

    [Fact]
    public void Mutate_extension_rejects_null_mutation()
    {
        var derived = new DerivedMutable();

        Assert.Throws<ArgumentNullException>(() => derived.Mutate(null!));
    }

    [Fact]
    public void Prefers_overridden_members()
    {
        var derived = new OverrideDerivedMutable();
        var proxy = new OverrideDerivedMutableProxy(derived);

        proxy.Value = 11;

        Assert.Equal(11, derived.Value);
        Assert.Equal(12, proxy.Echo(11));
    }
}

public class BaseMutable
{
    private int Hidden { get; set; }
    public int Count { get; init; }

    private int GetHidden() => Hidden;
}

[Reflexor]
public class DerivedMutable : BaseMutable
{
    public string Name { get; init; } = "";
}

public class OverrideBaseMutable
{
    public virtual int Value { get; init; }

    internal virtual int Echo(int value) => value;
}

[Reflexor]
public class OverrideDerivedMutable : OverrideBaseMutable
{
    public override int Value { get; init; }

    internal override int Echo(int value) => value + 1;
}
