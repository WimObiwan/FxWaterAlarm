using Core.Util;
using Xunit;

namespace CoreTests.Util;

public class OptionalTest
{
    [Fact]
    public void Record_WithSpecifiedAndValue_SetsProperties()
    {
        var opt = new Optional<int>(true, 42);

        Assert.True(opt.Specified);
        Assert.Equal(42, opt.Value);
    }

    [Fact]
    public void Record_WithNotSpecifiedAndDefault_SetsProperties()
    {
        var opt = new Optional<int>(false, default);

        Assert.False(opt.Specified);
        Assert.Equal(0, opt.Value);
    }

    [Fact]
    public void FromStruct_WithValue_ReturnsSpecifiedOptional()
    {
        int? value = 42;

        var result = Optional.From(value);

        Assert.True(result.Specified);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void FromStruct_WithNull_ReturnsNotSpecified()
    {
        int? value = null;

        var result = Optional.From(value);

        Assert.False(result.Specified);
        Assert.Equal(default, result.Value);
    }

    [Fact]
    public void FromWithNullValue_WithActualValue_ReturnsSpecified()
    {
        var result = Optional.From<int>(5, -1);

        Assert.True(result.Specified);
        Assert.Equal(5, result.Value);
    }

    [Fact]
    public void FromWithNullValue_WithNullInput_ReturnsNotSpecified()
    {
        var result = Optional.From<string>(null, "");

        Assert.False(result.Specified);
        Assert.Equal(default, result.Value);
    }

    [Fact]
    public void FromWithNullValue_WithNullValueSentinel_ReturnsSpecifiedWithDefault()
    {
        // When value equals the nullValue sentinel, it means "set to default/clear"
        var result = Optional.From<int>(0, 0);

        Assert.True(result.Specified);
        Assert.Equal(default, result.Value);
    }

    [Fact]
    public void FromString_WithNonEmptyString_ReturnsSpecified()
    {
        var result = Optional.From("hello");

        Assert.True(result.Specified);
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void FromString_WithNull_ReturnsNotSpecified()
    {
        var result = Optional.From((string?)null);

        Assert.False(result.Specified);
        Assert.Equal(default, result.Value);
    }

    [Fact]
    public void FromString_WithEmptyString_ReturnsSpecifiedWithDefault()
    {
        // Empty string is the sentinel for "clear the value"
        var result = Optional.From("");

        Assert.True(result.Specified);
        Assert.Equal(default, result.Value);
    }

    [Fact]
    public void Record_Equality_WorksCorrectly()
    {
        var opt1 = new Optional<int>(true, 42);
        var opt2 = new Optional<int>(true, 42);
        var opt3 = new Optional<int>(true, 99);

        Assert.Equal(opt1, opt2);
        Assert.NotEqual(opt1, opt3);
    }
}
