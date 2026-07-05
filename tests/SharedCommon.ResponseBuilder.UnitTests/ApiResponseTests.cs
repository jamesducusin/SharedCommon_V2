namespace SharedCommon.ResponseBuilder.UnitTests;

public sealed class ApiResponseTests
{
    [Fact]
    public void Ok_SetsSuccessTrue()
    {
        var response = ApiResponse<string>.Ok("hello");
        Assert.True(response.Success);
    }

    [Fact]
    public void Ok_SetsData()
    {
        var response = ApiResponse<string>.Ok("hello");
        Assert.Equal("hello", response.Data);
    }

    [Fact]
    public void Ok_SetsCorrelationId()
    {
        var response = ApiResponse<string>.Ok("hello", "corr-123");
        Assert.Equal("corr-123", response.CorrelationId);
    }

    [Fact]
    public void Ok_NoPagination_IsNull()
    {
        var response = ApiResponse<string>.Ok("hello");
        Assert.Null(response.Pagination);
    }

    [Fact]
    public void Paged_SetsPaginationInfo()
    {
        var pagination = new PaginationInfo(2, 10, 50);
        var response = ApiResponse<string[]>.Paged(["a", "b"], pagination);
        Assert.Equal(pagination, response.Pagination);
    }

    // PaginationInfo
    [Fact]
    public void PaginationInfo_TotalPages_ComputedCorrectly() =>
        Assert.Equal(5, new PaginationInfo(1, 10, 50).TotalPages);

    [Fact]
    public void PaginationInfo_HasNextPage_WhenNotLast() =>
        Assert.True(new PaginationInfo(1, 10, 50).HasNextPage);

    [Fact]
    public void PaginationInfo_NoNextPage_WhenLast() =>
        Assert.False(new PaginationInfo(5, 10, 50).HasNextPage);

    [Fact]
    public void PaginationInfo_HasPreviousPage_WhenNotFirst() =>
        Assert.True(new PaginationInfo(2, 10, 50).HasPreviousPage);

    [Fact]
    public void PaginationInfo_NoPreviousPage_OnFirstPage() =>
        Assert.False(new PaginationInfo(1, 10, 50).HasPreviousPage);

    [Fact]
    public void PaginationInfo_PageSizeZero_TotalPagesIsZero() =>
        Assert.Equal(0, new PaginationInfo(1, 0, 50).TotalPages);

    [Fact]
    public void PaginationInfo_RecordEquality()
    {
        var a = new PaginationInfo(1, 10, 100);
        var b = new PaginationInfo(1, 10, 100);
        Assert.Equal(a, b);
    }
}
