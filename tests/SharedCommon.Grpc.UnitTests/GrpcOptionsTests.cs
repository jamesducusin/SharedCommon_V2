namespace SharedCommon.Grpc.UnitTests;

public sealed class GrpcOptionsTests
{
    [Fact]
    public void EnableReflection_DisabledByDefault() =>
        Assert.False(new GrpcOptions().EnableReflection);

    [Fact]
    public void EnableHealthCheck_EnabledByDefault() =>
        Assert.True(new GrpcOptions().EnableHealthCheck);

    [Fact]
    public void MaxReceiveMessageSizeBytes_DefaultIsFourMb() =>
        Assert.Equal(4 * 1024 * 1024, new GrpcOptions().MaxReceiveMessageSizeBytes);

    [Fact]
    public void MaxSendMessageSizeBytes_DefaultIsFourMb() =>
        Assert.Equal(4 * 1024 * 1024, new GrpcOptions().MaxSendMessageSizeBytes);

    [Fact]
    public void CorrelationIdHeader_IsLowercase() =>
        Assert.Equal("x-correlation-id", new GrpcOptions().CorrelationIdHeader);

    [Fact]
    public void SectionName_IsCorrect() =>
        Assert.Equal("SharedCommon:Grpc", GrpcOptions.SectionName);
}
