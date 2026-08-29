namespace International.EInvoicing.Validation;

/// <summary>
/// One rule that fired, named the way the rule set names it so it can be looked up in the published
/// specification.
/// </summary>
/// <param name="RuleIdentifier">The rule's own identifier, such as <c>BR-CO-10</c>.</param>
/// <param name="Severity">How much it matters.</param>
/// <param name="Message">What the rule set says about it.</param>
public sealed record ValidationMessage(string RuleIdentifier, RuleSeverity Severity, string Message)
{
    /// <summary>Where in the document the rule fired.</summary>
    public string? Location { get; init; }

    /// <summary>The business term concerned, when the rule names one.</summary>
    public string? BusinessTerm { get; init; }

    /// <summary>Which rule set the rule belongs to.</summary>
    public string? RuleSet { get; init; }

    /// <inheritdoc />
    public override string ToString() =>
        Location is null
            ? $"{RuleIdentifier}  {Severity}  {Message}"
            : $"{RuleIdentifier}  {Severity}  {Message} at {Location}";
}
