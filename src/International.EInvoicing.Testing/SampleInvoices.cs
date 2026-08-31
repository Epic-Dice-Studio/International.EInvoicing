using International.EInvoicing.Building;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;

namespace International.EInvoicing.Testing;

/// <summary>
/// Documents to test against.
/// </summary>
/// <remarks>
/// <para>
/// Every one of these conforms to EN 16931 as this library validates it, which is the point: when a test
/// using one of them fails, the fixture is not the suspect. Building a document that satisfies the base norm
/// takes some thirty terms — type code, both postal addresses, a line identifier, a quantity with a unit,
/// a net price — and getting that wrong is where an afternoon goes.
/// </para>
/// <para>
/// They are built, not stored as files, so a caller can change one term and keep the rest correct. The
/// artefact corpora themselves — Factur-X's, KoSIT's, Peppol's — are not redistributable and are not here;
/// see <c>build/fetch-specs.sh</c>.
/// </para>
/// </remarks>
public static class SampleInvoices
{
    /// <summary>An invoice EN 16931 accepts, in the profile you name.</summary>
    /// <param name="specification">The profile to declare. Defaults to plain EN 16931 for UBL.</param>
    /// <param name="configure">Anything else to change before it is built.</param>
    public static EInvoice Conforming(
        ProfileIdentifier? specification = null,
        Action<EInvoiceBuilder>? configure = null)
    {
        EInvoiceBuilder builder = Base(specification ?? KnownProfiles.En16931Ubl.Id)
            .OfType(InvoiceTypeCodes.CommercialInvoice)
            .AddLine(line => line
                .WithIdentifier("1")
                .WithItem("Consulting")
                .WithQuantity(1m, "DAY")
                .WithNetPrice(450m)
                .WithNetAmount(450m)
                .WithVat(VatCategoryCodes.Standard, 20m));

        configure?.Invoke(builder);

        return builder.WithComputedVatBreakdown().WithComputedTotals().Build();
    }

    /// <summary>A credit note EN 16931 accepts.</summary>
    /// <param name="specification">The profile to declare. Defaults to plain EN 16931 for UBL.</param>
    /// <param name="configure">Anything else to change before it is built.</param>
    public static EInvoice ConformingCreditNote(
        ProfileIdentifier? specification = null,
        Action<EInvoiceBuilder>? configure = null) =>
        Conforming(specification, builder =>
        {
            builder.OfType(InvoiceTypeCodes.CreditNote);
            configure?.Invoke(builder);
        });

    /// <summary>
    /// The same invoice carrying an element the model has no field for.
    /// </summary>
    /// <remarks>
    /// For testing that nothing a document contained is dropped: this element has no business term, no
    /// mapping and no future, and it must still come out the other side when the document is written back in
    /// the syntax it came from.
    /// </remarks>
    /// <param name="specification">The profile to declare.</param>
    public static EInvoice WithSomethingUnmapped(ProfileIdentifier? specification = null) =>
        Conforming(specification, builder => builder.Extend(invoice => invoice.Extensions.Add(
            new ExtensionElement(
                HouseNamespace,
                "Approval",
                $"<house:Approval xmlns:house=\"{HouseNamespace}\">signed off by finance</house:Approval>"))));

    /// <summary>The namespace <see cref="WithSomethingUnmapped"/> uses. Invented, and deliberately so.</summary>
    public const string HouseNamespace = "urn:example:house:1p0";

    private static EInvoiceBuilder Base(ProfileIdentifier specification) => EInvoiceBuilder
        .Create(specification)
        .WithNumber("SAMPLE-2026-001")
        .IssuedOn(new DateOnly(2026, 9, 1))
        .DueOn(new DateOnly(2026, 10, 1))
        .InCurrency("EUR")
        .WithBuyerReference("PURCHASING")
        .From(seller => seller
            .Named("Seller Ltd")
            .WithVatIdentifier("FR32732829320")
            .WithAddress(address =>
            {
                address.Line1 = "12 rue de la Paix";
                address.City = "Paris";
                address.PostCode = "75002";
                address.CountryCode = "FR";
            }))
        .To(buyer => buyer
            .Named("Buyer SA")
            .WithVatIdentifier("FR89552081317")
            .WithAddress(address =>
            {
                address.Line1 = "3 avenue des Champs";
                address.City = "Lyon";
                address.PostCode = "69002";
                address.CountryCode = "FR";
            }));
}
