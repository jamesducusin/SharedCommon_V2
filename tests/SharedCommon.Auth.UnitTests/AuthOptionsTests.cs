namespace SharedCommon.Auth.UnitTests;

public sealed class AuthOptionsTests
{
    [Fact]
    public void JwtOptions_DefaultsEnabled() =>
        Assert.True(new JwtOptions().Enabled);

    [Fact]
    public void JwtOptions_DefaultAlgorithm_IsHS256() =>
        Assert.Equal("HS256", new JwtOptions().Algorithm);

    [Fact]
    public void JwtOptions_DefaultExpiration_Is60Minutes() =>
        Assert.Equal(60, new JwtOptions().ExpirationMinutes);

    [Fact]
    public void JwtOptions_RefreshTokenExpiration_Is7Days() =>
        Assert.Equal(7, new JwtOptions().RefreshTokenExpirationDays);

    [Fact]
    public void JwtValidationOptions_ValidatesAudienceIssuerLifetime()
    {
        var options = new JwtValidationOptions();
        Assert.True(options.ValidateAudience);
        Assert.True(options.ValidateIssuer);
        Assert.True(options.ValidateLifetime);
    }

    [Fact]
    public void JwtValidationOptions_ClockSkew_DefaultsZero() =>
        Assert.Equal(0, new JwtValidationOptions().ClockSkewSeconds);

    [Fact]
    public void PasswordPolicyOptions_DefaultMinLength_Is12() =>
        Assert.Equal(12, new PasswordPolicyOptions().MinLength);

    [Fact]
    public void PasswordPolicyOptions_AllComplexityRequirements_EnabledByDefault()
    {
        var policy = new PasswordPolicyOptions();
        Assert.True(policy.RequireUppercase);
        Assert.True(policy.RequireLowercase);
        Assert.True(policy.RequireDigits);
        Assert.True(policy.RequireSpecialChars);
    }

    [Fact]
    public void OAuthOptions_DefaultsDisabled() =>
        Assert.False(new OAuthOptions().Enabled);

    [Fact]
    public void TokenBlacklist_DefaultsEnabled_WithRedisBackend()
    {
        var options = new TokenBlacklistOptions();
        Assert.True(options.Enabled);
        Assert.Equal("Redis", options.Backend);
    }

    [Fact]
    public void SectionName_IsCorrect() =>
        Assert.Equal("SharedCommon:Auth", AuthOptions.SectionName);
}
