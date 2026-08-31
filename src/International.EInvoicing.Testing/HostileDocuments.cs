using International.EInvoicing.Diagnostics;

namespace International.EInvoicing.Testing;

/// <summary>One document that fights back, and what reading it must do.</summary>
/// <param name="Name">What to call it in a test name.</param>
/// <param name="Xml">The document, as text.</param>
/// <param name="StaysUsable">Whether something usable must still come out.</param>
/// <param name="ExpectedDiagnostic">The code reading it must report, when one is promised.</param>
/// <param name="What">What is wrong with it, in a sentence.</param>
public sealed record HostileDocument(
    string Name,
    string Xml,
    bool StaysUsable,
    string? ExpectedDiagnostic,
    string What)
{
    /// <summary>
    /// The document as bytes, which is how some of these have to be read.
    /// </summary>
    /// <remarks>
    /// A mis-declared encoding does not survive being handed over as a <see cref="string"/>: by then the
    /// decoding has already happened, correctly or not. Those cases carry their own bytes.
    /// </remarks>
    public byte[] Bytes { get; init; } = System.Text.Encoding.UTF8.GetBytes(Xml);

    /// <inheritdoc />
    public override string ToString() => Name;
}

/// <summary>
/// Documents a trading partner will send you, and a specification never describes.
/// </summary>
/// <remarks>
/// <para>
/// These exist to defend one promise: <b>reading a document you received never throws</b>. A profile nobody
/// has heard of, a date in a format nobody agreed to, an amount with a comma, a code outside its list, an
/// element with no business term, XML that stops halfway — each is read as far as it can be, and what was
/// given up is reported rather than raised.
/// </para>
/// <para>
/// Run them against your own reader, your own profile, your own rule set. If one of them throws, the promise
/// is broken for whoever integrates with you.
/// </para>
/// </remarks>
public static class HostileDocuments
{
    /// <summary>Every document in the corpus.</summary>
    public static IReadOnlyList<HostileDocument> All { get; } =
    [
        new(
            "profile-nobody-registered",
            Invoice(profile: "urn:example:profile:9p9"),
            StaysUsable: true,
            DiagnosticCodes.UnknownProfile.Code,
            "declares a profile no registry knows; reading falls back and says so"),
        new(
            "date-in-a-format-nobody-agreed-to",
            Invoice(issueDate: "le 1er septembre"),
            StaysUsable: true,
            ExpectedDiagnostic: null,
            "BT-2 cannot be typed; the raw text survives and the field says why"),
        new(
            "amount-with-a-comma",
            Invoice(payable: "1 234,56"),
            StaysUsable: true,
            ExpectedDiagnostic: null,
            "a decimal comma, which XML Schema forbids and half of Europe writes"),
        new(
            "element-with-no-business-term",
            Invoice(extra: "<cbc:HouseNote>approved by finance</cbc:HouseNote>"),
            StaysUsable: true,
            ExpectedDiagnostic: null,
            "an element the model has no field for; it must be kept, not dropped"),
        new(
            "no-profile-at-all",
            Invoice(profile: null),
            StaysUsable: true,
            ExpectedDiagnostic: null,
            "BT-24 absent, which the norm forbids and receivers still see"),
        new(
            "xml-that-stops-halfway",
            "<Invoice xmlns=\"urn:oasis:names:specification:ubl:schema:xsd:Invoice-2\"><cbc:ID>",
            StaysUsable: false,
            Ubl.UblDiagnostics.MalformedDocument.Code,
            "the one case that yields nothing — and it is still a diagnostic, not a throw"),
        new(
            "not-xml-at-all",
            "this is a PDF, honestly",
            StaysUsable: false,
            ExpectedDiagnostic: null,
            "whatever was in the folder, handed over by mistake"),
        new(
            "empty",
            string.Empty,
            StaysUsable: false,
            ExpectedDiagnostic: null,
            "a zero-byte file, which is what a failed transfer leaves behind"),
        new(
            "a-root-element-from-somewhere-else",
            "<order xmlns=\"urn:example:orders\"><id>1</id></order>",
            StaysUsable: false,
            ExpectedDiagnostic: null,
            "well-formed XML that is not an invoice in any syntax"),
        new(
            "declares-utf8-and-sends-latin1",
            Latin1Invoice,
            StaysUsable: true,
            DiagnosticCodes.DeclaredEncodingMismatch.Code,
            "the sender's database is Latin-1 and its template says UTF-8; the buyer's name is the casualty")
        {
            Bytes = Latin1Bytes(),
        },
        new(
            "declares-an-encoding-nobody-carries-a-package-for",
            Invoice(declaration: "<?xml version=\"1.0\" encoding=\"windows-1252\"?>"),
            StaysUsable: true,
            DiagnosticCodes.UnsupportedEncoding.Code,
            "a real encoding this library does not decode; it must say what it assumed"),
        new(
            "nested-a-thousand-deep",
            DeeplyNested(1_000),
            StaysUsable: false,
            ExpectedDiagnostic: null,
            "not an attack that allocates, one that recurses; the depth limit exists for it"),
        new(
            "an-attachment-larger-than-the-limit",
            Invoice(extra: Attachment(2_000_000)),
            StaysUsable: true,
            ExpectedDiagnostic: null,
            "a base64 payload past DocumentLimits.MaxAttachmentBytes must be refused, not decoded"),
        new(
            "a-line-that-claims-a-quantity-of-nothing",
            Invoice(extra: string.Empty, payable: ""),
            StaysUsable: true,
            ExpectedDiagnostic: null,
            "an empty amount element, which the schema forbids and systems still emit"),
    ];

    /// <summary>An invoice whose buyer name needs a byte above 0x7F to survive.</summary>
    private static string Latin1Invoice => Invoice(buyer: "Müller und Söhne");

    /// <summary>The same invoice, declared UTF-8 and encoded ISO-8859-1 — the mismatch itself.</summary>
    private static byte[] Latin1Bytes() =>
        System.Text.Encoding.GetEncoding(28591).GetBytes(Latin1Invoice);

    private static string DeeplyNested(int depth) =>
        "<Invoice xmlns=\"urn:oasis:names:specification:ubl:schema:xsd:Invoice-2\">"
        + string.Concat(Enumerable.Repeat("<a>", depth))
        + string.Concat(Enumerable.Repeat("</a>", depth))
        + "</Invoice>";

    private static string Attachment(int bytes) =>
        "<cac:AdditionalDocumentReference>"
        + "<cbc:ID>ATTACHMENT-1</cbc:ID>"
        + "<cac:Attachment><cbc:EmbeddedDocumentBinaryObject mimeCode=\"application/pdf\" filename=\"big.pdf\">"
        + new string('A', bytes)
        + "</cbc:EmbeddedDocumentBinaryObject></cac:Attachment>"
        + "</cac:AdditionalDocumentReference>";

    /// <summary>The documents that must still produce something usable.</summary>
    public static IEnumerable<HostileDocument> Survivable => All.Where(document => document.StaysUsable);

    private static string Invoice(
        string? profile = "urn:cen.eu:en16931:2017",
        string issueDate = "2026-09-01",
        string payable = "540.00",
        string extra = "",
        string buyer = "Buyer SA",
        string declaration = "")
    {
        string customization = profile is null
            ? string.Empty
            : $"<cbc:CustomizationID>{profile}</cbc:CustomizationID>";

        return $"""
            {declaration}<Invoice xmlns="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2"
                     xmlns:cbc="urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2"
                     xmlns:cac="urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2">
              {customization}
              <cbc:ID>HOSTILE-1</cbc:ID>
              <cbc:IssueDate>{issueDate}</cbc:IssueDate>
              <cbc:InvoiceTypeCode>380</cbc:InvoiceTypeCode>
              <cbc:DocumentCurrencyCode>EUR</cbc:DocumentCurrencyCode>
              {extra}
              <cac:AccountingSupplierParty>
                <cac:Party><cac:PartyName><cbc:Name>Seller Ltd</cbc:Name></cac:PartyName></cac:Party>
              </cac:AccountingSupplierParty>
              <cac:AccountingCustomerParty>
                <cac:Party><cac:PartyName><cbc:Name>{buyer}</cbc:Name></cac:PartyName></cac:Party>
              </cac:AccountingCustomerParty>
              <cac:LegalMonetaryTotal>
                <cbc:PayableAmount currencyID="EUR">{payable}</cbc:PayableAmount>
              </cac:LegalMonetaryTotal>
            </Invoice>
            """;
    }
}
