using HardwarePaintShop.Application.Models;
namespace HardwarePaintShop.Application.Interfaces;
public interface ILicenseService
{
    string GetMachineId();
    Task<LicenseStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<LicenseStatus> ActivateAsync(string licenseCode, CancellationToken cancellationToken = default);
}
