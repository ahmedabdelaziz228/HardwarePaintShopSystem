using HardwarePaintShop.Domain.Enums;

namespace HardwarePaintShop.Application.Helpers;

/// <summary>
/// Utility for common invoice calculations.
/// Keeps financial math in one place and away from UI code.
/// </summary>
public static class InvoiceCalculator
{
    /// <summary>
    /// Calculates the line total for an invoice line item.
    /// Formula: (quantity × unitPrice) - discount
    /// </summary>
    /// <param name="quantity">Number of units sold/purchased.</param>
    /// <param name="unitPrice">Price per unit.</param>
    /// <param name="discountAmount">Flat discount amount for this line (not percentage).</param>
    /// <returns>Line total after discount (floored at 0).</returns>
    public static decimal CalculateLineTotal(decimal quantity, decimal unitPrice, decimal discountAmount)
    {
        var gross = quantity * unitPrice;
        var net = gross - discountAmount;
        return net < 0 ? 0 : net;
    }

    /// <summary>
    /// Calculates the remaining (unpaid) amount on an invoice.
    /// </summary>
    /// <param name="totalAmount">Total invoice amount.</param>
    /// <param name="paidAmount">Amount already paid.</param>
    /// <returns>Remaining balance (floored at 0).</returns>
    public static decimal CalculateRemaining(decimal totalAmount, decimal paidAmount)
    {
        var remaining = totalAmount - paidAmount;
        return remaining < 0 ? 0 : remaining;
    }

    /// <summary>
    /// Determines the payment status based on total and paid amounts.
    /// </summary>
    /// <param name="totalAmount">Total invoice amount.</param>
    /// <param name="paidAmount">Amount already paid.</param>
    /// <returns>
    /// <see cref="PaymentStatus.Paid"/> when paidAmount >= totalAmount,
    /// <see cref="PaymentStatus.Partial"/> when paidAmount > 0 but less than total,
    /// <see cref="PaymentStatus.Unpaid"/> when paidAmount is 0.
    /// </returns>
    public static PaymentStatus DeterminePaymentStatus(decimal totalAmount, decimal paidAmount)
    {
        if (paidAmount <= 0)
            return PaymentStatus.Unpaid;

        if (paidAmount >= totalAmount)
            return PaymentStatus.Paid;

        return PaymentStatus.Partial;
    }
}
