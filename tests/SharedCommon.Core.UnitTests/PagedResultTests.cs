namespace SharedCommon.Core.UnitTests;

public sealed class PagedResultTests
{
    // Pagination
    [Fact]
    public void Pagination_Default_IsPageOneSize20()
    {
        var p = Pagination.Default;
        Assert.Equal(1, p.Page);
        Assert.Equal(20, p.PageSize);
    }

    [Fact]
    public void Pagination_Offset_ComputedCorrectly()
    {
        var p = new Pagination(3, 10);
        Assert.Equal(20, p.Offset);
    }

    [Fact]
    public void Pagination_Of_ClampsPageBelowOne() =>
        Assert.Equal(1, Pagination.Of(0, 10).Page);

    [Fact]
    public void Pagination_Of_ClampsPageSizeAboveMax() =>
        Assert.Equal(Pagination.MaxPageSize, Pagination.Of(1, 9999).PageSize);

    [Fact]
    public void Pagination_Of_ClampsPageSizeBelowOne() =>
        Assert.Equal(1, Pagination.Of(1, 0).PageSize);

    // PagedResult
    [Fact]
    public void PagedResult_TotalPages_ComputedCorrectly()
    {
        var result = new PagedResult<int>([1, 2, 3], 25, new Pagination(1, 10));
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public void PagedResult_HasNextPage_WhenNotLastPage()
    {
        var result = new PagedResult<int>([1], 25, new Pagination(1, 10));
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public void PagedResult_HasNextPage_False_OnLastPage()
    {
        var result = new PagedResult<int>([1], 5, new Pagination(1, 10));
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public void PagedResult_HasPreviousPage_WhenNotFirstPage()
    {
        var result = new PagedResult<int>([1], 25, new Pagination(2, 10));
        Assert.True(result.HasPreviousPage);
    }

    [Fact]
    public void PagedResult_HasPreviousPage_False_OnFirstPage()
    {
        var result = new PagedResult<int>([1], 25, new Pagination(1, 10));
        Assert.False(result.HasPreviousPage);
    }

    [Fact]
    public void PagedResult_Empty_HasZeroItems()
    {
        var result = PagedResult<int>.Empty(Pagination.Default);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public void PagedResult_From_SlicesCorrectly()
    {
        var source = Enumerable.Range(1, 15).ToList();
        var result = PagedResult<int>.From(source, new Pagination(2, 5));
        Assert.Equal([6, 7, 8, 9, 10], result.Items);
        Assert.Equal(15, result.TotalCount);
    }

    [Fact]
    public void PagedResult_From_LastPageMayBePartial()
    {
        var source = Enumerable.Range(1, 7).ToList();
        var result = PagedResult<int>.From(source, new Pagination(2, 5));
        Assert.Equal([6, 7], result.Items);
    }
}
