namespace SharedCommon.Messaging.UnitTests;

public sealed class MessagingOptionsTests
{
    [Fact]
    public void MessagingOptions_DefaultTransport_IsRabbitMQ() =>
        Assert.Equal(MessagingTransport.RabbitMQ, new MessagingOptions().Transport);

    [Fact]
    public void MessagingTransport_BothValuesExist()
    {
        Assert.True(Enum.IsDefined(MessagingTransport.RabbitMQ));
        Assert.True(Enum.IsDefined(MessagingTransport.Kafka));
    }

    [Fact]
    public void RabbitMqOptions_Defaults_AreCorrect()
    {
        var options = new RabbitMqOptions();
        Assert.Equal("localhost", options.Host);
        Assert.Equal(5672, options.Port);
        Assert.Equal("/", options.VirtualHost);
        Assert.Equal("guest", options.Username);
    }

    [Fact]
    public void KafkaOptions_Defaults_AreCorrect()
    {
        var options = new KafkaOptions();
        Assert.Equal("localhost:9092", options.BootstrapServers);
        Assert.Equal("shared-common", options.ConsumerGroupId);
        Assert.Equal("Plaintext", options.SecurityProtocol);
        Assert.Null(options.SaslUsername);
        Assert.Null(options.SaslPassword);
    }

    [Fact]
    public void RetryOptions_Defaults_AreCorrect()
    {
        var options = new RetryOptions();
        Assert.Equal(3, options.MaxAttempts);
        Assert.Equal(TimeSpan.FromSeconds(1), options.MinInterval);
        Assert.Equal(TimeSpan.FromSeconds(30), options.MaxInterval);
    }

    [Fact]
    public void SectionName_IsCorrect() =>
        Assert.Equal("SharedCommon:Messaging", MessagingOptions.SectionName);
}
