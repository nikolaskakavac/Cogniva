using Cogniva.Api.DTOs.Dashboard;

namespace Cogniva.Api.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardResponse> GetAsync(Guid userId, CancellationToken cancellationToken = default);
}
