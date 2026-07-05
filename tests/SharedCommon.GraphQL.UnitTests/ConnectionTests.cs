namespace SharedCommon.GraphQL.UnitTests;

public sealed class ConnectionTests
{
    [Fact]
    public void From_EmptyList_ReturnsEmptyConnection()
    {
        var conn = Connection<string>.From([], 0);
        Assert.Empty(conn.Edges);
        Assert.Equal(0, conn.TotalCount);
        Assert.Null(conn.PageInfo.StartCursor);
        Assert.Null(conn.PageInfo.EndCursor);
    }

    [Fact]
    public void From_WithItems_CreatesEdgesWithCursors()
    {
        var conn = Connection<string>.From(["a", "b", "c"], 3);
        Assert.Equal(3, conn.Edges.Count);
        Assert.All(conn.Edges, edge => Assert.False(string.IsNullOrEmpty(edge.Cursor)));
    }

    [Fact]
    public void From_PageInfo_HasNextPage_WhenMoreResults()
    {
        var conn = Connection<int>.From([1, 2, 3], totalCount: 10, offset: 0);
        Assert.True(conn.PageInfo.HasNextPage);
    }

    [Fact]
    public void From_PageInfo_NoNextPage_WhenAllReturned()
    {
        var conn = Connection<int>.From([1, 2, 3], totalCount: 3, offset: 0);
        Assert.False(conn.PageInfo.HasNextPage);
    }

    [Fact]
    public void From_PageInfo_HasPreviousPage_WhenOffsetPositive()
    {
        var conn = Connection<int>.From([4, 5, 6], totalCount: 10, offset: 3);
        Assert.True(conn.PageInfo.HasPreviousPage);
    }

    [Fact]
    public void From_PageInfo_NoPreviousPage_WhenOffsetZero()
    {
        var conn = Connection<int>.From([1, 2], totalCount: 5, offset: 0);
        Assert.False(conn.PageInfo.HasPreviousPage);
    }

    [Fact]
    public void From_Cursors_AreBase64Encoded()
    {
        var conn = Connection<string>.From(["x"], 1, offset: 0);
        var cursor = conn.Edges[0].Cursor;
        var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
        Assert.Equal("cursor:0", decoded);
    }

    [Fact]
    public void From_WithOffset_CursorsReflectOffset()
    {
        var conn = Connection<string>.From(["x"], 10, offset: 5);
        var cursor = conn.Edges[0].Cursor;
        var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
        Assert.Equal("cursor:5", decoded);
    }

    [Fact]
    public void From_StartAndEndCursors_MatchFirstAndLastEdge()
    {
        var conn = Connection<int>.From([1, 2, 3], 3);
        Assert.Equal(conn.Edges[0].Cursor, conn.PageInfo.StartCursor);
        Assert.Equal(conn.Edges[^1].Cursor, conn.PageInfo.EndCursor);
    }

    [Fact]
    public void Edge_ExposesNodeAndCursor()
    {
        var edge = new Edge<string>("hello", "cursor");
        Assert.Equal("hello", edge.Node);
        Assert.Equal("cursor", edge.Cursor);
    }

    [Fact]
    public void PageInfo_RecordEquality()
    {
        var a = new PageInfo(true, false, "start", "end");
        var b = new PageInfo(true, false, "start", "end");
        Assert.Equal(a, b);
    }
}
