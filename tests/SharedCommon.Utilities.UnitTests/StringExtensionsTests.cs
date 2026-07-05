namespace SharedCommon.Utilities.UnitTests;

public sealed class StringExtensionsTests
{
    [Theory]
    [InlineData("Hello World", "hello-world")]
    [InlineData("  Leading spaces  ", "leading-spaces")]
    [InlineData("Café au lait", "cafe-au-lait")]
    [InlineData("multiple   spaces", "multiple-spaces")]
    [InlineData("special!@#chars", "specialchars")]
    [InlineData("UPPERCASE", "uppercase")]
    public void ToSlug_ConvertsToUrlSafeSlug(string input, string expected) =>
        Assert.Equal(expected, input.ToSlug());

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ToSlug_EmptyOrWhitespace_ReturnsEmpty(string input) =>
        Assert.Equal(string.Empty, input.ToSlug());

    [Theory]
    [InlineData("Hello World", 5, "He...")]
    [InlineData("Hello", 10, "Hello")]
    [InlineData("Hello World", 8, "Hello...")]
    public void Truncate_TruncatesCorrectly(string input, int maxLength, string expected) =>
        Assert.Equal(expected, input.Truncate(maxLength));

    [Fact]
    public void Truncate_Empty_ReturnsEmpty() =>
        Assert.Equal(string.Empty, string.Empty.Truncate(5));

    [Theory]
    [InlineData("api-key-secret-value", 4, "****************alue")]
    [InlineData("abcd", 4, "****")]
    [InlineData("ab", 4, "**")]
    [InlineData("", 4, "")]
    public void Mask_MasksAllButVisibleChars(string input, int visible, string expected) =>
        Assert.Equal(expected, input.Mask(visible));

    [Fact]
    public void IsNullOrEmpty_NullString_ReturnsTrue() =>
        Assert.True(((string?)null).IsNullOrEmpty());

    [Fact]
    public void IsNullOrEmpty_EmptyString_ReturnsTrue() =>
        Assert.True(string.Empty.IsNullOrEmpty());

    [Fact]
    public void IsNullOrEmpty_NonEmpty_ReturnsFalse() =>
        Assert.False("hello".IsNullOrEmpty());

    [Fact]
    public void IsNullOrWhiteSpace_Whitespace_ReturnsTrue() =>
        Assert.True("   ".IsNullOrWhiteSpace());

    [Fact]
    public void IsNullOrWhiteSpace_NonEmpty_ReturnsFalse() =>
        Assert.False("x".IsNullOrWhiteSpace());

    [Fact]
    public void ToTitleCase_LowercaseInput_CapitalizesFirstLetters() =>
        Assert.Equal("Hello World", "hello world".ToTitleCase());
}
