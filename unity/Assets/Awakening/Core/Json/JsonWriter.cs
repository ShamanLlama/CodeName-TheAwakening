using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Awakening.Core.Json
{
    /// <summary>
    /// Compact JSON writer for the shapes JsonReader produces: IDictionary&lt;string, object&gt;
    /// (written in enumeration order), IList / IEnumerable, string, bool, null and numbers.
    /// Integral numbers below 2^53 are written without a fraction ("35", not "35.0"), others
    /// with the shortest text that reads back to the same double, always in invariant culture.
    /// Non-finite numbers are rejected, as JSON has no representation for them.
    /// </summary>
    public static class JsonWriter
    {
        public static string Write(object value)
            => Write(value, FormatNumber);

        /// <summary>Allows a wire adapter to retain its existing numeric precision and key order.
        /// The formatter must return a JSON number; strings and containers still use this writer.</summary>
        public static string Write(object value, Func<double, string> formatNumber, bool sortKeys = false)
        {
            var builder = new StringBuilder();
            Append(builder, value, 0, formatNumber ?? throw new ArgumentNullException(nameof(formatNumber)), sortKeys);
            return builder.ToString();
        }

        public static string FormatNumber(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentException("JSON cannot hold a non-finite number");
            if (value == Math.Floor(value) && Math.Abs(value) < 9007199254740992.0)
                return ((long)value).ToString(CultureInfo.InvariantCulture);
            string text = value.ToString("R", CultureInfo.InvariantCulture);
            // Mono's "R" can fall back to 15 digits that do not round-trip; widen until they do.
            if (double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture) != value)
                text = value.ToString("G17", CultureInfo.InvariantCulture);
            return text;
        }

        static void Append(StringBuilder builder, object value, int depth, Func<double, string> formatNumber, bool sortKeys)
        {
            if (depth > JsonReader.MaxDepth) throw new ArgumentException("JSON nesting is too deep");
            switch (value)
            {
                case null: builder.Append("null"); return;
                case RawJson raw: builder.Append(raw.Text); return;
                case string text: AppendString(builder, text); return;
                case bool flag: builder.Append(flag ? "true" : "false"); return;
                case double number: builder.Append(formatNumber(number)); return;
                case float number: builder.Append(formatNumber(number)); return;
                case int number: builder.Append(number.ToString(CultureInfo.InvariantCulture)); return;
                case long number: builder.Append(number.ToString(CultureInfo.InvariantCulture)); return;
                case uint number: builder.Append(number.ToString(CultureInfo.InvariantCulture)); return;
                case IDictionary<string, object> map:
                    builder.Append('{');
                    bool first = true;
                    IEnumerable<KeyValuePair<string, object>> entries = sortKeys ? new SortedDictionary<string, object>(map, StringComparer.Ordinal) : map;
                    foreach (KeyValuePair<string, object> entry in entries)
                    {
                        if (!first) builder.Append(',');
                        first = false;
                        AppendString(builder, entry.Key);
                        builder.Append(':');
                        Append(builder, entry.Value, depth + 1, formatNumber, sortKeys);
                    }
                    builder.Append('}');
                    return;
                case IEnumerable items:
                    builder.Append('[');
                    bool head = true;
                    foreach (object item in items)
                    {
                        if (!head) builder.Append(',');
                        head = false;
                        Append(builder, item, depth + 1, formatNumber, sortKeys);
                    }
                    builder.Append(']');
                    return;
                default:
                    throw new ArgumentException($"JSON cannot hold a {value.GetType().Name}");
            }
        }

        static void AppendString(StringBuilder builder, string text)
        {
            builder.Append('"');
            foreach (char c in text)
            {
                switch (c)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    default:
                        if (c < 0x20) builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else builder.Append(c);
                        break;
                }
            }
            builder.Append('"');
        }
    }
}
