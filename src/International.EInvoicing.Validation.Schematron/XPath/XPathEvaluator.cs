using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace International.EInvoicing.Validation.Schematron.XPath;

/// <summary>
/// Where an expression is being evaluated: the context node, its place in the sequence being filtered, and
/// any variables in scope.
/// </summary>
/// <remarks>
/// Position and size exist because <c>position()</c> and <c>last()</c> do. A predicate such as
/// <c>tokenize(...)[last()]</c> means the last item, and an engine that answers 1 to <c>last()</c> silently
/// returns the first — a rule that then passes on the wrong data.
/// </remarks>
internal sealed record XPathContext(
    object Node,
    XDocument Document,
    IReadOnlyDictionary<string, XPathValue> Variables,
    int Position = 1,
    int Size = 1)
{
    public XPathContext With(object node) => this with { Node = node };

    public XPathContext At(object node, int position, int size) =>
        this with { Node = node, Position = position, Size = size };

    public XPathContext With(string variable, XPathValue value)
    {
        var variables = new Dictionary<string, XPathValue>(Variables, StringComparer.Ordinal)
        {
            [variable] = value,
        };

        return this with { Variables = variables };
    }
}

/// <summary>
/// Evaluates the parsed expression against a document.
/// </summary>
/// <remarks>
/// Comparisons follow XPath's general-comparison rule: a comparison between sequences is true when any pair
/// of items satisfies it. Getting that wrong makes rules pass that should fail, which is the failure mode a
/// validator can least afford.
/// </remarks>
internal sealed class XPathEvaluator(
    IReadOnlyDictionary<string, string> namespaces,
    IReadOnlyDictionary<string, SchematronFunction>? functions = null)
{
    public XPathValue Evaluate(XPathNode node, XPathContext context) => node switch
    {
        LiteralNode literal => literal.Value,
        VariableNode variable => context.Variables.TryGetValue(variable.Name, out XPathValue value)
            ? value
            : throw new XPathException($"${variable.Name} is not in scope."),
        SequenceNode sequence => XPathValue.Nodes([.. sequence.Items.SelectMany(item => Evaluate(item, context).Items)]),
        NegateNode negate => XPathValue.Number(-(Evaluate(negate.Operand, context).AsNumber() ?? 0)),
        FunctionNode function => Call(function, context),
        QuantifiedNode quantified => Quantify(quantified, context),
        ForNode loop => XPathValue.Nodes(
        [
            .. Evaluate(loop.Sequence, context).Items.SelectMany(item =>
                Evaluate(loop.Body, context.With(loop.Variable, XPathValue.Nodes([item]))).Items),
        ]),
        ConditionalNode conditional => Evaluate(
            Evaluate(conditional.Condition, context).AsBoolean() ? conditional.Then : conditional.Else,
            context),
        BinaryNode binary => Binary(binary, context),
        PathNode path => EvaluatePath(path, context),
        _ => throw new XPathException($"Cannot evaluate {node.GetType().Name}."),
    };

    private XPathValue Quantify(QuantifiedNode node, XPathContext context)
    {
        IReadOnlyList<object> items = Evaluate(node.Sequence, context).Items;

        foreach (object item in items)
        {
            bool satisfied = Evaluate(node.Test, context.With(node.Variable, XPathValue.Nodes([item]))).AsBoolean();

            if (node.Every && !satisfied)
            {
                return XPathValue.Boolean(false);
            }

            if (!node.Every && satisfied)
            {
                return XPathValue.Boolean(true);
            }
        }

        return XPathValue.Boolean(node.Every);
    }

    private XPathValue Binary(BinaryNode node, XPathContext context)
    {
        switch (node.Operator)
        {
            case "and":
                return XPathValue.Boolean(
                    Evaluate(node.Left, context).AsBoolean() && Evaluate(node.Right, context).AsBoolean());

            case "or":
                return XPathValue.Boolean(
                    Evaluate(node.Left, context).AsBoolean() || Evaluate(node.Right, context).AsBoolean());

            case "|":
                return XPathValue.Nodes(
                    [.. Evaluate(node.Left, context).Items, .. Evaluate(node.Right, context).Items]);
        }

        XPathValue left = Evaluate(node.Left, context);
        XPathValue right = Evaluate(node.Right, context);

        return node.Operator switch
        {
            "+" or "-" or "*" or "div" or "idiv" or "mod" => Arithmetic(node.Operator, left, right),
            "to" => Range(left, right),
            _ => XPathValue.Boolean(Compare(node.Operator, left, right)),
        };
    }

    /// <summary>Every whole number from one bound to the other, as XPath's <c>to</c> produces it.</summary>
    private static XPathValue Range(XPathValue left, XPathValue right)
    {
        if (left.AsNumber() is not { } first || right.AsNumber() is not { } last)
        {
            return XPathValue.Empty;
        }

        var items = new List<object>();

        for (decimal value = Math.Truncate(first); value <= Math.Truncate(last); value++)
        {
            items.Add(value);
        }

        return XPathValue.Nodes(items);
    }

    private static XPathValue Arithmetic(string op, XPathValue left, XPathValue right)
    {
        decimal? a = left.AsNumber();
        decimal? b = right.AsNumber();

        if (a is not { } first || b is not { } second)
        {
            return XPathValue.Empty;
        }

        return op switch
        {
            "+" => XPathValue.Number(first + second),
            "-" => XPathValue.Number(first - second),
            "*" => XPathValue.Number(first * second),
            "div" => second == 0 ? XPathValue.Empty : XPathValue.Number(first / second),
            "idiv" => second == 0 ? XPathValue.Empty : XPathValue.Number(decimal.Truncate(first / second)),
            "mod" => second == 0 ? XPathValue.Empty : XPathValue.Number(first % second),
            _ => throw new XPathException($"Unknown operator '{op}'."),
        };
    }

    /// <summary>
    /// A general comparison is true when <em>any</em> pair of items satisfies it; a value comparison
    /// (<c>eq</c>, <c>lt</c> and the rest) compares single values and is false when either side is empty.
    /// </summary>
    private static bool Compare(string op, XPathValue left, XPathValue right)
    {
        bool valueComparison = op is "eq" or "ne" or "lt" or "le" or "gt" or "ge";
        string normalised = op switch
        {
            "eq" => "=",
            "ne" => "!=",
            "lt" => "<",
            "le" => "<=",
            "gt" => ">",
            "ge" => ">=",
            _ => op,
        };

        if (valueComparison)
        {
            return left.IsEmpty || right.IsEmpty ? false : Matches(normalised, left, right, single: true);
        }

        return Matches(normalised, left, right, single: false);
    }

    private static bool Matches(string op, XPathValue left, XPathValue right, bool single)
    {
        List<decimal> leftNumbers = [.. left.AllNumbers()];
        List<decimal> rightNumbers = [.. right.AllNumbers()];

        if (leftNumbers.Count > 0 && rightNumbers.Count > 0)
        {
            IEnumerable<(decimal A, decimal B)> pairs = single
                ? [(leftNumbers[0], rightNumbers[0])]
                : leftNumbers.SelectMany(a => rightNumbers.Select(b => (a, b)));

            if (pairs.Any(pair => CompareNumbers(op, pair.A, pair.B)))
            {
                return true;
            }

            if (op != "=" && op != "!=")
            {
                return false;
            }
        }

        List<string> leftText = [.. left.AllText()];
        List<string> rightText = [.. right.AllText()];

        if (leftText.Count == 0 || rightText.Count == 0)
        {
            return false;
        }

        IEnumerable<(string A, string B)> textPairs = single
            ? [(leftText[0], rightText[0])]
            : leftText.SelectMany(a => rightText.Select(b => (a, b)));

        return textPairs.Any(pair => CompareText(op, pair.A, pair.B));
    }

    private static bool CompareNumbers(string op, decimal a, decimal b) => op switch
    {
        "=" => a == b,
        "!=" => a != b,
        "<" => a < b,
        "<=" => a <= b,
        ">" => a > b,
        ">=" => a >= b,
        _ => false,
    };

    private static bool CompareText(string op, string a, string b) => op switch
    {
        "=" => string.Equals(a, b, StringComparison.Ordinal),
        "!=" => !string.Equals(a, b, StringComparison.Ordinal),
        _ => CompareMoments(op, a, b),
    };

    /// <summary>
    /// Ordering two values that are not numbers. The rule sets do this to dates — an invoicing period ends
    /// after it starts — so two timestamps are compared chronologically and anything else is false, as an
    /// ordering comparison on plain text is in XPath 1.0.
    /// </summary>
    private static bool CompareMoments(string op, string a, string b)
    {
        const DateTimeStyles styles = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;

        if (!DateTimeOffset.TryParse(a, CultureInfo.InvariantCulture, styles, out DateTimeOffset first)
            || !DateTimeOffset.TryParse(b, CultureInfo.InvariantCulture, styles, out DateTimeOffset second))
        {
            return false;
        }

        int order = first.CompareTo(second);

        return op switch
        {
            "<" => order < 0,
            "<=" => order <= 0,
            ">" => order > 0,
            ">=" => order >= 0,
            _ => false,
        };
    }

    private XPathValue EvaluatePath(PathNode node, XPathContext context)
    {
        IReadOnlyList<object> current;

        if (node.Absolute)
        {
            current = [context.Document];
        }
        else if (node.Start is not null)
        {
            current = Evaluate(node.Start, context).Items;
        }
        else
        {
            current = [context.Node];
        }

        foreach (StepNode step in node.Steps)
        {
            current = ApplyStep(step, current, context);
        }

        return XPathValue.Nodes(current);
    }

    private IReadOnlyList<object> ApplyStep(StepNode step, IReadOnlyList<object> nodes, XPathContext context)
    {
        var results = new List<object>();

        foreach (object node in nodes)
        {
            IEnumerable<object> candidates = step.DescendantOrSelf
                ? SelectFrom(step, DescendantsAndSelf(node), context)
                : SelectFrom(step, [node], context);

            results.AddRange(candidates);
        }

        return Filter(step.Predicates, results, context);
    }

    private IEnumerable<object> SelectFrom(StepNode step, IEnumerable<object> nodes, XPathContext context)
    {
        foreach (object node in nodes)
        {
            if (step.Expression is not null)
            {
                foreach (object item in Evaluate(step.Expression, context.With(node)).Items)
                {
                    yield return item;
                }

                continue;
            }

            foreach (object item in Axis(step, node))
            {
                yield return item;
            }
        }
    }

    private IEnumerable<object> Axis(StepNode step, object node) => step switch
    {
        { Name: "text()" } => TextOf(node),
        _ => OnAxis(step, node),
    };

    /// <summary>The text a node carries, as XPath's <c>text()</c> selects it.</summary>
    private static IEnumerable<object> TextOf(object node) =>
        node is XElement element && !element.HasElements && element.Value.Length > 0 ? [element.Value] : [];

    private IEnumerable<object> OnAxis(StepNode step, object node) => step.Axis switch
    {
        StepAxis.Self => [node],
        StepAxis.Parent => Parent(node) is { } parent ? [parent] : [],
        StepAxis.Attribute => node is XElement element
            ? element.Attributes().Where(a => NameMatches(step.Name, a.Name))
            : [],
        StepAxis.Descendant => Descendants(node).Where(child => NameMatches(step.Name, NameOf(child))),
        StepAxis.Ancestor => Ancestors(node).Where(ancestor => NameMatches(step.Name, NameOf(ancestor))),
        StepAxis.Preceding => Preceding(node).Where(other => NameMatches(step.Name, NameOf(other))),
        StepAxis.PrecedingSibling => Siblings(node, before: true).Where(other => NameMatches(step.Name, NameOf(other))),
        StepAxis.FollowingSibling => Siblings(node, before: false).Where(other => NameMatches(step.Name, NameOf(other))),
        StepAxis.Following => Following(node).Where(other => NameMatches(step.Name, NameOf(other))),
        _ => Children(node).Where(child => NameMatches(step.Name, NameOf(child))),
    };

    private static IEnumerable<object> Children(object node) => node switch
    {
        XDocument document => document.Elements(),
        XElement element => element.Elements(),
        _ => [],
    };

    private static IEnumerable<object> Descendants(object node) => node switch
    {
        XDocument document => document.Descendants(),
        XElement element => element.Descendants(),
        _ => [],
    };

    private static IEnumerable<object> DescendantsAndSelf(object node) => node switch
    {
        XDocument document => [document, .. document.Descendants()],
        XElement element => [element, .. element.Descendants()],
        _ => [node],
    };

    private static IEnumerable<object> Ancestors(object node)
    {
        for (object? current = Parent(node); current is not null; current = Parent(current))
        {
            yield return current;
        }
    }

    /// <summary>
    /// Everything before this node in document order, excluding its own ancestors, as XPath defines the
    /// preceding axis.
    /// </summary>
    private static IEnumerable<object> Preceding(object node)
    {
        if (node is not XElement element)
        {
            yield break;
        }

        var ancestors = new HashSet<object>(element.Ancestors());

        foreach (XElement other in element.Document?.Descendants() ?? [])
        {
            if (ReferenceEquals(other, element))
            {
                yield break;
            }

            if (!ancestors.Contains(other))
            {
                yield return other;
            }
        }
    }

    private static IEnumerable<object> Siblings(object node, bool before)
    {
        if (node is not XElement element)
        {
            yield break;
        }

        IEnumerable<XElement> siblings = before
            ? element.ElementsBeforeSelf()
            : element.ElementsAfterSelf();

        foreach (XElement sibling in siblings)
        {
            yield return sibling;
        }
    }

    /// <summary>Everything after this node in document order, excluding what it contains.</summary>
    private static IEnumerable<object> Following(object node)
    {
        if (node is not XElement element)
        {
            yield break;
        }

        var descendants = new HashSet<object>(element.Descendants());
        bool passed = false;

        foreach (XElement other in element.Document?.Descendants() ?? [])
        {
            if (ReferenceEquals(other, element))
            {
                passed = true;
                continue;
            }

            if (passed && !descendants.Contains(other))
            {
                yield return other;
            }
        }
    }

    private static object? Parent(object node) => node switch
    {
        XElement element => (object?)element.Parent ?? element.Document,
        XAttribute attribute => attribute.Parent,
        _ => null,
    };

    private static XName? NameOf(object node) => node switch
    {
        XElement element => element.Name,
        XAttribute attribute => attribute.Name,
        _ => null,
    };

    private bool NameMatches(string? test, XName? name)
    {
        if (test is null or "*")
        {
            return name is not null;
        }

        if (name is null)
        {
            return false;
        }

        int colon = test.IndexOf(':', StringComparison.Ordinal);
        if (colon < 0)
        {
            return string.Equals(name.LocalName, test, StringComparison.Ordinal)
                && name.NamespaceName.Length == 0;
        }

        string prefix = test[..colon];
        string local = test[(colon + 1)..];

        if (!namespaces.TryGetValue(prefix, out string? uri))
        {
            return false;
        }

        return string.Equals(name.NamespaceName, uri, StringComparison.Ordinal)
            && (local == "*" || string.Equals(name.LocalName, local, StringComparison.Ordinal));
    }

    private IReadOnlyList<object> Filter(
        IReadOnlyList<XPathNode> predicates,
        IReadOnlyList<object> nodes,
        XPathContext context)
    {
        IReadOnlyList<object> current = nodes;

        foreach (XPathNode predicate in predicates)
        {
            var kept = new List<object>();
            for (int index = 0; index < current.Count; index++)
            {
                XPathValue result = Evaluate(predicate, context.At(current[index], index + 1, current.Count));

                bool keep = result.AsNumber() is { } position && result.Items is [decimal]
                    ? position == index + 1
                    : result.AsBoolean();

                if (keep)
                {
                    kept.Add(current[index]);
                }
            }

            current = kept;
        }

        return current;
    }

    /// <summary>
    /// Functions this engine implements in place of the rule set's own definition, and why: the IBAN check
    /// expands an account number to a 34-digit integer, which needs arbitrary precision.
    /// </summary>
    private static readonly HashSet<string> EngineImplemented = new(StringComparer.Ordinal) { "checkIBAN" };

    /// <summary>A function's name without its prefix, except for the <c>xs:</c> casts the prefix defines.</summary>
    private static string LocalFunctionName(string name) =>
        name.Contains(':', StringComparison.Ordinal) && !name.StartsWith("xs:", StringComparison.Ordinal)
            ? name[(name.IndexOf(':', StringComparison.Ordinal) + 1)..]
            : name;

    private XPathValue Call(FunctionNode node, XPathContext context)
    {
        List<XPathValue> arguments = [.. node.Arguments.Select(argument => Evaluate(argument, context))];

        // A function the rule set defines for itself wins: it is what the artefact means by that name. The
        // exception is a function this engine implements deliberately — the artefact's IBAN check expands an
        // account number to a 34-digit integer, which needs arbitrary precision this evaluator does not have.
        if (functions?.TryGetValue(node.Name, out SchematronFunction? declared) == true
            && !EngineImplemented.Contains(LocalFunctionName(node.Name)))
        {
            return CallDeclared(declared, arguments, context);
        }

        XPathValue First() => arguments.Count > 0 ? arguments[0] : XPathValue.Nodes([context.Node]);

        string name = LocalFunctionName(node.Name);

        return name switch
        {
            "not" => XPathValue.Boolean(!First().AsBoolean()),
            "true" => XPathValue.Boolean(true),
            "false" => XPathValue.Boolean(false),
            "boolean" => XPathValue.Boolean(First().AsBoolean()),
            "exists" => XPathValue.Boolean(!First().IsEmpty),
            "empty" => XPathValue.Boolean(First().IsEmpty),
            "count" => XPathValue.Number(First().Items.Count),
            "sum" => XPathValue.Number(First().AllNumbers().Sum()),
            "string" or "xs:string" => XPathValue.Text(First().AsText()),
            "number" or "xs:decimal" or "xs:double" or "xs:integer" or "xs:float" =>
                First().AsNumber() is { } value ? XPathValue.Number(value) : XPathValue.Empty,
            "castable-as" => XPathValue.Boolean(IsCastable(
                First(),
                arguments.Count > 1 ? arguments[1].AsText() : "xs:decimal")),
            "xs:date" or "xs:dateTime" => XPathValue.Text(First().AsText()),
            "normalize-space" => XPathValue.Text(NormalizeSpace(First().AsText())),
            "upper-case" => XPathValue.Text(First().AsText().ToUpperInvariant()),
            // CA1308 asks for upper-casing; this is XPath's lower-case() function, which lower-cases.
#pragma warning disable CA1308
            "lower-case" => XPathValue.Text(First().AsText().ToLowerInvariant()),
#pragma warning restore CA1308
            "string-length" => XPathValue.Number(First().AsText().Length),
            "concat" => XPathValue.Text(string.Concat(arguments.Select(a => a.AsText()))),
            "contains" => XPathValue.Boolean(arguments[0].AsText().Contains(arguments[1].AsText(), StringComparison.Ordinal)),
            "starts-with" => XPathValue.Boolean(arguments[0].AsText().StartsWith(arguments[1].AsText(), StringComparison.Ordinal)),
            "ends-with" => XPathValue.Boolean(arguments[0].AsText().EndsWith(arguments[1].AsText(), StringComparison.Ordinal)),
            "substring-before" => XPathValue.Text(SubstringBefore(arguments[0].AsText(), arguments[1].AsText())),
            "substring-after" => XPathValue.Text(SubstringAfter(arguments[0].AsText(), arguments[1].AsText())),
            "substring" => XPathValue.Text(Substring(arguments)),
            "matches" => XPathValue.Boolean(Regex.IsMatch(
                arguments[0].AsText(),
                arguments[1].AsText(),
                RegexOptions.None,
                TimeSpan.FromSeconds(1))),
            "replace" => XPathValue.Text(Regex.Replace(
                arguments[0].AsText(),
                arguments[1].AsText(),
                // XPath writes group references as $1; .NET reads $1 the same way, but a literal $ must not
                // be taken for one.
                arguments[2].AsText(),
                RegexOptions.None,
                TimeSpan.FromSeconds(1))),
            "string-join" => XPathValue.Text(string.Join(
                arguments.Count > 1 ? arguments[1].AsText() : string.Empty,
                arguments[0].AllText())),
            "distinct-values" => XPathValue.Nodes([.. First().AllText().Distinct(StringComparer.Ordinal).Cast<object>()]),
            "checkIBAN" => XPathValue.Boolean(International.EInvoicing.Identifiers.CheckDigit.IsIban(First().AsText())),
            "abs" => XPathValue.Number(Math.Abs(First().AsNumber() ?? 0)),
            "round" => XPathValue.Number(Math.Round(First().AsNumber() ?? 0, MidpointRounding.AwayFromZero)),
            "floor" => XPathValue.Number(Math.Floor(First().AsNumber() ?? 0)),
            "ceiling" => XPathValue.Number(Math.Ceiling(First().AsNumber() ?? 0)),
            "name" => XPathValue.Text(QualifiedNameOf(First())),
            "local-name" => XPathValue.Text(NameOf(FirstItem(First()))?.LocalName ?? string.Empty),
            "namespace-uri" => XPathValue.Text(NameOf(FirstItem(First()))?.NamespaceName ?? string.Empty),
            "position" => XPathValue.Number(context.Position),
            "last" => XPathValue.Number(context.Size),
            "translate" => XPathValue.Text(Translate(arguments[0].AsText(), arguments[1].AsText(), arguments[2].AsText())),
            "reverse" => XPathValue.Nodes([.. First().Items.Reverse()]),
            "string-to-codepoints" => XPathValue.Nodes(
                [.. First().AsText().Select(character => (object)(decimal)character)]),
            "tokenize" => XPathValue.Nodes([.. Regex
                .Split(arguments[0].AsText(), arguments[1].AsText(), RegexOptions.None, TimeSpan.FromSeconds(1))
                .Cast<object>()]),
            _ => throw new XPathException($"Function '{node.Name}' is not supported."),
        };
    }

    /// <summary>Whether a value could be cast to a type, which is a different question per type.</summary>
    private static bool IsCastable(XPathValue value, string type)
    {
        if (value.IsEmpty)
        {
            return false;
        }

        string text = value.AsText().Trim();

        return type switch
        {
            "xs:date" => DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
            "xs:dateTime" => DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _),
            "xs:time" => TimeOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
            "xs:integer" => long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
            "xs:string" => true,
            _ => value.AsNumber() is not null,
        };
    }

    /// <summary>
    /// XPath's <c>translate</c>: each character of <paramref name="from"/> is replaced by the one at the same
    /// position in <paramref name="to"/>, or dropped when there is none.
    /// </summary>
    private static string Translate(string value, string from, string to)
    {
        var result = new System.Text.StringBuilder(value.Length);

        foreach (char character in value)
        {
            int index = from.IndexOf(character, StringComparison.Ordinal);

            if (index < 0)
            {
                result.Append(character);
            }
            else if (index < to.Length)
            {
                result.Append(to[index]);
            }
        }

        return result.ToString();
    }

    private static object FirstItem(XPathValue value)
    {
        IReadOnlyList<object> items = value.Items;
        return items.Count > 0 ? items[0] : string.Empty;
    }

    /// <summary>Binds the arguments, evaluates the function's own variables in order, then its body.</summary>
    private XPathValue CallDeclared(
        SchematronFunction function,
        List<XPathValue> arguments,
        XPathContext context)
    {
        // The rule set's own variables stay in scope: a declared function reads them, as the German IBAN
        // check reads the pattern its rule set declares once.
        var scope = new Dictionary<string, XPathValue>(context.Variables, StringComparer.Ordinal);

        for (int index = 0; index < function.Parameters.Count; index++)
        {
            scope[function.Parameters[index]] = index < arguments.Count ? arguments[index] : XPathValue.Empty;
        }

        XPathContext inner = context with { Variables = scope };

        foreach (SchematronVariable variable in function.Variables)
        {
            scope[variable.Name] = Evaluate(variable.Expression, inner);
            inner = inner with { Variables = scope };
        }

        return Evaluate(function.Body, inner);
    }

    private string QualifiedNameOf(XPathValue value)
    {
        XName? name = NameOf(FirstItem(value));
        if (name is null)
        {
            return string.Empty;
        }

        KeyValuePair<string, string> prefix = namespaces
            .FirstOrDefault(pair => string.Equals(pair.Value, name.NamespaceName, StringComparison.Ordinal));

        return prefix.Key is null ? name.LocalName : $"{prefix.Key}:{name.LocalName}";
    }

    private static string NormalizeSpace(string value) =>
        string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string SubstringBefore(string value, string separator)
    {
        int index = value.IndexOf(separator, StringComparison.Ordinal);
        return index < 0 ? string.Empty : value[..index];
    }

    private static string SubstringAfter(string value, string separator)
    {
        int index = value.IndexOf(separator, StringComparison.Ordinal);
        return index < 0 ? string.Empty : value[(index + separator.Length)..];
    }

    /// <summary>
    /// XPath's <c>substring</c>: the characters whose one-based position falls in the window that starts at
    /// <c>start</c> and runs for <c>length</c>.
    /// </summary>
    /// <remarks>
    /// The window, not an offset and a count: a start below one shortens what the length reaches, which is
    /// how <c>substring($value, 0, $n)</c> takes the first <c>n - 1</c> characters. The Peppol check-digit
    /// functions are written that way.
    /// </remarks>
    private static string Substring(List<XPathValue> arguments)
    {
        string text = arguments[0].AsText();
        decimal start = Math.Round(arguments[1].AsNumber() ?? 1, MidpointRounding.AwayFromZero);
        decimal end = arguments.Count < 3
            ? decimal.MaxValue
            : start + Math.Round(arguments[2].AsNumber() ?? 0, MidpointRounding.AwayFromZero);

        var result = new System.Text.StringBuilder(text.Length);

        for (int index = 0; index < text.Length; index++)
        {
            decimal position = index + 1;

            if (position >= start && position < end)
            {
                result.Append(text[index]);
            }
        }

        return result.ToString();
    }
}
