using International.EInvoicing.Diagnostics;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Core.Tests.Diagnostics;

public class SourceLocationTests
{
    [Fact]
    public void None_IsNotKnown()
    {
        SourceLocation.None.IsKnown.ShouldBeFalse();
        SourceLocation.None.ToString().ShouldBe("unknown location");
    }

    [Theory]
    [InlineData("/Invoice/cbc:ID", 0, 0, "/Invoice/cbc:ID")]
    [InlineData("/Invoice/cbc:ID", 12, 5, "/Invoice/cbc:ID (line 12, position 5)")]
    [InlineData(null, 12, 5, "line 12, position 5")]
    public void ToString_DescribesWhateverIsKnown(string? path, int line, int position, string expected)
        => new SourceLocation(path, line, position).ToString().ShouldBe(expected);
}
