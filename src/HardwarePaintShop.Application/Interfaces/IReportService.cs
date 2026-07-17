using HardwarePaintShop.Application.Models;
namespace HardwarePaintShop.Application.Interfaces;
public interface IReportService
{
    Task<BusinessReportData> GetBusinessReportAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
}
