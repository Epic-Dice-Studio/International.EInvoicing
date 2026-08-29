using System.Xml;
using International.EInvoicing.Xml;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Core.Tests.Xml;

/// <summary>
/// These tests encode a security promise, not an implementation detail: an invoice arrives from a third
/// party, so the XML plumbing must refuse to dereference anything or to expand anything.
/// </summary>
public class SecureXmlTests
{
    [Fact]
    public void CreateReader_RejectsExternalEntities()
    {
        // Classic XXE: without DtdProcessing.Prohibit this reads /etc/passwd off the host.
        const string xxe = """
            <?xml version="1.0"?>
            <!DOCTYPE invoice [ <!ENTITY leak SYSTEM "file:///etc/passwd"> ]>
            <invoice>&leak;</invoice>
            """;

        using var reader = SecureXml.CreateReader(xxe);

        Should.Throw<XmlException>(() => ReadToEnd(reader));
    }

    [Fact]
    public void CreateReader_RejectsRecursiveEntityExpansion()
    {
        // "Billion laughs": each entity references the previous one ten times.
        const string billionLaughs = """
            <?xml version="1.0"?>
            <!DOCTYPE lolz [
              <!ENTITY lol "lol">
              <!ENTITY lol1 "&lol;&lol;&lol;&lol;&lol;&lol;&lol;&lol;&lol;&lol;">
              <!ENTITY lol2 "&lol1;&lol1;&lol1;&lol1;&lol1;&lol1;&lol1;&lol1;&lol1;&lol1;">
            ]>
            <lolz>&lol2;</lolz>
            """;

        using var reader = SecureXml.CreateReader(billionLaughs);

        Should.Throw<XmlException>(() => ReadToEnd(reader));
    }

    [Fact]
    public void CreateReaderSettings_NeverResolvesExternalResources()
    {
        // XmlResolver is write-only, so the guarantee is asserted through behaviour above (XXE) and
        // through the settings that are observable here.
        XmlReaderSettings settings = SecureXml.CreateReaderSettings();

        settings.DtdProcessing.ShouldBe(DtdProcessing.Prohibit);
        settings.ValidationType.ShouldBe(ValidationType.None);
        settings.MaxCharactersFromEntities.ShouldBeGreaterThan(0);
        settings.MaxCharactersInDocument.ShouldBe(DocumentLimits.Default.MaxDocumentCharacters);
    }

    [Fact]
    public void CreateReader_DoesNotFetchAnExternalDtd()
    {
        const string externalDtd = """
            <?xml version="1.0"?>
            <!DOCTYPE invoice SYSTEM "https://example.invalid/invoice.dtd">
            <invoice />
            """;

        using var reader = SecureXml.CreateReader(externalDtd);

        Should.Throw<XmlException>(() => ReadToEnd(reader));
    }

    [Fact]
    public void CreateReader_EnforcesTheDocumentSizeLimit()
    {
        var limits = new DocumentLimits { MaxDocumentCharacters = 64 };
        string oversized = $"<invoice>{new string('x', 500)}</invoice>";

        using var reader = SecureXml.CreateReader(oversized, limits);

        Should.Throw<XmlException>(() => ReadToEnd(reader));
    }

    [Fact]
    public void CreateReader_PreservesWhitespaceAndComments()
    {
        // The library hands back the raw text of every field, so nothing may be normalised away.
        const string xml = "<invoice>  <!-- note -->  <id> 42 </id>\n</invoice>";

        using var reader = SecureXml.CreateReader(xml);
        var nodeTypes = new List<XmlNodeType>();
        while (reader.Read())
        {
            nodeTypes.Add(reader.NodeType);
        }

        nodeTypes.ShouldContain(XmlNodeType.Comment);
        nodeTypes.ShouldContain(XmlNodeType.Whitespace);
    }

    [Fact]
    public void CreateReader_ReadsAWellFormedDocument()
    {
        using var reader = SecureXml.CreateReader("<invoice><id>FA-2026-001</id></invoice>");

        reader.ReadToFollowing("id").ShouldBeTrue();
        reader.ReadElementContentAsString().ShouldBe("FA-2026-001");
    }

    [Fact]
    public void IsDepthExceeded_FlagsDocumentsNestedBeyondTheLimit()
    {
        var limits = new DocumentLimits { MaxElementDepth = 3 };
        // <a><b><c><d/></c></b></a> — <d> sits at depth 3, its content at depth 4.
        using var reader = SecureXml.CreateReader("<a><b><c><d>x</d></c></b></a>", limits);

        var exceeded = false;
        while (reader.Read())
        {
            exceeded |= SecureXml.IsDepthExceeded(reader, limits);
        }

        exceeded.ShouldBeTrue();
    }

    [Fact]
    public void IsDepthExceeded_IsFalseForARealisticInvoiceDepth()
    {
        DocumentLimits limits = DocumentLimits.Default;
        using var reader = SecureXml.CreateReader("<a><b><c><d>x</d></c></b></a>", limits);

        while (reader.Read())
        {
            SecureXml.IsDepthExceeded(reader, limits).ShouldBeFalse();
        }
    }

    [Fact]
    public void Unlimited_DisablesEveryCheck()
    {
        DocumentLimits limits = DocumentLimits.Unlimited;

        limits.MaxDocumentCharacters.ShouldBe(0);
        limits.MaxElementDepth.ShouldBe(0);
        SecureXml.CreateReaderSettings(limits).MaxCharactersInDocument.ShouldBe(0);
    }

    [Theory]
    [InlineData(null)]
    public void CreateReaderSettings_RejectsNullLimits(DocumentLimits? limits)
        => Should.Throw<ArgumentNullException>(() => SecureXml.CreateReaderSettings(limits!));

    private static void ReadToEnd(XmlReader reader)
    {
        while (reader.Read())
        {
            // Drain the document; hostile input must fail here rather than silently succeed.
        }
    }
}
