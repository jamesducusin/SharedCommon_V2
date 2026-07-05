namespace SharedCommon.Validation.UnitTests;

public sealed class ValidationOptionsTests
{
    [Fact]
    public void ValidationOptions_EnabledByDefault() =>
        Assert.True(new ValidationOptions().Enabled);

    [Fact]
    public void ValidationOptions_AutomaticControllerValidation_EnabledByDefault() =>
        Assert.True(new ValidationOptions().AutomaticControllerValidation);

    [Fact]
    public void LanguageManagerOptions_DefaultLanguage_IsEnglish() =>
        Assert.Equal("en", new LanguageManagerOptions().DefaultLanguage);

    [Fact]
    public void LanguageManagerOptions_DisabledByDefault() =>
        Assert.False(new LanguageManagerOptions().Enabled);

    [Fact]
    public void RuleSetNamesOptions_DefaultNames_AreConventional()
    {
        var options = new RuleSetNamesOptions();
        Assert.Equal("CreateRuleSet", options.Create);
        Assert.Equal("UpdateRuleSet", options.Update);
        Assert.Equal("DeleteRuleSet", options.Delete);
    }

    [Fact]
    public void SectionName_IsCorrect() =>
        Assert.Equal("SharedCommon:Validation", ValidationOptions.SectionName);
}
