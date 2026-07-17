namespace HardwarePaintShop.Application.Models;
public sealed record LicenseStatus(
    bool IsValid, bool IsTrial, string MachineId, string CustomerName,
    string Edition, DateTime? ExpiresAt, int DaysRemaining, string Message);
