namespace HardwarePaintShop.Application.Helpers;

/// <summary>
/// Utility for converting product quantities between units.
/// All stock is stored in base units; these helpers do the math.
/// </summary>
public static class UnitConverter
{
    /// <summary>
    /// Converts a quantity from a product unit to the base unit.
    /// </summary>
    /// <param name="quantity">Quantity in the given unit (e.g. 50 bags).</param>
    /// <param name="conversionFactor">How many base units equals 1 of this unit (e.g. 25 kg/bag).</param>
    /// <returns>Quantity in base units (e.g. 1250 kg).</returns>
    /// <exception cref="ArgumentException">Thrown when conversionFactor is invalid.</exception>
    public static decimal ToBase(decimal quantity, decimal conversionFactor)
    {
        ValidateConversionFactor(conversionFactor);
        return quantity * conversionFactor;
    }

    /// <summary>
    /// Converts a base-unit quantity back to a given unit.
    /// </summary>
    /// <param name="baseQuantity">Quantity in the base unit (e.g. 1250 kg).</param>
    /// <param name="conversionFactor">How many base units equals 1 of this unit (e.g. 25 kg/bag).</param>
    /// <returns>Quantity in the target unit (e.g. 50 bags).</returns>
    /// <exception cref="ArgumentException">Thrown when conversionFactor is invalid.</exception>
    public static decimal FromBase(decimal baseQuantity, decimal conversionFactor)
    {
        ValidateConversionFactor(conversionFactor);
        return baseQuantity / conversionFactor;
    }

    /// <summary>
    /// Validates that a conversion factor is a positive non-zero value.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when factor is &lt;= 0.</exception>
    public static void ValidateConversionFactor(decimal conversionFactor)
    {
        if (conversionFactor <= 0)
            throw new ArgumentException(
                $"Conversion factor must be greater than zero. Got: {conversionFactor}",
                nameof(conversionFactor));
    }
}
