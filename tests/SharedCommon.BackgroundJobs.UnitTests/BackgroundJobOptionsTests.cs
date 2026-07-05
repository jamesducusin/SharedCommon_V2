namespace SharedCommon.BackgroundJobs.UnitTests;

public sealed class BackgroundJobOptionsTests
{
    [Fact]
    public void Defaults_AreCorrect()
    {
        var options = new BackgroundJobOptions();
        Assert.Equal(JobStorageBackend.InMemory, options.Backend);
        Assert.Equal(5, options.WorkerCount);
        Assert.Equal("default", options.DefaultQueue);
        Assert.False(options.EnableDashboard);
        Assert.Equal("/jobs", options.DashboardPath);
        Assert.Equal(string.Empty, options.DashboardRequiredRole);
    }

    [Fact]
    public void SqlServerBackend_EnumHasCorrectValues()
    {
        Assert.True(Enum.IsDefined(JobStorageBackend.InMemory));
        Assert.True(Enum.IsDefined(JobStorageBackend.SqlServer));
    }
}
