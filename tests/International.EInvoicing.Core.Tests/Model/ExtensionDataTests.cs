using International.EInvoicing.Model;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Core.Tests.Model;

public class ExtensionDataTests
{
    private const string AcmeNamespace = "urn:acme:invoice:1p0";

    [Fact]
    public void AnUnmappedElementIsKeptVerbatim()
    {
        var data = new ExtensionData
        {
            new ExtensionElement(
                AcmeNamespace,
                "PurchaseOrderScan",
                "<acme:PurchaseOrderScan>PO-42</acme:PurchaseOrderScan>"),
        };

        data.Count.ShouldBe(1);
        data.IsEmpty.ShouldBeFalse();
        data[0].Xml.ShouldBe("<acme:PurchaseOrderScan>PO-42</acme:PurchaseOrderScan>");
        data[0].QualifiedName.ShouldBe("{urn:acme:invoice:1p0}PurchaseOrderScan");
    }

    [Fact]
    public void ElementsAreFoundByQualifiedName()
    {
        var data = new ExtensionData
        {
            new ExtensionElement(AcmeNamespace, "Ref", "<acme:Ref>1</acme:Ref>"),
            new ExtensionElement(AcmeNamespace, "Ref", "<acme:Ref>2</acme:Ref>"),
            new ExtensionElement(string.Empty, "Note", "<Note>hello</Note>"),
        };

        data.Named(AcmeNamespace, "Ref").Count().ShouldBe(2);
        data.Named(string.Empty, "Note").Single().QualifiedName.ShouldBe("Note");
    }

    [Fact]
    public void AnEmptyBagKeepsNothing()
    {
        var data = new ExtensionData();

        data.IsEmpty.ShouldBeTrue();
        data.ShouldBeEmpty();
    }
}
