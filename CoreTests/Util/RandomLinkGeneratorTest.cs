using Core.Util;
using Xunit;

namespace CoreTests.Util;

public class RandomLinkGeneratorTest
{
    [Fact]
    public void Get_ReturnsAlphanumericString()
    {
        var result = RandomLinkGenerator.Get();

        Assert.Matches("^[A-Za-z0-9]+$", result);
    }

    [Fact]
    public void Get_ReturnsNonEmptyString()
    {
        var result = RandomLinkGenerator.Get();

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void Get_ReturnsReasonableLength()
    {
        // 8 random bytes → base64 is 12 chars, removing non-alphanumeric gives 8-12 chars
        var result = RandomLinkGenerator.Get();

        Assert.InRange(result.Length, 5, 15);
    }

    [Fact]
    public void Get_ReturnsDifferentValuesOnSubsequentCalls()
    {
        var results = Enumerable.Range(0, 10).Select(_ => RandomLinkGenerator.Get()).ToList();

        // At least 2 distinct values should be generated (deterministic, non-flaky assertion)
        Assert.True(results.Distinct().Count() >= 2, "Expected at least 2 distinct values from multiple calls");
    }
}
