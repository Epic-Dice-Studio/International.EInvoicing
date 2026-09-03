using International.EInvoicing.Diagnostics;
using International.EInvoicing.Profiles;
using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>
/// A lifecycle message: what happened to one or more documents, who says so, and when.
/// </summary>
/// <remarks>
/// The model is the generic UN/CEFACT one. National profilings — the French CDV profile above all — restrict
/// it and give meaning to its codes; they do not change its shape, which is what lets an unknown profiling
/// still be read.
/// </remarks>
public sealed class LifecycleStatusMessage : InvoiceNode
{
    /// <summary>The message's own identifier.</summary>
    public IdentifierField Identifier { get; set; }

    /// <summary>The message's name, when the sender gave one.</summary>
    public TextField Name { get; set; }

    /// <summary>A free-text note about the message as a whole.</summary>
    public TextField Note { get; set; }

    /// <summary>When the message was created.</summary>
    public DateTimeField IssuedAt { get; set; }

    /// <summary>The business process this message takes part in.</summary>
    public IdentifierField BusinessProcessType { get; set; }

    /// <summary>
    /// What the message claims to conform to. For the French lifecycle statuses this is
    /// <c>urn.cpro.gouv.fr:1p0:CDV:invoice</c>.
    /// </summary>
    public ProfileIdentifier SpecificationIdentifier { get; set; }

    /// <summary>Who sent the message.</summary>
    public StatusParty? Sender { get; set; }

    /// <summary>Who issued it, when that differs from who sent it.</summary>
    public StatusParty? Issuer { get; set; }

    /// <summary>Who it is for. A message may address several recipients — a platform and a directory, say.</summary>
    public List<StatusParty> Recipients { get; } = [];

    /// <summary>Whether the message reports on more than one document.</summary>
    public IndicatorField CoversMultipleDocuments { get; set; }

    /// <summary>The type of the acknowledgement itself.</summary>
    public CodeField TypeCode { get; set; }

    /// <summary>When the status being reported occurred, as opposed to when the message was written.</summary>
    public DateTimeField StatusIssuedAt { get; set; }

    /// <summary>What happened to each document reported on.</summary>
    public List<ReferencedDocumentStatus> References { get; } = [];

    /// <summary>What was reported while this message was read. Empty for a message built in code.</summary>
    /// <remarks>Set by whichever reader produced the message, including a reader you wrote yourself.</remarks>
    public IReadOnlyList<Diagnostic> Diagnostics { get; set; } = [];

    /// <summary>
    /// How the declared profile was resolved, and what was given up along the way. <c>null</c> for a message
    /// built in code.
    /// </summary>
    /// <remarks>Set by whichever reader produced the message, including a reader you wrote yourself.</remarks>
    public ProfileResolution? Profile { get; set; }
}
