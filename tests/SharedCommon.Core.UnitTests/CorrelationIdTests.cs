using SharedCommon.Core;

namespace SharedCommon.Core.UnitTests;

public class CorrelationIdTests
{
    [Fact]
    public void New_ReturnsNonEmptyValue()
    {
        var id = CorrelationId.New();
        Assert.False(string.IsNullOrWhiteSpace(id.Value));
    }

    [Fact]
    public void New_ReturnsDifferentValuesEachCall()
    {
        var a = CorrelationId.New();
        var b = CorrelationId.New();
        Assert.NotEqual(a.Value, b.Value);
    }

    [Fact]
    public void New_ValueIsValidGuid()
    {
        var id = CorrelationId.New();
        Assert.True(Guid.TryParse(id.Value, out _));
    }

    [Fact]
    public void From_ValidString_ReturnsId()
    {
        var id = CorrelationId.From("abc-123");
        Assert.Equal("abc-123", id.Value);
    }

    [Fact]
    public void From_NullString_Throws()
    {
        Assert.Throws<ArgumentException>(() => CorrelationId.From(null!));
    }

    [Fact]
    public void From_EmptyString_Throws()
    {
        Assert.Throws<ArgumentException>(() => CorrelationId.From(""));
    }

    [Fact]
    public void From_WhitespaceString_Throws()
    {
        Assert.Throws<ArgumentException>(() => CorrelationId.From("   "));
    }

    [Fact]
    public void TryCreate_ValidString_ReturnsTrueAndId()
    {
        var result = CorrelationId.TryCreate("test-id", out var id);
        Assert.True(result);
        Assert.NotNull(id);
        Assert.Equal("test-id", id!.Value);
    }

    [Fact]
    public void TryCreate_NullString_ReturnsFalseAndNullId()
    {
        var result = CorrelationId.TryCreate(null, out var id);
        Assert.False(result);
        Assert.Null(id);
    }

    [Fact]
    public void TryCreate_EmptyString_ReturnsFalse()
    {
        var result = CorrelationId.TryCreate("", out var id);
        Assert.False(result);
        Assert.Null(id);
    }

    [Fact]
    public void TryCreate_WhitespaceString_ReturnsFalse()
    {
        var result = CorrelationId.TryCreate("  ", out var id);
        Assert.False(result);
        Assert.Null(id);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var id = CorrelationId.From("my-id");
        Assert.Equal("my-id", id.ToString());
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        var id = CorrelationId.From("my-id");
        string s = id;
        Assert.Equal("my-id", s);
    }

    [Fact]
    public void RecordEquality_SameValue_AreEqual()
    {
        var a = CorrelationId.From("same");
        var b = CorrelationId.From("same");
        Assert.Equal(a, b);
    }

    [Fact]
    public void RecordEquality_DifferentValues_AreNotEqual()
    {
        var a = CorrelationId.From("aaa");
        var b = CorrelationId.From("bbb");
        Assert.NotEqual(a, b);
    }
}
