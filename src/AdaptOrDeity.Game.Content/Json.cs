using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AdaptOrDeity.Game.Content
{
    /// <summary>
    /// A small JSON reader for the content files (netstandard2.1 has no System.Text.Json, and the simulation must not
    /// depend on Unity's). Objects become <see cref="Dictionary{TKey,TValue}"/>, arrays <see cref="List{T}"/>, numbers
    /// <see cref="double"/>, and true/false/null their C# values.
    /// </summary>
    public static class Json
    {
        public static object? Parse(string text)
        {
            int i = 0;
            object? value = ReadValue(text, ref i);
            SkipSpace(text, ref i);
            if (i != text.Length)
                throw Error(text, i, "trailing characters");
            return value;
        }

        private static object? ReadValue(string s, ref int i)
        {
            SkipSpace(s, ref i);
            if (i >= s.Length)
                throw Error(s, i, "unexpected end");
            char c = s[i];
            switch (c)
            {
                case '{': return ReadObject(s, ref i);
                case '[': return ReadArray(s, ref i);
                case '"': return ReadString(s, ref i);
                case 't': Expect(s, ref i, "true"); return true;
                case 'f': Expect(s, ref i, "false"); return false;
                case 'n': Expect(s, ref i, "null"); return null;
                default: return ReadNumber(s, ref i);
            }
        }

        private static Dictionary<string, object?> ReadObject(string s, ref int i)
        {
            var d = new Dictionary<string, object?>();
            i++;
            SkipSpace(s, ref i);
            if (i < s.Length && s[i] == '}')
            {
                i++;
                return d;
            }
            while (true)
            {
                SkipSpace(s, ref i);
                if (i >= s.Length || s[i] != '"')
                    throw Error(s, i, "expected a key");
                string key = ReadString(s, ref i);
                SkipSpace(s, ref i);
                if (i >= s.Length || s[i] != ':')
                    throw Error(s, i, "expected ':'");
                i++;
                d[key] = ReadValue(s, ref i);
                SkipSpace(s, ref i);
                if (i < s.Length && s[i] == ',')
                {
                    i++;
                    continue;
                }
                if (i < s.Length && s[i] == '}')
                {
                    i++;
                    return d;
                }
                throw Error(s, i, "expected ',' or '}'");
            }
        }

        private static List<object?> ReadArray(string s, ref int i)
        {
            var list = new List<object?>();
            i++;
            SkipSpace(s, ref i);
            if (i < s.Length && s[i] == ']')
            {
                i++;
                return list;
            }
            while (true)
            {
                list.Add(ReadValue(s, ref i));
                SkipSpace(s, ref i);
                if (i < s.Length && s[i] == ',')
                {
                    i++;
                    continue;
                }
                if (i < s.Length && s[i] == ']')
                {
                    i++;
                    return list;
                }
                throw Error(s, i, "expected ',' or ']'");
            }
        }

        private static string ReadString(string s, ref int i)
        {
            var sb = new StringBuilder();
            i++;
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"')
                    return sb.ToString();
                if (c != '\\')
                {
                    sb.Append(c);
                    continue;
                }
                if (i >= s.Length)
                    break;
                char e = s[i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 > s.Length)
                            throw Error(s, i, "short \\u escape");
                        sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        i += 4;
                        break;
                    default: throw Error(s, i, $"bad escape \\{e}");
                }
            }
            throw Error(s, i, "unterminated string");
        }

        private static double ReadNumber(string s, ref int i)
        {
            int start = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0)
                i++;
            if (start == i || !double.TryParse(s.Substring(start, i - start), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out double v))
                throw Error(s, start, "expected a value");
            return v;
        }

        private static void Expect(string s, ref int i, string word)
        {
            if (string.CompareOrdinal(s, i, word, 0, word.Length) != 0)
                throw Error(s, i, $"expected {word}");
            i += word.Length;
        }

        private static void SkipSpace(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i]))
                i++;
        }

        private static FormatException Error(string s, int i, string what)
        {
            int line = 1;
            for (int k = 0; k < i && k < s.Length; k++)
                if (s[k] == '\n')
                    line++;
            return new FormatException($"JSON line {line}: {what}");
        }
    }
}
