using International.EInvoicing.Diagnostics;
using International.EInvoicing.Model;
using International.EInvoicing.Validation;

namespace International.EInvoicing.Testing;

/// <summary>An expectation this library's own promises did not meet.</summary>
public sealed class EInvoicingAssertionException : Exception
{
    /// <summary>A failure with a message that says what was expected and what happened.</summary>
    public EInvoicingAssertionException(string message)
        : base(message)
    {
    }

    /// <summary>A failure carrying an underlying cause.</summary>
    public EInvoicingAssertionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Required by the exception pattern; prefer the message constructor.</summary>
    public EInvoicingAssertionException()
    {
    }
}

/// <summary>
/// Assertions that read like the promise they defend.
/// </summary>
/// <remarks>
/// <para>
/// Framework-free on purpose: these throw an exception, which every test framework understands as a failure.
/// Depending on one of them would make this package pick your runner for you.
/// </para>
/// <para>
/// The messages carry the evidence — which rules fired, which rule sets did not run, what the diagnostic
/// actually said. An assertion that fails with "expected true, was false" costs an hour.
/// </para>
/// </remarks>
public static class Expect
{
    /// <summary>
    /// The document broke no rule <em>and</em> everything that should have checked it did.
    /// </summary>
    /// <remarks>
    /// Not the same as "no errors". A document judged by fewer rule sets than apply to it is unchecked, not
    /// valid, and a test that accepts the first as the second is a test that passes while proving nothing.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="report"/> is <c>null</c>.</exception>
    /// <exception cref="EInvoicingAssertionException">A rule failed, or a rule set did not run.</exception>
    public static void Conforming(ValidationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (report.IsConforming)
        {
            return;
        }

        throw new EInvoicingAssertionException(
            "Expected a conforming document." + Environment.NewLine + Evidence(report));
    }

    /// <summary>A named rule failed — the half of a rule's test that is usually forgotten.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    /// <exception cref="EInvoicingAssertionException">The rule did not fail.</exception>
    public static void Failed(ValidationReport report, string ruleIdentifier)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(ruleIdentifier);

        if (report.Failed(ruleIdentifier))
        {
            return;
        }

        throw new EInvoicingAssertionException(
            $"Expected {ruleIdentifier} to fail, and it did not." + Environment.NewLine + Evidence(report));
    }

    /// <summary>A named rule did not fail.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    /// <exception cref="EInvoicingAssertionException">The rule failed.</exception>
    public static void Passed(ValidationReport report, string ruleIdentifier)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(ruleIdentifier);

        if (!report.Failed(ruleIdentifier))
        {
            return;
        }

        throw new EInvoicingAssertionException(
            $"Expected {ruleIdentifier} to pass, and it failed." + Environment.NewLine + Evidence(report));
    }

    /// <summary>A named rule set ran, so the report is about what you think it is about.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    /// <exception cref="EInvoicingAssertionException">Nothing by that name ran.</exception>
    public static void Checked(ValidationReport report, string ruleSetName)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(ruleSetName);

        bool ran = report.RuleSets.Any(outcome =>
            outcome.Ran && outcome.Name.Contains(ruleSetName, StringComparison.OrdinalIgnoreCase));

        if (ran)
        {
            return;
        }

        throw new EInvoicingAssertionException(
            $"Expected a rule set named like '{ruleSetName}' to have run." + Environment.NewLine + Evidence(report));
    }

    /// <summary>Reading reported a diagnostic by code, with the fallback it applied.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    /// <exception cref="EInvoicingAssertionException">Nothing reported that code.</exception>
    public static void Reported(DocumentResult result, string diagnosticCode)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(diagnosticCode);

        if (result.Diagnostics.Any(diagnostic =>
            string.Equals(diagnostic.Code, diagnosticCode, StringComparison.Ordinal)))
        {
            return;
        }

        string reported = result.Diagnostics.Count == 0
            ? "nothing was reported"
            : string.Join(", ", result.Diagnostics.Select(diagnostic => diagnostic.Code).Distinct(StringComparer.Ordinal));

        throw new EInvoicingAssertionException(
            $"Expected diagnostic {diagnosticCode}; {reported}.");
    }

    /// <summary>
    /// Reading produced something usable, whatever it had to give up doing so.
    /// </summary>
    /// <remarks>
    /// The promise a hostile-document test exists to check: a profile nobody registered, a date in a format
    /// nobody agreed to, an element with no business term — none of them costs you the document.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is <c>null</c>.</exception>
    /// <exception cref="EInvoicingAssertionException">Nothing usable came out.</exception>
    public static void Usable(DocumentResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsUsable)
        {
            return;
        }

        throw new EInvoicingAssertionException(
            "Expected a usable document." + Environment.NewLine
            + string.Join(Environment.NewLine, result.Diagnostics.Select(diagnostic => "  " + diagnostic)));
    }

    /// <summary>The round trip lost nothing.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is <c>null</c>.</exception>
    /// <exception cref="EInvoicingAssertionException">Something the original carried is missing.</exception>
    public static void LostNothing(RoundTripResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsFaithful)
        {
            return;
        }

        throw new EInvoicingAssertionException(
            "The round trip lost elements the original carried:" + Environment.NewLine
            + string.Join(Environment.NewLine, result.Lost.Select(lost => "  " + lost)));
    }

    /// <summary>
    /// A field kept the exact text it came from, whatever the typed value became.
    /// </summary>
    /// <remarks>
    /// Takes the interface rather than the generic type so it works for every field alike — a date, an
    /// amount, an identifier — which is the whole point of them sharing one.
    /// </remarks>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    /// <exception cref="EInvoicingAssertionException">The raw text is something else.</exception>
    public static void Raw(Values.IField field, string expectedRaw)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(expectedRaw);

        if (string.Equals(field.Raw, expectedRaw, StringComparison.Ordinal))
        {
            return;
        }

        throw new EInvoicingAssertionException(
            $"Expected the field to have kept '{expectedRaw}'; it kept '{field.Raw ?? "(nothing)"}'.");
    }

    private static string Evidence(ValidationReport report)
    {
        IEnumerable<string> lines = report.RuleSets
            .Select(outcome => "  " + outcome)
            .Concat(report.OfAtLeast(RuleSeverity.Warning).Select(message => "  " + message));

        return string.Join(Environment.NewLine, lines);
    }
}
