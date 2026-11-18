using System;
using System.Globalization;
using System.Numerics;

namespace Awakening.Core.Rules
{
    /// <summary>The helpers in godot/core/js.gd, including its fixed-decimal search.</summary>
    public static class Js
    {
        public static double RoundHalfUp(double x) => Math.Floor(x + 0.5);

        public static string Num(double x)
        {
            if (x == Math.Floor(x) && Math.Abs(x) < 1e15)
                return ((long)x).ToString(CultureInfo.InvariantCulture);
            if (double.IsNaN(x)) return "nan";
            if (double.IsInfinity(x)) return x < 0 ? "-inf" : "inf";
            string text = "";
            for (int digits = 1; digits <= 17; digits++)
            {
                text = DecimalText(x, digits);
                if (GodotDecimal(text) == x) return text;
            }
            return text;
        }

        // String.to_float's decimal path accumulates at most 18 digits in two integer
        // groups, then scales by binary powers of ten. Its last-bit rounding differs
        // from double.Parse, and that difference decides when js.gd ends its search.
        static double GodotDecimal(string text)
        {
            bool negative = text[0] == '-';
            string unsigned = negative ? text.Substring(1) : text;
            int point = unsigned.IndexOf('.');
            if (point < 0) point = unsigned.Length;
            string mantissa = unsigned.Replace(".", "");
            int count = Math.Min(18, mantissa.Length);
            int exponent = point - count;
            int first = 0, second = 0, at = 0;
            for (; count > 9; count--) first = 10 * first + mantissa[at++] - '0';
            for (; count > 0; count--) second = 10 * second + mantissa[at++] - '0';
            double fraction = 1.0e9 * first + second;
            double[] powers = { 10.0, 100.0, 1.0e4, 1.0e8, 1.0e16, 1.0e32, 1.0e64, 1.0e128, 1.0e256 };
            double scale = 1.0;
            int e = Math.Abs(exponent), i = 0;
            for (; e != 0; e >>= 1, i++) if ((e & 1) != 0) scale *= powers[i];
            fraction = exponent < 0 ? fraction / scale : fraction * scale;
            return negative ? -fraction : fraction;
        }

        // Godot String.num uses snprintf("%.*lf") and trims zeros, retaining one fractional
        // digit. Round the exact binary rational ourselves (ties to even), so this search
        // behaves identically on .NET, Mono and IL2CPP without any runtime double formatter.
        static string DecimalText(double x, int digits)
        {
            ulong bits = (ulong)BitConverter.DoubleToInt64Bits(x);
            int exponent = (int)((bits >> 52) & 2047);
            ulong mantissa = bits & 0xfffffffffffffUL;
            if (exponent != 0) mantissa |= 1UL << 52;
            int power = exponent == 0 ? -1074 : exponent - 1023 - 52;
            BigInteger numerator = new BigInteger(mantissa) * BigInteger.Pow(10, digits);
            BigInteger rounded;
            if (power >= 0) rounded = numerator << power;
            else
            {
                BigInteger divisor = BigInteger.One << -power;
                rounded = BigInteger.DivRem(numerator, divisor, out BigInteger remainder);
                int comparison = (remainder * 2).CompareTo(divisor);
                if (comparison > 0 || comparison == 0 && !rounded.IsEven) rounded++;
            }
            string text = DecimalPlaces(rounded.ToString(CultureInfo.InvariantCulture), digits);
            text = text.TrimEnd('0');
            if (text.EndsWith(".", StringComparison.Ordinal)) text += "0";
            return (bits >> 63 != 0 ? "-" : "") + text;
        }

        public static string Fixed(double x, int digits)
        {
            bool negative = x < 0.0;
            long scaled = (long)Math.Floor(Math.Abs(x) * Math.Pow(10.0, digits) + 0.5);
            string text = DecimalPlaces(scaled.ToString(CultureInfo.InvariantCulture), digits);
            return (negative ? "-" : "") + text;
        }
        static string DecimalPlaces(string text, int digits)
        {
            if (digits <= 0) return text;
            text = text.PadLeft(digits + 1, '0');
            return text.Insert(text.Length - digits, ".");
        }
    }
}
