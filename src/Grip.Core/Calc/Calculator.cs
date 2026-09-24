using System.Globalization;
using System.Text.RegularExpressions;

namespace Grip.Core.Calc;

/// <summary>
/// A small, forgiving calculator for the command field. Understands + − × ÷ ^,
/// parentheses, percentages ("200 + 15%", "15% от 200"), factorials, √,
/// constants π and e, common functions, implicit multiplication ("2π", "3(4+1)")
/// and both decimal separators ("3,5" and "3.5").
/// </summary>
public static partial class Calculator
{
    public static bool TryEvaluate(string input, out double result)
    {
        result = double.NaN;
        if (string.IsNullOrWhiteSpace(input) || input.Length > 256) return false;
        var text = input.Trim().TrimEnd('=').Trim();
        if (text.Length == 0) return false;

        try
        {
            var parser = new Parser(Tokenize(text));
            var (value, _) = parser.ParseExpression();
            if (!parser.AtEnd) return false;
            result = value;
            return double.IsFinite(value);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// <summary>
    /// True when the input is worth showing a calculator row for: something to
    /// compute, not just a bare number.
    /// </summary>
    public static bool LooksLikeMath(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        var t = input.Trim().ToLowerInvariant();
        if (t is "pi" or "π" or "e") return true;
        bool hasDigit = t.Any(char.IsDigit) || t.Contains('π') || t.Contains("pi");
        if (!hasDigit) return false;
        if (t.IndexOfAny(new[] { '+', '*', '/', '^', '%', '×', '÷', '!', '√', '(', '−' }) >= 0) return true;
        if (MultiplyByXRegex().IsMatch(t)) return true;
        // A minus between operands, not a leading sign.
        if (t.LastIndexOf('-') > 0) return true;
        return FunctionNames.Any(f => t.Contains(f + "(", StringComparison.Ordinal));
    }

    public static string Format(double value, CultureInfo culture, bool grouping = true)
    {
        if (value == 0) return "0";
        double abs = Math.Abs(value);
        if (abs >= 1e15 || abs < 1e-9)
            return value.ToString("0.#########E+0", culture);
        var rounded = Math.Round(value, 10);
        return rounded.ToString(grouping ? "#,##0.##########" : "0.##########", culture);
    }

    [GeneratedRegex(@"\d\s*[xх]\s*\d", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MultiplyByXRegex();

    private static readonly string[] FunctionNames =
    {
        "sqrt", "sin", "cos", "tan", "tg", "asin", "acos", "atan", "arctg", "ln", "log", "lg", "log2",
        "abs", "round", "floor", "ceil", "exp", "cbrt",
    };

    // ---------- tokenizer ----------

    private enum TokenKind { Number, Identifier, Operator, LeftParen, RightParen }

    private readonly record struct Token(TokenKind Kind, string Text, double Number = 0);

    private static List<Token> Tokenize(string text)
    {
        var tokens = new List<Token>();
        int i = 0;
        while (i < text.Length)
        {
            char c = text[i];
            if (char.IsWhiteSpace(c)) { i++; continue; }

            if (char.IsDigit(c) || ((c == '.' || c == ',') && i + 1 < text.Length && char.IsDigit(text[i + 1])))
            {
                tokens.Add(ReadNumber(text, ref i));
                continue;
            }

            // "3x4" and "3 х 4" read as multiplication.
            if ((c is 'x' or 'X' or 'х' or 'Х') && tokens.Count > 0 && tokens[^1].Kind == TokenKind.Number
                && NextNonSpaceIsDigit(text, i + 1))
            {
                tokens.Add(new Token(TokenKind.Operator, "*"));
                i++;
                continue;
            }

            if (char.IsLetter(c) || c == 'π' || c == '√')
            {
                if (c == 'π' || c == '√')
                {
                    tokens.Add(new Token(TokenKind.Identifier, c == 'π' ? "pi" : "sqrt"));
                    i++;
                    continue;
                }
                int start = i;
                while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_')) i++;
                var word = text[start..i].ToLowerInvariant();
                // "x" between numbers reads as multiplication: "3x4".
                if (word == "x" || word == "х") { tokens.Add(new Token(TokenKind.Operator, "*")); continue; }
                if (word is "mod") { tokens.Add(new Token(TokenKind.Operator, "mod")); continue; }
                // "15% от 200", "15% of 200": a percentage of something is a product.
                if (word is "от" or "of" or "из") { tokens.Add(new Token(TokenKind.Operator, "*")); continue; }
                tokens.Add(new Token(TokenKind.Identifier, word));
                continue;
            }

            switch (c)
            {
                case '+': tokens.Add(new Token(TokenKind.Operator, "+")); break;
                case '-': case '−': case '–': tokens.Add(new Token(TokenKind.Operator, "-")); break;
                case '*': case '×': case '·':
                    if (c == '*' && i + 1 < text.Length && text[i + 1] == '*')
                    {
                        tokens.Add(new Token(TokenKind.Operator, "^"));
                        i++;
                    }
                    else tokens.Add(new Token(TokenKind.Operator, "*"));
                    break;
                case '/': case '÷': case ':': tokens.Add(new Token(TokenKind.Operator, "/")); break;
                case '^': tokens.Add(new Token(TokenKind.Operator, "^")); break;
                case '%': tokens.Add(new Token(TokenKind.Operator, "%")); break;
                case '!': tokens.Add(new Token(TokenKind.Operator, "!")); break;
                case '(': case '[': tokens.Add(new Token(TokenKind.LeftParen, "(")); break;
                case ')': case ']': tokens.Add(new Token(TokenKind.RightParen, ")")); break;
                default: throw new FormatException($"Unexpected '{c}'.");
            }
            i++;
        }
        return tokens;
    }

    private static bool NextNonSpaceIsDigit(string text, int start)
    {
        for (int k = start; k < text.Length; k++)
        {
            if (char.IsWhiteSpace(text[k])) continue;
            return char.IsDigit(text[k]);
        }
        return false;
    }

    private static Token ReadNumber(string text, ref int i)
    {
        var digits = new System.Text.StringBuilder();
        bool seenSeparator = false;
        while (i < text.Length)
        {
            char c = text[i];
            if (char.IsDigit(c)) { digits.Append(c); i++; continue; }
            if ((c == '.' || c == ',') && !seenSeparator && i + 1 < text.Length && char.IsDigit(text[i + 1]))
            {
                digits.Append('.');
                seenSeparator = true;
                i++;
                continue;
            }
            // Thousands grouping with a (non-breaking) space or underscore: "1 000 000".
            if ((c == ' ' || c == ' ' || c == ' ' || c == '_') && !seenSeparator
                && i + 3 < text.Length + 0 && IsThreeDigitGroup(text, i + 1))
            {
                i++;
                continue;
            }
            break;
        }

        // Scientific notation: 1e6, 2.5E-3.
        if (i < text.Length && (text[i] == 'e' || text[i] == 'E') && i + 1 < text.Length)
        {
            int j = i + 1;
            if (j < text.Length && (text[j] == '+' || text[j] == '-')) j++;
            if (j < text.Length && char.IsDigit(text[j]))
            {
                digits.Append('e');
                if (text[i + 1] == '-' || text[i + 1] == '+') digits.Append(text[i + 1]);
                i = j;
                while (i < text.Length && char.IsDigit(text[i])) digits.Append(text[i++]);
            }
        }

        var raw = digits.ToString();
        if (raw.StartsWith('.')) raw = "0" + raw;
        if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            throw new FormatException("Bad number.");
        return new Token(TokenKind.Number, raw, value);
    }

    private static bool IsThreeDigitGroup(string text, int start)
    {
        if (start + 3 > text.Length) return false;
        for (int k = 0; k < 3; k++)
            if (!char.IsDigit(text[start + k])) return false;
        // The group must end there: "1 0005" is not grouping.
        return start + 3 == text.Length || !char.IsDigit(text[start + 3]);
    }

    // ---------- parser ----------

    private sealed class Parser
    {
        private readonly List<Token> _tokens;
        private int _pos;

        public Parser(List<Token> tokens) => _tokens = tokens;

        public bool AtEnd => _pos >= _tokens.Count;

        private Token? Peek => _pos < _tokens.Count ? _tokens[_pos] : null;

        private bool IsOperator(string op) => Peek is { Kind: TokenKind.Operator } t && t.Text == op;

        /// <summary>Returns the value and whether it was a bare percentage term.</summary>
        public (double Value, bool IsPercent) ParseExpression()
        {
            var (left, leftPercent) = ParseTerm();
            while (IsOperator("+") || IsOperator("-"))
            {
                bool plus = Peek!.Value.Text == "+";
                _pos++;
                var (right, rightPercent) = ParseTerm();
                if (rightPercent) right = left * right; // "200 + 15%" adds 15% of 200
                left = plus ? left + right : left - right;
                leftPercent = false;
            }
            return (left, leftPercent);
        }

        private (double Value, bool IsPercent) ParseTerm()
        {
            var (left, isPercent) = ParseUnary();
            while (true)
            {
                if (IsOperator("*") || IsOperator("/") || IsOperator("mod"))
                {
                    var op = Peek!.Value.Text;
                    _pos++;
                    var (right, _) = ParseUnary();
                    left = op switch
                    {
                        "*" => left * right,
                        "/" => right == 0 ? throw new FormatException("Division by zero.") : left / right,
                        _ => right == 0 ? throw new FormatException("Division by zero.") : left % right,
                    };
                    isPercent = false;
                }
                else if (Peek is { Kind: TokenKind.LeftParen or TokenKind.Identifier or TokenKind.Number } && _pos > 0
                         && _tokens[_pos - 1].Kind is TokenKind.Number or TokenKind.RightParen or TokenKind.Identifier
                         && !(Peek.Value.Kind == TokenKind.Number && _tokens[_pos - 1].Kind == TokenKind.Number))
                {
                    // Implicit multiplication: 2π, 3(4+1), (1+2)(3+4).
                    if (_tokens[_pos - 1].Kind == TokenKind.Identifier && IsFunction(_tokens[_pos - 1].Text)) break;
                    var (right, _) = ParseUnary();
                    left *= right;
                    isPercent = false;
                }
                else break;
            }
            return (left, isPercent);
        }

        private (double Value, bool IsPercent) ParseUnary()
        {
            if (IsOperator("-")) { _pos++; var (v, p) = ParseUnary(); return (-v, p); }
            if (IsOperator("+")) { _pos++; return ParseUnary(); }
            return ParsePower();
        }

        private (double Value, bool IsPercent) ParsePower()
        {
            var (value, isPercent) = ParsePostfix();
            if (IsOperator("^"))
            {
                _pos++;
                var (exponent, _) = ParseUnary();
                value = Math.Pow(value, exponent);
                isPercent = false;
            }
            return (value, isPercent);
        }

        private (double Value, bool IsPercent) ParsePostfix()
        {
            double value = ParsePrimary();
            bool isPercent = false;
            while (true)
            {
                if (IsOperator("%")) { _pos++; value /= 100.0; isPercent = true; continue; }
                if (IsOperator("!")) { _pos++; value = Factorial(value); isPercent = false; continue; }
                break;
            }
            return (value, isPercent);
        }

        private double ParsePrimary()
        {
            var token = Peek ?? throw new FormatException("Unexpected end.");
            switch (token.Kind)
            {
                case TokenKind.Number:
                    _pos++;
                    return token.Number;
                case TokenKind.LeftParen:
                {
                    _pos++;
                    var (value, _) = ParseExpression();
                    if (Peek is { Kind: TokenKind.RightParen }) _pos++;
                    // A missing closing parenthesis at the very end is forgiven: "sqrt(16".
                    else if (!AtEnd) throw new FormatException("Missing ')'.");
                    return value;
                }
                case TokenKind.Identifier:
                {
                    _pos++;
                    var name = token.Text;
                    if (name is "pi" or "пи") return Math.PI;
                    if (name == "e") return Math.E;
                    if (!IsFunction(name)) throw new FormatException($"Unknown '{name}'.");
                    // Functions accept "sqrt 16", "sqrt(16)" and "√16".
                    double argument = Peek is { Kind: TokenKind.LeftParen } ? ParsePrimary() : ParsePostfix().Value;
                    return Apply(name, argument);
                }
                default:
                    throw new FormatException($"Unexpected '{token.Text}'.");
            }
        }
    }

    private static bool IsFunction(string name) => Array.IndexOf(FunctionNames, name) >= 0;

    private static double Apply(string name, double x) => name switch
    {
        "sqrt" => x < 0 ? double.NaN : Math.Sqrt(x),
        "cbrt" => Math.Cbrt(x),
        "sin" => Math.Sin(x),
        "cos" => Math.Cos(x),
        "tan" or "tg" => Math.Tan(x),
        "asin" => Math.Asin(x),
        "acos" => Math.Acos(x),
        "atan" or "arctg" => Math.Atan(x),
        "ln" => Math.Log(x),
        "log" or "lg" => Math.Log10(x),
        "log2" => Math.Log2(x),
        "abs" => Math.Abs(x),
        "round" => Math.Round(x, MidpointRounding.AwayFromZero),
        "floor" => Math.Floor(x),
        "ceil" => Math.Ceiling(x),
        "exp" => Math.Exp(x),
        _ => throw new FormatException($"Unknown function '{name}'."),
    };

    private static double Factorial(double n)
    {
        if (n < 0 || n > 170 || Math.Abs(n - Math.Round(n)) > 1e-9) throw new FormatException("Bad factorial.");
        double result = 1;
        for (int i = 2; i <= (int)Math.Round(n); i++) result *= i;
        return result;
    }
}
