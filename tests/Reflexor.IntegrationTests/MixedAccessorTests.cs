namespace Reflexor.IntegrationTests;

public class MixedAccessorTests
{
    [Fact]
    public void Mutates_mixed_accessor_properties()
    {
        var target = new MixedAccessors(1, 2, 3, 4);
        var proxy = new MixedAccessorsProxy(target);

        Assert.Equal(1, proxy.PrivateGet);
        proxy.PrivateGet = 10;
        Assert.Equal(10, target.ReadPrivateGet());

        Assert.Equal(2, proxy.PrivateSet);
        proxy.PrivateSet = 20;
        Assert.Equal(20, target.PrivateSet);

        Assert.Equal(3, proxy.PublicSet);
        proxy.PublicSet = 30;
        Assert.Equal(30, target.PublicSet);

        Assert.Equal(4, proxy.InitOnly);
        proxy.InitOnly = 40;
        Assert.Equal(40, target.InitOnly);
    }
}

[Reflexor]
public class MixedAccessors
{
    public MixedAccessors(int privateGet, int privateSet, int publicSet, int initOnly)
    {
        PrivateGet = privateGet;
        PrivateSet = privateSet;
        PublicSet = publicSet;
        InitOnly = initOnly;
    }

    public int PrivateGet { private get; set; }

    public int PrivateSet { get; private set; }

    public int PublicSet { get; set; }

    public int InitOnly { get; init; }

    public int ReadPrivateGet() => PrivateGet;
}
