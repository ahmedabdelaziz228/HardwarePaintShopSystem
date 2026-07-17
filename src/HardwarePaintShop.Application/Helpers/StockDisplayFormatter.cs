namespace HardwarePaintShop.Application.Helpers;

/// <summary>
/// Formats stock quantities for display in the UI.
/// All quantities are stored in base units; this helper produces human-readable strings.
/// Supports compound display like "47 شكارة و20 كيلو".
/// </summary>
public static class StockDisplayFormatter
{
    /// <summary>
    /// Formats a base-unit quantity with the base unit name for simple display.
    /// </summary>
    /// <param name="baseQuantity">Quantity in the base unit.</param>
    /// <param name="baseUnitName">Name of the base unit (e.g. "كيلو", "قطعة").</param>
    /// <returns>Formatted string (e.g. "1250 كيلو", "0 قطعة").</returns>
    public static string FormatBaseQuantity(decimal baseQuantity, string baseUnitName)
    {
        // Remove trailing zeros but keep up to 3 decimal places for quantities
        var formatted = baseQuantity.ToString("0.###");
        return $"{formatted} {baseUnitName}";
    }

    /// <summary>Formats base stock using the largest available units first.</summary>
    public static string FormatCompound(
        decimal baseQuantity,
        IEnumerable<(string UnitName, decimal Factor)> units)
    {
        var validUnits = units
            .Where(u => !string.IsNullOrWhiteSpace(u.UnitName) && u.Factor > 0)
            .GroupBy(u => u.Factor)
            .Select(g => g.First())
            .OrderByDescending(u => u.Factor)
            .ToList();
        if (validUnits.Count == 0)
            return baseQuantity.ToString("0.###");
        if (baseQuantity < 0)
            return $"-{FormatCompound(Math.Abs(baseQuantity), validUnits)}";

        var remaining = baseQuantity;
        var parts = new List<string>();
        for (var index = 0; index < validUnits.Count; index++)
        {
            var unit = validUnits[index];
            var isLast = index == validUnits.Count - 1;
            var quantity = isLast
                ? remaining / unit.Factor
                : decimal.Floor(remaining / unit.Factor);
            if (quantity > 0 || (isLast && parts.Count == 0))
                parts.Add($"{quantity:0.###} {unit.UnitName}");
            remaining -= decimal.Floor(quantity) * unit.Factor;
        }

        return string.Join(" و", parts);
    }
}
