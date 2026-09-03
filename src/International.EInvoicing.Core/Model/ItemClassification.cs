using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>
/// A classification an item falls under: the code, and what that code is called.
/// </summary>
/// <remarks>
/// The code alone is what a rule checks and what a customs system reads, which is why it was for a long time
/// the only part this model kept. The name is what a person reads, and both syntaxes carry it — UBL as the
/// <c>name</c> attribute on the code, CII as a <c>ClassName</c> element beside it. A document that states
/// "70.20.11" and "renting of own property" has said two things, and dropping the second was losing one.
/// </remarks>
public sealed class ItemClassification : InvoiceNode
{
    /// <summary>The code, and the list it was drawn from.</summary>
    public CodeField Code { get; set; }

    /// <summary>What the code is called, when the document says.</summary>
    public TextField Name { get; set; }

    /// <summary>Wraps a bare code, for the common case of a classification with no name.</summary>
    public static implicit operator ItemClassification(CodeField code) => new() { Code = code };

    /// <summary>Wraps a bare code, for the common case of a classification with no name.</summary>
    public static ItemClassification FromCode(CodeField code) => new() { Code = code };
}
