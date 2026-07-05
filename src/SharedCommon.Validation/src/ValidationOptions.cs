namespace SharedCommon.Validation;

/// <summary>
/// Configuration for SharedCommon.Validation.
///
/// Configure via appsettings.json:
/// <code>
/// {
///   "SharedCommon": {
///     "Validation": {
///       "AutomaticControllerValidation": true
///     }
///   }
/// }
/// </code>
/// </summary>
public sealed class ValidationOptions
{
    /// <summary>Configuration section key: <c>SharedCommon:Validation</c>.</summary>
    public const string SectionName = "SharedCommon:Validation";

    /// <summary>Enable the validation pipeline globally. Default: <c>true</c>.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Register <see cref="AutoValidationFilter"/> as a global MVC action filter
    /// so every controller action is validated automatically.
    /// Default: <c>true</c>.
    /// </summary>
    public bool AutomaticControllerValidation { get; set; } = true;

    /// <summary>Language manager settings for localized validation messages.</summary>
    public LanguageManagerOptions LanguageManager { get; set; } = new();

    /// <summary>Named rule-set identifiers used by convention in validators.</summary>
    public RuleSetNamesOptions RuleSets { get; set; } = new();
}

/// <summary>Localization settings for FluentValidation messages.</summary>
public sealed class LanguageManagerOptions
{
    /// <summary>Enable localized validation messages. Default: <c>false</c>.</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>IETF language tag for the default locale. Default: <c>en</c>.</summary>
    public string DefaultLanguage { get; set; } = "en";
}

/// <summary>Named rule set identifiers used by convention across validators.</summary>
public sealed class RuleSetNamesOptions
{
    /// <summary>Rule set name applied to create operations. Default: <c>CreateRuleSet</c>.</summary>
    public string Create { get; set; } = "CreateRuleSet";

    /// <summary>Rule set name applied to update operations. Default: <c>UpdateRuleSet</c>.</summary>
    public string Update { get; set; } = "UpdateRuleSet";

    /// <summary>Rule set name applied to delete operations. Default: <c>DeleteRuleSet</c>.</summary>
    public string Delete { get; set; } = "DeleteRuleSet";
}
