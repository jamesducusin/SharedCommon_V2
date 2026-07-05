namespace SharedCommon.Core.UnitTests;

public sealed class GuardTests
{
    // AgainstNull (reference type)
    [Fact]
    public void AgainstNull_NonNull_ReturnsValue()
    {
        var value = "hello";
        Assert.Equal(value, Guard.AgainstNull(value));
    }

    [Fact]
    public void AgainstNull_Null_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => Guard.AgainstNull<string>(null));

    // AgainstNull (value type nullable)
    [Fact]
    public void AgainstNull_NullableValueType_NonNull_ReturnsValue()
    {
        int? value = 42;
        Assert.Equal(42, Guard.AgainstNull(value));
    }

    [Fact]
    public void AgainstNull_NullableValueType_Null_Throws() =>
        Assert.Throws<ArgumentNullException>(() => Guard.AgainstNull((int?)null));

    // AgainstNullOrEmpty
    [Fact]
    public void AgainstNullOrEmpty_ValidString_ReturnsString() =>
        Assert.Equal("hello", Guard.AgainstNullOrEmpty("hello"));

    [Fact]
    public void AgainstNullOrEmpty_Null_Throws() =>
        Assert.Throws<ArgumentException>(() => Guard.AgainstNullOrEmpty(null));

    [Fact]
    public void AgainstNullOrEmpty_Empty_Throws() =>
        Assert.Throws<ArgumentException>(() => Guard.AgainstNullOrEmpty(string.Empty));

    // AgainstNullOrWhiteSpace
    [Fact]
    public void AgainstNullOrWhiteSpace_Whitespace_Throws() =>
        Assert.Throws<ArgumentException>(() => Guard.AgainstNullOrWhiteSpace("   "));

    [Fact]
    public void AgainstNullOrWhiteSpace_ValidString_ReturnsString() =>
        Assert.Equal("ok", Guard.AgainstNullOrWhiteSpace("ok"));

    // AgainstEmpty (collection)
    [Fact]
    public void AgainstEmpty_NonEmptyCollection_Returns() =>
        Assert.Equal([1, 2], Guard.AgainstEmpty(new[] { 1, 2 }));

    [Fact]
    public void AgainstEmpty_EmptyCollection_Throws() =>
        Assert.Throws<ArgumentException>(() => Guard.AgainstEmpty(Array.Empty<int>()));

    [Fact]
    public void AgainstEmpty_NullCollection_Throws() =>
        Assert.Throws<ArgumentException>(() => Guard.AgainstEmpty((IEnumerable<int>?)null));

    // AgainstLessThan
    [Fact]
    public void AgainstLessThan_ValueAboveMin_Returns() =>
        Assert.Equal(5, Guard.AgainstLessThan(5, 1));

    [Fact]
    public void AgainstLessThan_ValueBelowMin_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Guard.AgainstLessThan(0, 1));

    // AgainstGreaterThan
    [Fact]
    public void AgainstGreaterThan_ValueBelowMax_Returns() =>
        Assert.Equal(5, Guard.AgainstGreaterThan(5, 10));

    [Fact]
    public void AgainstGreaterThan_ValueAboveMax_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Guard.AgainstGreaterThan(11, 10));

    // AgainstOutOfRange
    [Fact]
    public void AgainstOutOfRange_InRange_Returns() =>
        Assert.Equal(5, Guard.AgainstOutOfRange(5, 1, 10));

    [Fact]
    public void AgainstOutOfRange_BelowMin_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Guard.AgainstOutOfRange(0, 1, 10));

    [Fact]
    public void AgainstOutOfRange_AboveMax_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Guard.AgainstOutOfRange(11, 1, 10));

    // AgainstEmptyGuid
    [Fact]
    public void AgainstEmptyGuid_ValidGuid_Returns()
    {
        var id = Guid.NewGuid();
        Assert.Equal(id, Guard.AgainstEmptyGuid(id));
    }

    [Fact]
    public void AgainstEmptyGuid_EmptyGuid_Throws() =>
        Assert.Throws<ArgumentException>(() => Guard.AgainstEmptyGuid(Guid.Empty));

    // AgainstInvalidState
    [Fact]
    public void AgainstInvalidState_FalseCondition_DoesNotThrow() =>
        Guard.AgainstInvalidState(false, "should not throw");

    [Fact]
    public void AgainstInvalidState_TrueCondition_Throws() =>
        Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidState(true, "invalid state"));

    // AgainstExceedingLength
    [Fact]
    public void AgainstExceedingLength_WithinLimit_Returns() =>
        Assert.Equal("hello", Guard.AgainstExceedingLength("hello", 10));

    [Fact]
    public void AgainstExceedingLength_ExceedsLimit_Throws() =>
        Assert.Throws<ArgumentException>(() => Guard.AgainstExceedingLength("hello world", 5));
}
