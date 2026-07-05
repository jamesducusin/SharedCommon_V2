namespace SharedCommon.Security.UnitTests;

public sealed class SecurityOptionsTests
{
    [Fact]
    public void SecurityHeadersOptions_EnabledByDefault() =>
        Assert.True(new SecurityHeadersOptions().Enabled);

    [Fact]
    public void HstsOptions_DefaultMaxAge_IsOneYear() =>
        Assert.Equal(31_536_000, new HstsOptions().MaxAge);

    [Fact]
    public void HstsOptions_IncludeSubdomainsEnabled() =>
        Assert.True(new HstsOptions().IncludeSubdomains);

    [Fact]
    public void HstsOptions_PreloadDisabledByDefault() =>
        Assert.False(new HstsOptions().Preload);

    [Fact]
    public void CspOptions_DefaultSrcIsSelf() =>
        Assert.Equal("'self'", new CspOptions().DefaultSrc);

    [Fact]
    public void XFrameOptions_DefaultPolicyIsDeny() =>
        Assert.Equal("Deny", new XFrameOptions().Policy);

    [Fact]
    public void RateLimitOptions_EnabledByDefault() =>
        Assert.True(new RateLimitOptions().Enabled);

    [Fact]
    public void RateLimitOptions_HasDefaultPolicy()
    {
        var options = new RateLimitOptions();
        Assert.True(options.Policies.ContainsKey("Default"));
        Assert.Equal(100, options.Policies["Default"].MaxRequests);
        Assert.Equal(60, options.Policies["Default"].WindowSeconds);
    }

    [Fact]
    public void CorsOptions_EnabledByDefault() =>
        Assert.True(new CorsOptions().Enabled);

    [Fact]
    public void CorsOptions_AllowedMethods_IncludesCommonVerbs()
    {
        var methods = new CorsOptions().AllowedMethods;
        Assert.Contains("GET", methods);
        Assert.Contains("POST", methods);
        Assert.Contains("PUT", methods);
        Assert.Contains("DELETE", methods);
    }

    [Fact]
    public void HttpsOptions_EnforcedByDefault() =>
        Assert.True(new HttpsOptions().Enforced);

    [Fact]
    public void HttpsOptions_DefaultRedirectCode_Is307() =>
        Assert.Equal(307, new HttpsOptions().RedirectStatusCode);

    [Fact]
    public void InputValidation_BlocksSuspiciousPatternsDefault() =>
        Assert.True(new InputValidationOptions().BlockSuspiciousPatterns);

    [Fact]
    public void SectionName_IsCorrect() =>
        Assert.Equal("SharedCommon:Security", SecurityOptions.SectionName);
}
