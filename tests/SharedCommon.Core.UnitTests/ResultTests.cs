using SharedCommon.Core;

namespace SharedCommon.Core.UnitTests;

public class ResultTests
{
    // ── Result (untyped) ──────────────────────────────────────────────────────

    [Fact]
    public void Result_Success_IsSuccess_ReturnsTrue()
    {
        Result result = new Result.Success();
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
    }

    [Fact]
    public void Result_Success_WithData_ExposesData()
    {
        var data = new { Id = 1 };
        Result result = new Result.Success(data);
        var success = Assert.IsType<Result.Success>(result);
        Assert.Equal(data, success.Data);
    }

    [Fact]
    public void Result_Failure_IsFailure_ReturnsTrue()
    {
        Result result = new Result.Failure("ERR", "Something went wrong");
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Result_Failure_ExposesCodeAndMessage()
    {
        var ex = new InvalidOperationException("inner");
        Result result = new Result.Failure("NOT_FOUND", "Not found", ex);
        var failure = Assert.IsType<Result.Failure>(result);
        Assert.Equal("NOT_FOUND", failure.Code);
        Assert.Equal("Not found", failure.Message);
        Assert.Same(ex, failure.Exception);
    }

    [Fact]
    public void Result_Validation_IsFailure_ReturnsTrue()
    {
        var errors = new Dictionary<string, string[]> { ["Field"] = ["Required"] };
        Result result = new Result.Validation(errors);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Result_Validation_ExposesErrors()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["Email"] = ["Invalid format"],
            ["Name"] = ["Required", "Too short"]
        };
        var validation = new Result.Validation(errors);
        Assert.Equal(2, validation.Errors.Count);
        Assert.Equal(["Invalid format"], validation.Errors["Email"]);
    }

    // ── Result<T> (typed) ─────────────────────────────────────────────────────

    [Fact]
    public void ResultT_Ok_ReturnsSuccessWithData()
    {
        var result = Result<int>.Ok(42);
        var success = Assert.IsType<Result<int>.Success>(result);
        Assert.Equal(42, success.Data);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ResultT_Fail_ReturnsFailureWithCodeAndMessage()
    {
        var result = Result<string>.Fail("FORBIDDEN", "Access denied");
        var failure = Assert.IsType<Result<string>.Failure>(result);
        Assert.Equal("FORBIDDEN", failure.Code);
        Assert.Equal("Access denied", failure.Message);
        Assert.Null(failure.Exception);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void ResultT_Fail_WithException_ExposesException()
    {
        var ex = new TimeoutException();
        var result = Result<string>.Fail("TIMEOUT", "Timed out", ex);
        var failure = Assert.IsType<Result<string>.Failure>(result);
        Assert.Same(ex, failure.Exception);
    }

    [Fact]
    public void ResultT_Invalid_WithDictionary_ReturnsValidation()
    {
        var errors = new Dictionary<string, string[]> { ["Age"] = ["Must be >= 18"] };
        var result = Result<string>.Invalid(errors);
        var validation = Assert.IsType<Result<string>.Validation>(result);
        Assert.Equal(["Must be >= 18"], validation.Errors["Age"]);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void ResultT_Invalid_SingleField_BuildsDictionary()
    {
        var result = Result<string>.Invalid("Email", "Invalid email format");
        var validation = Assert.IsType<Result<string>.Validation>(result);
        Assert.Single(validation.Errors);
        Assert.Equal(["Invalid email format"], validation.Errors["Email"]);
    }

    [Fact]
    public void ResultT_Success_IsSuccess_ReturnsTrue()
    {
        var result = Result<bool>.Ok(true);
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
    }

    [Fact]
    public void ResultT_PatternMatch_AllArmsReachable()
    {
        var results = new Result<string>[]
        {
            Result<string>.Ok("data"),
            Result<string>.Fail("ERR", "error"),
            Result<string>.Invalid("Field", "message")
        };

        var labels = results.Select(r => r switch
        {
            Result<string>.Success s  => $"ok:{s.Data}",
            Result<string>.Failure f  => $"fail:{f.Code}",
            Result<string>.Validation v => $"invalid:{v.Errors.Count}",
            _                         => "unknown"
        }).ToArray();

        Assert.Equal(["ok:data", "fail:ERR", "invalid:1"], labels);
    }

    [Fact]
    public void ResultT_Failure_DefaultExceptionIsNull()
    {
        var result = Result<int>.Fail("ERR", "error");
        var failure = Assert.IsType<Result<int>.Failure>(result);
        Assert.Null(failure.Exception);
    }

    [Fact]
    public void ResultT_RecordEquality_SameValues_AreEqual()
    {
        var a = Result<int>.Ok(1);
        var b = Result<int>.Ok(1);
        Assert.Equal(a, b);
    }

    [Fact]
    public void ResultT_RecordEquality_DifferentValues_AreNotEqual()
    {
        var a = Result<int>.Ok(1);
        var b = Result<int>.Ok(2);
        Assert.NotEqual(a, b);
    }
}
