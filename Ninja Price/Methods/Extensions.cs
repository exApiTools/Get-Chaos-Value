using System;
using Ninja_Price.API.PoeNinja;

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

        return Math.Round((decimal)number, significantDigits).ToString($"#,##0.{new string(forceDecimals ? '0' : '#', significantDigits)}");
    }

    public static bool IsChanceable(this INameAndDetailsId item)
    {
        return !item.Name.StartsWith("Replica ", StringComparison.Ordinal) && 
               !item.DetailsId.EndsWith("-relic", StringComparison.Ordinal);
    }
}