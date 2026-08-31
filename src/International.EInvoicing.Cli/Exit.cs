namespace International.EInvoicing.Cli;

/// <summary>
/// What the process returns.
/// </summary>
/// <remarks>
/// A validator's exit code is read by scripts that will never read its output, so the three cases have to be
/// distinguishable: the document is fine, the document is not, and the tool could not tell. Conflating the
/// last two is how a broken pipeline passes.
/// </remarks>
internal static class Exit
{
    /// <summary>The document conforms, or the command did what was asked.</summary>
    public const int Ok = 0;

    /// <summary>The document was read and judged, and it did not pass.</summary>
    public const int DocumentRejected = 1;

    /// <summary>The tool could not do the job: bad arguments, missing file, unreadable rules.</summary>
    public const int CouldNotRun = 2;
}
