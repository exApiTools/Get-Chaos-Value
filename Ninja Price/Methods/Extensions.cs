using Ninja_Price.API.PoeNinja;
using System;
using System.Globalization;
using System.Linq;

namespace Ninja_Price.Main;

public static class Extensions
{
    public static string FormatNumber(this double number, int significantDigits, double maxInvertValue = 0, bool forceDecimals = false)
    {
        if (double.IsNaN(number))
        {
            return "NaN";
        }

        if (number == 0)
        {
            return "0";
        }

        if (Math.Abs(number) <= 1e-10)
        {
            return "~0";
        }

        if (Math.Abs(number) < maxInvertValue)
        {
            return $"1/{Math.Round((decimal)(1 / number), 1):#.#}";
        }

        return Math.Round((decimal)number, significantDigits).ToString($"#,##0.{new string(forceDecimals ? '0' : '#', significantDigits)}", RenderableNumberFormat);
    }

    private static NumberFormatInfo _renderableNumberFormat;
    private static CultureInfo _renderableNumberFormatCulture;

    private static NumberFormatInfo RenderableNumberFormat
    {
        get
        {
            var culture = CultureInfo.CurrentCulture;
            if (_renderableNumberFormat != null && Equals(_renderableNumberFormatCulture, culture))
            {
                return _renderableNumberFormat;
            }

            var format = (NumberFormatInfo)culture.NumberFormat.Clone();
            if (format.NumberGroupSeparator.Any(c => c > 127))
            {
                format.NumberGroupSeparator = " ";
            }

            _renderableNumberFormatCulture = culture;
            return _renderableNumberFormat = format;
        }
    }

    public static bool IsChanceable(this INameAndDetailsId item)
    {
        return !item.Name.StartsWith("Replica ", StringComparison.Ordinal) && 
               !item.DetailsId.EndsWith("-relic", StringComparison.Ordinal);
    }
}