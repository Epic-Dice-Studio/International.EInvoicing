namespace International.EInvoicing.Validation.Schematron.XPath;

/// <summary>A parsed expression. The shapes here are the ones the published rule sets actually use.</summary>
internal abstract record XPathNode;

internal sealed record LiteralNode(XPathValue Value) : XPathNode;

internal sealed record VariableNode(string Name) : XPathNode;

/// <summary>A binary operation: comparison, arithmetic, boolean, or the node-set union.</summary>
internal sealed record BinaryNode(string Operator, XPathNode Left, XPathNode Right) : XPathNode;

internal sealed record NegateNode(XPathNode Operand) : XPathNode;

internal sealed record FunctionNode(string Name, IReadOnlyList<XPathNode> Arguments) : XPathNode;

/// <summary>
/// A parenthesised sequence, <c>('VA', 'FC')</c>. Comparing against one is how the artefacts express
/// membership of a small set.
/// </summary>
internal sealed record SequenceNode(IReadOnlyList<XPathNode> Items) : XPathNode;

/// <summary>
/// <c>every $v in sequence satisfies test</c>. XPath 1.0 has no equivalent, and the EN 16931 rules on VAT
/// breakdowns are written with it, so it is parsed rather than rewritten.
/// </summary>
internal sealed record QuantifiedNode(bool Every, string Variable, XPathNode Sequence, XPathNode Test) : XPathNode;

/// <summary>
/// <c>for $v in sequence return result</c>: the result of the body for every item, concatenated. The French
/// rules use it to compare a computed total against each line.
/// </summary>
internal sealed record ForNode(string Variable, XPathNode Sequence, XPathNode Body) : XPathNode;

/// <summary><c>if (condition) then a else b</c>, which XPath 2.0 adds and the German rules use.</summary>
internal sealed record ConditionalNode(XPathNode Condition, XPathNode Then, XPathNode Else) : XPathNode;

/// <summary>A path: an optional root, then steps. <c>Absolute</c> means it starts from the document.</summary>
internal sealed record PathNode(XPathNode? Start, IReadOnlyList<StepNode> Steps, bool Absolute) : XPathNode;

/// <summary>
/// One step of a path. A step is a node test, or a function call — XPath 2.0 allows
/// <c>ram:RateApplicablePercent/xs:decimal(.)</c>, and the CII rules rely on it.
/// </summary>
/// <remarks>
/// <c>FiltersSequence</c> says whether the predicates filter the incoming sequence as a whole rather than
/// each node's own step result. That is the difference between <c>$digits[$i]</c>, which picks one item out
/// of a sequence, and <c>a/b[1]</c>, which picks the first <c>b</c> of every <c>a</c>. Both appear in the
/// published rules.
/// </remarks>
internal sealed record StepNode(
    StepAxis Axis,
    string? Name,
    XPathNode? Expression,
    IReadOnlyList<XPathNode> Predicates,
    bool DescendantOrSelf = false,
    bool FiltersSequence = false);

internal enum StepAxis
{
    Child,
    Attribute,
    Self,
    Parent,
    Descendant,

    /// <summary>Every ancestor. The artefacts use it to exclude a rule inside a particular subtree.</summary>
    Ancestor,

    /// <summary>Every ancestor, and the node itself. Dropping the node is a different question entirely.</summary>
    AncestorOrSelf,

    /// <summary>Every descendant, and the node itself.</summary>
    DescendantOrSelf,

    /// <summary>Every node before this one in document order, excluding its ancestors.</summary>
    Preceding,

    /// <summary>The siblings before this node.</summary>
    PrecedingSibling,

    /// <summary>The siblings after it.</summary>
    FollowingSibling,

    /// <summary>Every node after this one in document order, excluding its descendants.</summary>
    Following,
}
