namespace International.EInvoicing.Validation;

/// <summary>
/// Whether a rule set actually ran. This is what stops a validation report from claiming more than it did.
/// </summary>
/// <param name="Name">The rule set's name, as a reader would recognise it.</param>
/// <param name="Version">Which version of it, because rules change between releases.</param>
/// <param name="Ran">Whether it was applied to the document.</param>
/// <param name="SkippedBecause">Why it was not, when it was not.</param>
public sealed record RuleSetOutcome(string Name, string Version, bool Ran, string? SkippedBecause = null)
{
    /// <inheritdoc />
    public override string ToString() =>
        Ran ? $"{Name} {Version}  ran" : $"{Name} {Version}  skipped — {SkippedBecause}";
}
