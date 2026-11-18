using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Awakening.Core.Json
{
    /// <summary>
    /// Strict RFC 8259 reader. Objects become Dictionary&lt;string, object&gt; (duplicate keys are
    /// rejected), arrays List&lt;object&gt;, numbers double (like JavaScript and Godot), and
    /// literals bool or null. No reflection, so IL2CPP keeps little of it.
    /// </summary>
    public static class JsonReader
    {
        public const int MaxDepth = 64;

        public static object Parse(string text)
        {
            var reader = new Cursor(text);
            reader.SkipSpace();
            object value = reader.Value(0);
            reader.SkipSpace();
            if (!reader.AtEnd) throw reader.Error("Unexpected text after the value");
            return value;
        }

        public static bool TryParse(string text, out object value)
        {
            try
            {
                value = Parse(text);
                return true;
            }
            catch (FormatException)
            {
                value = null;
                return false;
            }
        }

        sealed class Cursor
        {
            readonly string _text;
            int _at;

            public Cursor(string text)
            {
                _text = text ?? throw new FormatException("JSON text is null");
            }

            public bool AtEnd => _at >= _text.Length;

            public FormatException Error(string message) => new FormatException($"{message} at character {_at}");

            public void SkipSpace()
            {
                while (_at < _text.Length)
                {
                    char c = _text[_at];
                    if (c != ' ' && c != '\t' && c != '\n' && c != '\r') return;
                    _at++;
                }
            }

            public object Value(int depth)
            {
                if (depth > MaxDepth) throw Error("Nesting is too deep");
                if (AtEnd) throw Error("Unexpected end");
                char c = _text[_at];
                switch (c)
                {
                    case '{': return Object(depth);
                    case '[': return Array(depth);
                    case '"': return String();
                    case 't': Literal("true"); return true;
                    case 'f': Literal("false"); return false;
                    case 'n': Literal("null"); return null;
                    default:
                        if (c == '-' || (c >= '0' && c <= '9')) return Number();
                        throw Error($"Unexpected character '{c}'");
                }
            }

            Dictionary<string, object> Object(int depth)
            {
                var result = new Dictionary<string, object>(StringComparer.Ordinal);
                _at++;
                SkipSpace();
                if (Peek('}')) return result;
                while (true)
                {
                    SkipSpace();
                    if (AtEnd || _text[_at] != '"') throw Error("Expected a key");
                    string key = String();
                    SkipSpace();
                    Expect(':');
                    SkipSpace();
                    if (result.ContainsKey(key)) throw Error($"Duplicate key '{key}'");
                    result[key] = Value(depth + 1);
                    SkipSpace();
                    if (Peek('}')) return result;
                    Expect(',');
                }
            }

            List<object> Array(int depth)
            {
                var result = new List<object>();
                _at++;
                SkipSpace();
                if (Peek(']')) return result;
                while (true)
                {
                    SkipSpace();
                    result.Add(Value(depth + 1));
                    SkipSpace();
                    if (Peek(']')) return result;
                    Expect(',');
                }
            }

            string String()
            {
                _at++;
                var builder = new StringBuilder();
                while (true)
                {
                    if (AtEnd) throw Error("Unterminated string");
                    char c = _text[_at++];
                    if (c == '"') return builder.ToString();
                    if (c < 0x20) throw Error("Control character in string");
                    if (c != '\\')
                    {
                        builder.Append(c);
                        continue;
                    }
                    if (AtEnd) throw Error("Unterminated escape");
                    char e = _text[_at++];
                    switch (e)
                    {
                        case '"': builder.Append('"'); break;
                        case '\\': builder.Append('\\'); break;
                        case '/': builder.Append('/'); break;
                        case 'b': builder.Append('\b'); break;
                        case 'f': builder.Append('\f'); break;
                        case 'n': builder.Append('\n'); break;
                        case 'r': builder.Append('\r'); break;
                        case 't': builder.Append('\t'); break;
                        case 'u':
                            if (_at + 4 > _text.Length) throw Error("Short unicode escape");
                            if (!int.TryParse(_text.Substring(_at, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int unit))
                                throw Error("Bad unicode escape");
                            builder.Append((char)unit);
                            _at += 4;
                            break;
                        default: throw Error($"Bad escape '\\{e}'");
                    }
                }
            }

            double Number()
            {
                int start = _at;
                if (_text[_at] == '-') _at++;
                if (AtEnd) throw Error("Bad number");
                if (_text[_at] == '0') _at++;
                else if (!Digits()) throw Error("Bad number");
                if (!AtEnd && _text[_at] == '.')
                {
                    _at++;
                    if (!Digits()) throw Error("Bad fraction");
                }
                if (!AtEnd && (_text[_at] == 'e' || _text[_at] == 'E'))
                {
                    _at++;
                    if (!AtEnd && (_text[_at] == '+' || _text[_at] == '-')) _at++;
                    if (!Digits()) throw Error("Bad exponent");
                }
                double value = double.Parse(_text.Substring(start, _at - start), NumberStyles.Float, CultureInfo.InvariantCulture);
                if (double.IsInfinity(value)) throw Error("Number out of range");
                return value;
            }

            bool Digits()
            {
                int start = _at;
                while (!AtEnd && _text[_at] >= '0' && _text[_at] <= '9') _at++;
                return _at > start;
            }

            void Literal(string word)
            {
                if (string.CompareOrdinal(_text, _at, word, 0, word.Length) != 0) throw Error($"Expected {word}");
                _at += word.Length;
            }

            bool Peek(char c)
            {
                if (AtEnd || _text[_at] != c) return false;
                _at++;
                return true;
            }

            void Expect(char c)
            {
                if (!Peek(c)) throw Error($"Expected '{c}'");
            }
        }
    }
}
