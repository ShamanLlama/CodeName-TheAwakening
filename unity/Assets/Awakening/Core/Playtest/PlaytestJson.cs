using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using Awakening.Core.Json;

namespace Awakening.Core.Playtest
{
    // All persisted values remain JSON trees, including unknown fields from Godot saves.
    internal static class PlaytestJson
    {
        internal static Dictionary<string, object> Map(params object[] pairs)
        {
            var result = new Dictionary<string, object>(StringComparer.Ordinal);
            for (int i = 0; i < pairs.Length; i += 2) result[(string)pairs[i]] = pairs[i + 1];
            return result;
        }
        internal static object Get(Dictionary<string, object> map, string key, object fallback = null) => map.TryGetValue(key, out object value) ? value : fallback;
        internal static Dictionary<string, object> Obj(Dictionary<string, object> map, string key) => (Dictionary<string, object>)map[key];
        internal static List<object> Arr(Dictionary<string, object> map, string key) => (List<object>)map[key];
        internal static string Str(Dictionary<string, object> map, string key) => (string)map[key];
        internal static double Num(Dictionary<string, object> map, string key) => Convert.ToDouble(map[key], System.Globalization.CultureInfo.InvariantCulture);
        internal static bool Flag(Dictionary<string, object> map, string key) => Get(map, key) is bool flag && flag;
        internal static T Clone<T>(T value) => (T)Copy(value);
        static object Copy(object value)
        {
            if (value is Dictionary<string, object> map)
            {
                var copy = Map();
                foreach (var entry in map) copy[entry.Key] = Copy(entry.Value);
                return copy;
            }
            if (value is List<object> list)
            {
                var copy = new List<object>();
                foreach (object item in list) copy.Add(Copy(item));
                return copy;
            }
            return value;
        }
        internal static string Stringify(object value) => JsonWriter.Write(value, GodotNumber, sortKeys: true);
        internal static int Bytes(object value) => Encoding.UTF8.GetByteCount(Stringify(value));

        // JSON.stringify's default precision: max(1, 14 - floor(log10(abs(number)))),
        // capped at 32 decimal places by String::num. Use exact binary arithmetic so
        // Mono and IL2CPP round just like snprintf, including values near a midpoint.
        static string GodotNumber(double number)
        {
            if (double.IsNaN(number) || double.IsInfinity(number)) throw new ArgumentException("JSON cannot hold a non-finite number");
            if (number == 0) return "0.0";
            int precision = Math.Min(32, Math.Max(1, 14 - (int)Math.Floor(Math.Log10(Math.Abs(number)))));
            long bits = BitConverter.DoubleToInt64Bits(number);
            int exponent = (int)((bits >> 52) & 2047);
            long mantissa = bits & 0xfffffffffffffL;
            if (exponent != 0) mantissa |= 1L << 52;
            int shift = (exponent == 0 ? -1074 : exponent - 1075) + precision;
            BigInteger scaled = new BigInteger(mantissa) * BigInteger.Pow(5, precision);
            if (shift >= 0) scaled <<= shift;
            else
            {
                BigInteger divisor = BigInteger.One << -shift;
                scaled = BigInteger.DivRem(scaled, divisor, out BigInteger remainder);
                int compare = (remainder * 2).CompareTo(divisor);
                if (compare > 0 || (compare == 0 && !scaled.IsEven)) scaled++;
            }
            string digits = scaled.ToString(CultureInfo.InvariantCulture).PadLeft(precision + 1, '0');
            string fraction = digits.Substring(digits.Length - precision).TrimEnd('0');
            return (number < 0 ? "-" : "") + digits.Substring(0, digits.Length - precision) + "." + (fraction.Length == 0 ? "0" : fraction);
        }
        internal static string StripEdges(string text)
        {
            int start = 0, end = text.Length;
            while (start < end && text[start] <= 32) start++;
            while (end > start && text[end - 1] <= 32) end--;
            return text.Substring(start, end - start);
        }
        internal static int Characters(string value)
        {
            int count = 0;
            for (int i = 0; i < value.Length; i++, count++)
                if (char.IsHighSurrogate(value[i]) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1])) i++;
            return count;
        }
        internal static bool Number(object value, double maximum = 9007199254740991d)
        {
            if (!(value is double || value is int || value is long || value is uint)) return false;
            double number = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
            return !double.IsNaN(number) && !double.IsInfinity(number) && number >= 0 && number <= maximum;
        }
        internal static bool Integer(object value, double maximum = 9007199254740991d) => Number(value, maximum) && Convert.ToDouble(value) == Math.Floor(Convert.ToDouble(value));
    }
}
