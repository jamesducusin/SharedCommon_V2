namespace SharedCommon.Utilities.UnitTests;

public sealed class CollectionExtensionsTests
{
    [Fact]
    public void Batch_SplitsIntoCorrectSizedBatches()
    {
        var result = Enumerable.Range(1, 7).Batch(3).ToList();
        Assert.Equal(3, result.Count);
        Assert.Equal([1, 2, 3], result[0]);
        Assert.Equal([4, 5, 6], result[1]);
        Assert.Equal([7], result[2]);
    }

    [Fact]
    public void Batch_ExactMultiple_NoBatchRemainder()
    {
        var result = Enumerable.Range(1, 6).Batch(3).ToList();
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Batch_EmptySource_ReturnsNoBatches() =>
        Assert.Empty(Array.Empty<int>().Batch(3));

    [Fact]
    public void Batch_SizeZero_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new[] { 1 }.Batch(0).ToList());

    [Fact]
    public void SafeAny_NonEmptyCollection_ReturnsTrue() =>
        Assert.True(new[] { 1, 2 }.SafeAny());

    [Fact]
    public void SafeAny_NullCollection_ReturnsFalse() =>
        Assert.False(((IEnumerable<int>?)null).SafeAny());

    [Fact]
    public void SafeAny_EmptyCollection_ReturnsFalse() =>
        Assert.False(Array.Empty<int>().SafeAny());

    [Fact]
    public void SafeAny_WithPredicate_FiltersCorrectly() =>
        Assert.True(new[] { 1, 2, 3 }.SafeAny(x => x > 2));

    [Fact]
    public void WhereNotNull_FiltersNulls()
    {
        var result = new string?[] { "a", null, "b", null }.WhereNotNull().ToList();
        Assert.Equal(["a", "b"], result);
    }

    [Fact]
    public void ForEach_ExecutesActionOnEachElement()
    {
        var sum = 0;
        new[] { 1, 2, 3 }.ForEach(x => sum += x);
        Assert.Equal(6, sum);
    }

    [Fact]
    public void EmptyIfNull_NullSource_ReturnsEmpty() =>
        Assert.Empty(((IEnumerable<int>?)null).EmptyIfNull());

    [Fact]
    public void EmptyIfNull_NonNullSource_ReturnsSource() =>
        Assert.Equal([1, 2], new[] { 1, 2 }.EmptyIfNull());

    [Fact]
    public void Yield_SingleValue_ProducesOneElementSequence() =>
        Assert.Equal([42], 42.Yield());

    [Fact]
    public void IsNullOrEmpty_NullCollection_ReturnsTrue() =>
        Assert.True(((IEnumerable<int>?)null).IsNullOrEmpty());

    [Fact]
    public void IsNullOrEmpty_EmptyCollection_ReturnsTrue() =>
        Assert.True(Array.Empty<int>().IsNullOrEmpty());

    [Fact]
    public void IsNullOrEmpty_NonEmptyCollection_ReturnsFalse() =>
        Assert.False(new[] { 1 }.IsNullOrEmpty());
}
