using System.Globalization;

namespace International.EInvoicing.Validation.Schematron.XPath;

internal enum XPathTokenKind
{
    End,
    Name,
    Number,
    String,
    Operator,
    Variable,
}

internal readonly record struct XPathToken(XPathTokenKind Kind, string Text, decimal Number = 0)
{
    public bool Is(string text) => Kind == XPathTokenKind.Operator && string.Equals(Text, text, StringComparison.Ordinal);

    public bool IsName(string text) => Kind == XPathTokenKind.Name && string.Equals(Text, text, StringComparison.Ordinal);

    public override string ToString() => Kind == XPathTokenKind.End ? "end of expression" : Text;
}

/// <summary>
/// Turns an XPath expression into tokens.
/// </summary>
/// <remarks>
/// Numbers are read as <see cref="decimal"/> rather than <see cref="double"/>. The rules this engine exists
/// to run compare invoice totals, and binary floating point is how an engine reports a correct invoice as
/// wrong by a hundredth.
/// </remarks>
internal sealed class XPathLexer(string expression)
{
    private static readonly string[] TwoCharacterOperators =
        ["//", "!=", "<=", ">=", "..", "::"];

    private readonly string _text = expression ?? throw new ArgumentNullException(nameof(expression));
    private int _position;

    public List<XPathToken> Tokenise()
    {
        var tokens = new List<XPathToken>();
        while (true)
        {
            XPathToken token = Next();
            tokens.Add(token);
            if (token.Kind == XPathTokenKind.End)
            {
                return tokens;
            }
        }
    }

    private XPathToken Next()
    {
        SkipWhitespace();
        if (_position >= _text.Length)
        {
            return new XPathToken(XPathTokenKind.End, string.Empty);
        }

        char current = _text[_position];

        if (current is '\'' or '"')
        {
            return ReadString(current);
        }

        if (char.IsDigit(current) || (current == '.' && _position + 1 < _text.Length && char.IsDigit(_text[_position + 1])))
        {
            return ReadNumber();
        }

        if (current == '$')
        {
            _position++;
            return new XPathToken(XPathTokenKind.Variable, ReadName());
        }

        if (IsNameStart(current))
        {
            string name = ReadName();

            // A prefixed wildcard, ram:*, reads as a name ending in a colon followed by a star.
            if (name.EndsWith(':') && _position < _text.Length && _text[_position] == '*')
            {
                _position++;
                return new XPathToken(XPathTokenKind.Name, name + "*");
            }

            return new XPathToken(XPathTokenKind.Name, name);
        }

        // A wildcard prefix, *:schemaLocation — a name in whatever namespace. Only a name test, never
        // multiplication, because an operator cannot be followed by a colon.
        if (current == '*' && _position + 1 < _text.Length && _text[_position + 1] == ':'
            && _position + 2 < _text.Length && IsNameStart(_text[_position + 2]))
        {
            _position += 2;
            return new XPathToken(XPathTokenKind.Name, "*:" + ReadName());
        }

        foreach (string candidate in TwoCharacterOperators)
        {
            if (_position + 1 < _text.Length && _text[_position] == candidate[0] && _text[_position + 1] == candidate[1])
            {
                _position += 2;
                return new XPathToken(XPathTokenKind.Operator, candidate);
            }
        }

        _position++;
        return new XPathToken(XPathTokenKind.Operator, current.ToString());
    }

    private void SkipWhitespace()
    {
        while (_position < _text.Length && char.IsWhiteSpace(_text[_position]))
        {
            _position++;
        }
    }

    private XPathToken ReadString(char quote)
    {
        _position++;
        int start = _position;
        while (_position < _text.Length && _text[_position] != quote)
        {
            _position++;
        }

        string value = _text[start.._position];
        if (_position < _text.Length)
        {
            _position++;
        }

        return new XPathToken(XPathTokenKind.String, value);
    }

    private XPathToken ReadNumber()
    {
        int start = _position;
        while (_position < _text.Length && (char.IsDigit(_text[_position]) || _text[_position] == '.'))
        {
            _position++;
        }

        string text = _text[start.._position];
        return decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal number)
            ? new XPathToken(XPathTokenKind.Number, text, number)
            : throw new XPathException($"'{text}' is not a number.");
    }

    private string ReadName()
    {
        int start = _position;

        while (_position < _text.Length && IsNamePart(_text[_position]))
        {
            // A double colon is the axis separator, not part of a name: child::cbc:TaxAmount is two tokens.
            if (_text[_position] == ':' && _position + 1 < _text.Length && _text[_position + 1] == ':')
            {
                break;
            }

            _position++;
        }

        return _text[start.._position];
    }

    private static bool IsNameStart(char value) => char.IsLetter(value) || value is '_';

    private static bool IsNamePart(char value) => char.IsLetterOrDigit(value) || value is '_' or '-' or '.' or ':';
}

/// <summary>Raised when an expression cannot be read or evaluated. A rule set is code: a broken one is a bug.</summary>
public sealed class XPathException : Exception
{
    /// <summary>Creates the exception with a message.</summary>
    public XPathException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and an inner cause.</summary>
    public XPathException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Creates the exception with no message.</summary>
    public XPathException()
    {
    }
}
