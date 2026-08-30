namespace International.EInvoicing.Validation.Schematron.XPath;

/// <summary>
/// Parses the XPath subset the published rule sets use.
/// </summary>
/// <remarks>
/// Not a complete XPath 2.0 parser, and deliberately so: it covers what the EN 16931, Peppol and XRechnung
/// artefacts are written in, measured rather than assumed. An expression it cannot parse raises rather than
/// being silently skipped, because a rule that quietly does not run is worse than one that fails loudly.
/// </remarks>
internal sealed class XPathParser
{
    private readonly List<XPathToken> _tokens;
    private int _position;

    private XPathParser(string expression) => _tokens = new XPathLexer(expression).Tokenise();

    public static XPathNode Parse(string expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var parser = new XPathParser(expression);
        XPathNode node = parser.ParseExpression();
        parser.Expect(XPathTokenKind.End);
        return node;
    }

    private XPathToken Current => _tokens[_position];

    private XPathNode ParseExpression()
    {
        // A conditional binds looser than anything else, so it is recognised before the operator ladder.
        if (Current.IsName("if") && _position + 1 < _tokens.Count && _tokens[_position + 1].Is("("))
        {
            return ParseConditional();
        }

        return ParseOr();
    }

    private ConditionalNode ParseConditional()
    {
        _position++;
        Expect("(");
        XPathNode condition = ParseExpression();
        Expect(")");

        if (!Current.IsName("then"))
        {
            throw new XPathException($"Expected 'then', found {Current}.");
        }

        _position++;
        XPathNode whenTrue = ParseExpression();

        if (!Current.IsName("else"))
        {
            throw new XPathException($"Expected 'else', found {Current}.");
        }

        _position++;
        return new ConditionalNode(condition, whenTrue, ParseExpression());
    }

    private XPathNode ParseOr()
    {
        XPathNode left = ParseAnd();
        while (Current.IsName("or"))
        {
            _position++;
            left = new BinaryNode("or", left, ParseAnd());
        }

        return left;
    }

    private XPathNode ParseAnd()
    {
        XPathNode left = ParseComparison();
        while (Current.IsName("and"))
        {
            _position++;
            left = new BinaryNode("and", left, ParseComparison());
        }

        return left;
    }

    /// <summary>
    /// Comparisons, both the general operators of XPath 1.0 and the value operators <c>eq</c>, <c>ne</c>,
    /// <c>lt</c>, <c>le</c>, <c>gt</c>, <c>ge</c> that XPath 2.0 adds and the artefacts use.
    /// </summary>
    private XPathNode ParseComparison()
    {
        XPathNode left = ParseAdditive();

        while (true)
        {
            string? op = Current switch
            {
                { Kind: XPathTokenKind.Operator, Text: "=" or "!=" or "<" or ">" or "<=" or ">=" } => Current.Text,
                { Kind: XPathTokenKind.Name, Text: "eq" or "ne" or "lt" or "le" or "gt" or "ge" } => Current.Text,
                _ => null,
            };

            if (op is null)
            {
                return left;
            }

            _position++;
            left = new BinaryNode(op, left, ParseAdditive());
        }
    }

    /// <summary>
    /// <c>cast as</c> and <c>castable as</c>. The artefacts use them to force a decimal comparison, which is
    /// what this engine does anyway, so the cast is applied as the matching constructor function.
    /// </summary>
    private XPathNode ParseCast(XPathNode operand)
    {
        while (Current.IsName("cast") || Current.IsName("castable"))
        {
            bool castable = Current.IsName("castable");
            _position++;

            if (!Current.IsName("as"))
            {
                throw new XPathException($"Expected 'as' after cast, found {Current}.");
            }

            _position++;
            string type = Current.Text;
            _position++;

            if (Current.Is("?"))
            {
                _position++;
            }

            operand = new FunctionNode(castable ? "castable-as" : type, [operand]);
        }

        return operand;
    }

    private XPathNode ParseAdditive()
    {
        XPathNode left = ParseCast(ParseMultiplicative());
        while (Current.Is("+") || Current.Is("-"))
        {
            string op = Current.Text;
            _position++;
            left = new BinaryNode(op, left, ParseCast(ParseMultiplicative()));
        }

        return left;
    }

    private XPathNode ParseMultiplicative()
    {
        XPathNode left = ParseUnary();
        while (Current.Is("*") || Current.IsName("div") || Current.IsName("idiv") || Current.IsName("mod"))
        {
            string op = Current.Is("*") ? "*" : Current.Text;
            _position++;
            left = new BinaryNode(op, left, ParseUnary());
        }

        return left;
    }

    private XPathNode ParseUnary()
    {
        if (!Current.Is("-"))
        {
            return ParseUnion();
        }

        _position++;
        return new NegateNode(ParseUnary());
    }

    private XPathNode ParseUnion()
    {
        XPathNode left = ParsePath();
        while (Current.Is("|"))
        {
            _position++;
            left = new BinaryNode("|", left, ParsePath());
        }

        return left;
    }

    private XPathNode ParsePath()
    {
        if (Current.IsName("every") || Current.IsName("some"))
        {
            return ParseQuantified();
        }

        if (Current.Is("/") || Current.Is("//"))
        {
            bool descendant = Current.Is("//");
            _position++;

            if (IsPathEnd())
            {
                return new PathNode(null, [], Absolute: true);
            }

            return new PathNode(null, ReadSteps(descendant), Absolute: true);
        }

        XPathNode? start = ParseStartOfPath();
        if (start is null)
        {
            return new PathNode(null, ReadSteps(descendantOrSelf: false), Absolute: false);
        }

        if (!Current.Is("/") && !Current.Is("//"))
        {
            return start;
        }

        bool descendantStep = Current.Is("//");
        _position++;
        return new PathNode(start, ReadSteps(descendantStep), Absolute: false);
    }

    /// <summary>
    /// What a relative path may begin with other than a step: a literal, a variable, a parenthesised
    /// expression, or a function call. Returns <c>null</c> when the path begins with an ordinary step.
    /// </summary>
    private XPathNode? ParseStartOfPath()
    {
        if (Current.Kind is XPathTokenKind.Number or XPathTokenKind.String or XPathTokenKind.Variable)
        {
            return ParsePrimary();
        }

        if (Current.Is("("))
        {
            _position++;
            XPathNode inner = ParseExpression();

            if (Current.Is(","))
            {
                var items = new List<XPathNode> { inner };
                while (Current.Is(","))
                {
                    _position++;
                    items.Add(ParseExpression());
                }

                Expect(")");
                inner = new SequenceNode(items);
            }
            else
            {
                Expect(")");
            }

            return Filtered(inner);
        }

        if (Current.Kind == XPathTokenKind.Name
            && _position + 1 < _tokens.Count
            && _tokens[_position + 1].Is("(")
            && !IsNodeTest(Current.Text))
        {
            string name = Current.Text;
            _position++;
            return Filtered(ParseFunctionCall(name));
        }

        return null;
    }

    /// <summary>
    /// A predicate may follow a parenthesised expression or a function call — the German rules filter a
    /// sequence that way, as in <c>(ram:PersonName, ram:DepartmentName)[normalize-space(.)]</c>.
    /// </summary>
    private XPathNode Filtered(XPathNode expression)
    {
        if (!Current.Is("["))
        {
            return expression;
        }

        return new PathNode(
            expression,
            [new StepNode(StepAxis.Self, null, null, ReadPredicates())],
            Absolute: false);
    }

    /// <summary>
    /// A node test is written like a call — <c>node()</c>, <c>text()</c> — so its parentheses are consumed
    /// here. <c>node()</c> matches anything, which is what the wildcard means to the evaluator.
    /// </summary>
    private string ConsumeNodeTest(string name)
    {
        if (!IsNodeTest(name) || !Current.Is("("))
        {
            return name;
        }

        _position++;
        while (!Current.Is(")") && Current.Kind != XPathTokenKind.End)
        {
            _position++;
        }

        Expect(")");
        return "*";
    }

    /// <summary>Names that look like function calls but are node tests, so a step must claim them.</summary>
    private static bool IsNodeTest(string name) =>
        name is "node" or "text" or "comment" or "processing-instruction" or "element" or "attribute";

    private List<StepNode> ReadSteps(bool descendantOrSelf)
    {
        var steps = new List<StepNode> { ParseStep(descendantOrSelf) };

        while (Current.Is("/") || Current.Is("//"))
        {
            bool descendant = Current.Is("//");
            _position++;
            steps.Add(ParseStep(descendant));
        }

        return steps;
    }

    private QuantifiedNode ParseQuantified()
    {
        bool every = Current.IsName("every");
        _position++;

        if (Current.Kind != XPathTokenKind.Variable)
        {
            throw new XPathException($"Expected a variable after '{(every ? "every" : "some")}', found {Current}.");
        }

        string variable = Current.Text;
        _position++;

        if (!Current.IsName("in"))
        {
            throw new XPathException($"Expected 'in' after ${variable}, found {Current}.");
        }

        _position++;
        XPathNode sequence = ParseExpression();

        if (!Current.IsName("satisfies"))
        {
            throw new XPathException($"Expected 'satisfies', found {Current}.");
        }

        _position++;
        return new QuantifiedNode(every, variable, sequence, ParseExpression());
    }

    private StepNode ParseStep(bool descendantOrSelf)
    {
        StepAxis axis = StepAxis.Child;

        if (Current.Is("("))
        {
            _position++;
            XPathNode inner = ParseExpression();
            Expect(")");
            return new StepNode(StepAxis.Self, null, inner, ReadPredicates(), descendantOrSelf);
        }

        if (Current.Is("@"))
        {
            axis = StepAxis.Attribute;
            _position++;
        }
        else if (Current.Is(".."))
        {
            _position++;
            return new StepNode(StepAxis.Parent, null, null, ReadPredicates(), descendantOrSelf);
        }
        else if (Current.Is("."))
        {
            _position++;
            return new StepNode(StepAxis.Self, null, null, ReadPredicates(), descendantOrSelf);
        }

        string name;
        if (Current.Is("*"))
        {
            name = "*";
            _position++;
        }
        else if (Current.Kind == XPathTokenKind.Name)
        {
            name = Current.Text;
            _position++;

            if (Current.Is("::"))
            {
                axis = name switch
                {
                    "attribute" => StepAxis.Attribute,
                    "child" => StepAxis.Child,
                    "self" => StepAxis.Self,
                    "parent" => StepAxis.Parent,
                    "descendant" or "descendant-or-self" => StepAxis.Descendant,
                    "ancestor" or "ancestor-or-self" => StepAxis.Ancestor,
                    "preceding" => StepAxis.Preceding,
                    "preceding-sibling" => StepAxis.PrecedingSibling,
                    "following" => StepAxis.Following,
                    "following-sibling" => StepAxis.FollowingSibling,
                    _ => throw new XPathException($"Axis '{name}' is not supported."),
                };

                _position++;
                name = Current.Is("*") ? "*" : Current.Text;
                _position++;

                name = ConsumeNodeTest(name);
            }
            else if (IsNodeTest(name) && Current.Is("("))
            {
                name = ConsumeNodeTest(name);
            }
            else if (Current.Is("("))
            {
                // A function call standing where a step would: xs:decimal(.) inside a path.
                FunctionNode function = ParseFunctionCall(name);
                return new StepNode(StepAxis.Self, null, function, ReadPredicates(), descendantOrSelf);
            }
        }
        else
        {
            throw new XPathException($"Expected a step, found {Current}.");
        }

        return new StepNode(axis, name, null, ReadPredicates(), descendantOrSelf);
    }

    private FunctionNode ParseFunctionCall(string name)
    {
        Expect("(");
        var arguments = new List<XPathNode>();

        if (!Current.Is(")"))
        {
            arguments.Add(ParseExpression());
            while (Current.Is(","))
            {
                _position++;
                arguments.Add(ParseExpression());
            }
        }

        Expect(")");
        return new FunctionNode(name, arguments);
    }

    private List<XPathNode> ReadPredicates()
    {
        var predicates = new List<XPathNode>();
        while (Current.Is("["))
        {
            _position++;
            predicates.Add(ParseExpression());
            Expect("]");
        }

        return predicates;
    }

    private bool IsPathEnd() =>
        Current.Kind == XPathTokenKind.End
        || Current.Is(")")
        || Current.Is("]")
        || Current.Is(",");

    private XPathNode ParsePrimary()
    {
        switch (Current.Kind)
        {
            case XPathTokenKind.Number:
                {
                    var literal = new LiteralNode(XPathValue.Number(Current.Number));
                    _position++;
                    return literal;
                }

            case XPathTokenKind.String:
                {
                    var literal = new LiteralNode(XPathValue.Text(Current.Text));
                    _position++;
                    return literal;
                }

            case XPathTokenKind.Variable:
                {
                    var variable = new VariableNode(Current.Text);
                    _position++;
                    return variable;
                }

            default:
                throw new XPathException($"Expected a value, found {Current}.");
        }
    }

    private void Expect(string op)
    {
        if (!Current.Is(op))
        {
            throw new XPathException($"Expected '{op}', found {Current}.");
        }

        _position++;
    }

    private void Expect(XPathTokenKind kind)
    {
        if (Current.Kind != kind)
        {
            throw new XPathException($"Expected {kind}, found {Current}.");
        }
    }
}
