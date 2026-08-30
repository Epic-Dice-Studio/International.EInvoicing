using System.Text;
using International.EInvoicing.Diagnostics;

namespace International.EInvoicing.Validation;

/// <summary>
/// What validating a document produced: the rules that fired, and — just as important — which rule sets ran.
/// </summary>
/// <remarks>
/// A report says what was checked, not only what failed. A document validated against fewer rule sets than
/// apply to it is not valid, it is unchecked, and <see cref="IsComplete"/> is what distinguishes the two.
/// Presenting the first as the second is the way a validator does the most damage.
/// </remarks>
/// <param name="Messages">Every rule that fired.</param>
/// <param name="RuleSets">Every rule set that applied, and whether it ran.</param>
public sealed record ValidationReport(
    IReadOnlyList<ValidationMessage> Messages,
    IReadOnlyList<RuleSetOutcome> RuleSets)
{
    /// <summary>A report for a document nothing could be checked against.</summary>
    public static ValidationReport Empty { get; } = new([], []);

    /// <summary>Whether no rule failed at <see cref="RuleSeverity.Error"/>.</summary>
    public bool IsValid => !Messages.Any(message => message.Severity == RuleSeverity.Error);

    /// <summary>Whether every rule set that applies to this document actually ran.</summary>
    public bool IsComplete => RuleSets.All(ruleSet => ruleSet.Ran);

    /// <summary>
    /// Whether the document can be relied upon: it broke no rule, and everything that should have checked it
    /// did. Anything less deserves a human.
    /// </summary>
    public bool IsConforming => IsValid && IsComplete;

    /// <summary>The messages of at least the given severity.</summary>
    public IEnumerable<ValidationMessage> OfAtLeast(RuleSeverity severity) =>
        Messages.Where(message => message.Severity >= severity);

    /// <summary>The rules that failed.</summary>
    public IEnumerable<ValidationMessage> Errors => OfAtLeast(RuleSeverity.Error);

    /// <summary>The rules that fired without failing the document.</summary>
    public IEnumerable<ValidationMessage> Warnings =>
        Messages.Where(message => message.Severity == RuleSeverity.Warning);

    /// <summary>The rule sets that did not run, and why.</summary>
    public IEnumerable<RuleSetOutcome> NotRun => RuleSets.Where(ruleSet => !ruleSet.Ran);

    /// <summary>Whether a particular rule failed, by its identifier.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="ruleIdentifier"/> is <c>null</c>.</exception>
    public bool Failed(string ruleIdentifier)
    {
        ArgumentNullException.ThrowIfNull(ruleIdentifier);

        return Errors.Any(message =>
            string.Equals(message.RuleIdentifier, ruleIdentifier, StringComparison.Ordinal));
    }

    /// <summary>
    /// Hands the report back when the document conforms, and throws with the whole report when it does not.
    /// </summary>
    /// <remarks>
    /// For a pipeline that must not let a bad document through: one call, and the exception message is the
    /// report a person would have had to read anyway. Note that it insists on
    /// <see cref="IsConforming"/> — a document nothing checked is not a document that passed.
    /// </remarks>
    /// <exception cref="DocumentException">The document broke a rule, or something did not run.</exception>
    public ValidationReport EnsureConforming() =>
        IsConforming
            ? this
            : throw new DocumentException(
                IsValid
                    ? $"The document was not fully checked.{Environment.NewLine}{this}"
                    : $"The document is not valid.{Environment.NewLine}{this}");

    /// <summary>A report combining this one with another.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
    public ValidationReport And(ValidationReport other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new ValidationReport([.. Messages, .. other.Messages], [.. RuleSets, .. other.RuleSets]);
    }

    /// <summary>A summary a person can read, saying what ran as well as what failed.</summary>
    public override string ToString()
    {
        var text = new StringBuilder();
        text.Append(IsConforming ? "Conforming" : IsValid ? "Valid but incomplete" : "Not valid")
            .Append(" — ")
            .Append(Messages.Count)
            .AppendLine(" message(s)");

        foreach (RuleSetOutcome ruleSet in RuleSets)
        {
            text.Append("  ").AppendLine(ruleSet.ToString());
        }

        foreach (ValidationMessage message in OfAtLeast(RuleSeverity.Warning))
        {
            text.Append("  ").AppendLine(message.ToString());
        }

        return text.ToString();
    }
}
