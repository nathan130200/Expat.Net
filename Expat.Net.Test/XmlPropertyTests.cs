global using static Expat.NativeMethods;

namespace Expat.Test;

public class XmlPropertyTests
{
    [Theory]
    [InlineData(1 << 10, 10d)]
    [InlineData(1 << 11, 25d)]
    [InlineData(1 << 12, 50d)]
    [InlineData(1 << 13, 75d)]
    [InlineData(1 << 14, 100d)]
    public void TrySetBillionLaughsProperties(ulong threshold, double factor)
    {
        using var parser = new NativeXmlParser("UTF-8");

        Assert.NotEqual(0, (nint)parser);

        Assert.Equal(XmlPropertyResult.None,
            XML_SetPropertyUInt64(parser, XmlProperty.BillionLaughsActivationThreshold, threshold));

        Assert.Equal(XmlPropertyResult.None,
            XML_SetPropertyDouble(parser, XmlProperty.BillionLaughsMaximumAmplification, factor));
    }

    [Theory]
    [InlineData(1 << 10, 10d)]
    [InlineData(1 << 11, 25d)]
    [InlineData(1 << 12, 50d)]
    [InlineData(1 << 13, 75d)]
    [InlineData(1 << 14, 100d)]
    public void TrySetAndGetBillionLaughsProperties(ulong threshold, double factor)
    {
        using var parser = new NativeXmlParser("UTF-8");

        Assert.NotEqual(0, (nint)parser);

        Assert.Equal(XmlPropertyResult.None,
            XML_SetPropertyUInt64(parser, XmlProperty.BillionLaughsActivationThreshold, threshold));

        Assert.Equal(XmlPropertyResult.None,
            XML_SetPropertyDouble(parser, XmlProperty.BillionLaughsMaximumAmplification, factor));

        Assert.Equal(XmlPropertyResult.None,
           XML_GetPropertyUInt64(parser, XmlProperty.BillionLaughsActivationThreshold, out var currentThreshold));

        Assert.Equal(XmlPropertyResult.None,
            XML_GetPropertyDouble(parser, XmlProperty.BillionLaughsMaximumAmplification, out var currentFactor));

        Assert.Equal(threshold, currentThreshold);

        Assert.Equal(factor, currentFactor);
    }

    [Fact]
    public void TryGetDefaultBillionLaughsProperties()
    {
        using var parser = new NativeXmlParser("UTF-8");

        Assert.NotEqual(0, (nint)parser);

        Assert.Equal(XmlPropertyResult.None,
            XML_GetPropertyUInt64(parser, XmlProperty.BillionLaughsActivationThreshold, out var currentThreshold));

        Assert.Equal(XmlPropertyResult.None,
            XML_GetPropertyDouble(parser, XmlProperty.BillionLaughsMaximumAmplification, out var currentFactor));

        Assert.Equal(8388608UL, currentThreshold);

        Assert.Equal(100d, currentFactor);
    }

    [Fact]
    public void TrySetInvalidPropertyType()
    {
        using var parser = new NativeXmlParser("UTF-8");

        var code = XML_SetPropertyDouble(parser, XmlProperty.ReparseDeferralEnabled, 32d);

        Assert.Equal(XmlPropertyResult.InvalidType, code);
    }

    [Fact]
    public void TrySetUnknownProperty()
    {
        using var parser = new NativeXmlParser("UTF-8");

        var code = XML_SetPropertyDouble(parser, (XmlProperty)~0, 32d);

        Assert.Equal(XmlPropertyResult.InvalidKey, code);
    }

    // we cannot test for "invalid value" here because p/invoke will always marshall to correct managed type.
}
