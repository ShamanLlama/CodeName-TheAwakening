using System;
using System.Globalization;

namespace Awakening.UI
{
    /// <summary>Number and time formatting for the interface. Port of godot/ui/format.gd.</summary>
    public static class Format
    {
        /// <summary>Whole people, with thousands separators: 12,345.</summary>
        public static string Count(double value)
        {
            long whole = (long)Math.Floor(value);
            string digits = Math.Abs(whole).ToString(CultureInfo.InvariantCulture);
            string grouped = "";
            while (digits.Length > 3)
            {
                grouped = "," + digits.Substring(digits.Length - 3) + grouped;
                digits = digits.Substring(0, digits.Length - 3);
            }
            return (whole < 0 ? "-" : "") + digits + grouped;
        }

        /// <summary>Monthly rates read to one decimal: +4.1. Two decimals suggested a precision nobody plans with.</summary>
        public static string Rate(double value)
        {
            double rounded = Math.Round(value / 0.1, MidpointRounding.AwayFromZero) * 0.1;
            return (rounded >= 0 ? "+" : "−") + Math.Abs(rounded).ToString("0.0", CultureInfo.InvariantCulture);
        }

        public static string Months(int value) => value + " month" + (value == 1 ? "" : "s");

        // Presentation copy only: preserve the rules engine's forecast and serialized contract.
        public static string ForecastCopy(string message) => System.Text.RegularExpressions.Regex.Replace(
            message, @"(\d+)( more ritual | )site\(s\)", match => match.Groups[1].Value + match.Groups[2].Value
                + (match.Groups[1].Value == "1" ? "site" : "sites"));

        /// <summary>
        /// The game opens in January 1908. With a 200-month preparation the alignment opens in
        /// September 1924 and closes in September 1926, around the night R'lyeh rose: 23 March 1925.
        /// </summary>
        public const int FirstYear = 1908;

        public static readonly string[] MonthNames =
            { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

        /// <summary>"March 1925"</summary>
        public static string Date(int month) => MonthNames[month % 12] + " " + (FirstYear + (int)Math.Floor(month / 12.0));

        /// <summary>"Mar 1925"</summary>
        public static string DateShort(int month) => MonthNames[month % 12].Substring(0, 3) + " " + (FirstYear + (int)Math.Floor(month / 12.0));

        /// <summary>Zero-based month index for a calendar year and month (0 = January).</summary>
        public static int MonthOf(int year, int monthOfYear = 0) => (year - FirstYear) * 12 + monthOfYear;
    }
}
