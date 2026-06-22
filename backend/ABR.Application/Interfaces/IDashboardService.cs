using ABR.Application.DTOs.Dashboard;

namespace ABR.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(Guid siteId, CancellationToken cancellationToken = default);
}
