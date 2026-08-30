using International.EInvoicing.Xml;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Core.Tests;

/// <summary>
/// Text that reaches a writer comes from an accounting system, not from a specification. Some of it carries
/// characters XML cannot express at all, and the failure that follows names a hexadecimal value rather than
/// a field — a complaint every mature library in this space has had to answer.
/// </summary>
public class XmlCharactersTests
{
    private const char Bell = '\u0007';

    [Fact]
    public void TextWithNothingWrongWithItComesBackUnchanged()
    {
        const string value = "Prestation — 3 × 4 m², facturée à l'unité 😀";

        XmlCharacters.Sanitize(value).ShouldBeSameAs(value);
    }

    [Fact]
    public void NullComesBackAsNull() => XmlCharacters.Sanitize(null).ShouldBeNull();

    [Fact]
    public void ACharacterXmlCannotCarryIsRemoved()
    {
        XmlCharacters.Sanitize($"Ligne 1{Bell} avec cloche").ShouldBe("Ligne 1 avec cloche");
        XmlCharacters.Sanitize($"{Bell}").ShouldBeEmpty();
    }

    /// <summary>Tab, carriage return and line feed are the three control characters XML does allow.</summary>
    [Fact]
    public void TheControlCharactersXmlAllowsAreKept()
    {
        const string value = "Ligne 1\r\n\tLigne 2";

        XmlCharacters.Sanitize(value).ShouldBe(value);
    }

    [Fact]
    public void ACharacterOutsideTheBasicPlaneSurvives()
    {
        const string value = "facturé 𝄞";

        XmlCharacters.Sanitize(value).ShouldBe(value);
    }

    [Fact]
    public void ALoneSurrogateIsRemovedRatherThanLeftToBreakTheDocument()
    {
        XmlCharacters.Sanitize("avant \ud83d apres").ShouldBe("avant  apres");
    }
}
