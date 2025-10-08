using System.Text;

namespace ClassIsland.Core.Helpers.SimpleExpression;

internal static class SimpleExprParser
{
    public static Expression Parse(string input)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        int i = 0;
        SkipWhite();

        var fname = ParseIdentifier();
        if (string.IsNullOrEmpty(fname))
            throw new FormatException("Missing function name.");

        SkipWhite();
        if (!TryConsume('('))
            throw new FormatException("Expected '(' after function name.");

        var args = ParseArguments();

        SkipWhite();
        if (i < input.Length)
        {
            SkipWhite();
            if (i < input.Length) throw new FormatException("Unexpected trailing characters.");
        }

        return new Expression(fname, args);

        void SkipWhite()
        {
            while (i < input.Length && char.IsWhiteSpace(input[i])) i++;
        }

        bool TryConsume(char c)
        {
            SkipWhite();
            if (i < input.Length && input[i] == c)
            {
                i++;
                return true;
            }

            return false;
        }

        string ParseIdentifier()
        {
            SkipWhite();
            int start = i;
            while (i < input.Length && (char.IsLetterOrDigit(input[i]) || input[i] == '_' || input[i] == '.'))
                i++;
            return input.Substring(start, i - start).Trim();
        }

        string[] ParseArguments()
        {
            var list = new List<string>();
            SkipWhite();
            if (i < input.Length && input[i] == ')')
            {
                i++; // consume ')'
                return list.ToArray(); // no args
            }

            while (true)
            {
                SkipWhite();
                if (i >= input.Length)
                    throw new FormatException("Unexpected end of input while parsing arguments.");

                string arg;
                if (input[i] == '"' || input[i] == '\'')
                {
                    arg = ParseQuoted();
                }
                else
                {
                    arg = ParseUnquoted();
                }

                list.Add(arg);

                SkipWhite();
                if (i >= input.Length)
                    throw new FormatException("Unexpected end of input after argument.");

                if (input[i] == ',')
                {
                    i++; // consume comma and continue
                    continue;
                }
                else if (input[i] == ')')
                {
                    i++; // consume ')'
                    break;
                }
                else
                {
                    throw new FormatException(
                        $"Expected ',' or ')' after argument, found '{input[i]}' at position {i}.");
                }
            }

            return list.ToArray();
        }

        string ParseQuoted()
        {
            // input[i] is either " or '
            char quote = input[i++];
            var sb = new StringBuilder();
            while (i < input.Length)
            {
                char c = input[i++];
                if (c == '\\') // escape sequence
                {
                    if (i >= input.Length) throw new FormatException("Invalid escape at end of input.");
                    char next = input[i++];
                    switch (next)
                    {
                        case '\\': sb.Append('\\'); break;
                        case '"': sb.Append('"'); break;
                        case '\'': sb.Append('\''); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        default:
                            // 未知转义：按原样添加（或你也可以抛错）
                            sb.Append(next);
                            break;
                    }

                    continue;
                }

                if (c == quote)
                {
                    // 结束
                    return sb.ToString();
                }

                sb.Append(c);
            }

            throw new FormatException("Unterminated quoted string.");
        }

        string ParseUnquoted()
        {
            int start = i;
            while (i < input.Length && input[i] != ',' && input[i] != ')')
            {
                i++;
            }

            var raw = input.Substring(start, i - start);
            return raw.Trim();
        }
    }
}