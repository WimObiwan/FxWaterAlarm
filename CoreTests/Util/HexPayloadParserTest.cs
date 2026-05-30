using Core.Util;

namespace CoreTests.Util;

public class HexPayloadParserTest
{
    [Fact]
    public void Parse_ValidHex_ReturnsExpectedBytes()
    {
        var result = HexPayloadParser.Parse("0100012C");

        Assert.Equal([0x01, 0x00, 0x01, 0x2C], result);
    }

    [Fact]
    public void Parse_ValidHexWithSeparators_ReturnsExpectedBytes()
    {
        var result = HexPayloadParser.Parse("01 00-01:2C");

        Assert.Equal([0x01, 0x00, 0x01, 0x2C], result);
    }

    [Fact]
    public void Parse_InvalidLength_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => HexPayloadParser.Parse("ABC"));

        Assert.Equal("payloadHex", exception.ParamName);
    }

    [Fact]
    public void Parse_InvalidCharacters_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => HexPayloadParser.Parse("01ZZ"));

        Assert.Equal("payloadHex", exception.ParamName);
    }
}
