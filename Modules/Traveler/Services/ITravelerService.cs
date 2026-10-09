using TaxRefund.Api.Modules.Traveler.DTOs;

namespace TaxRefund.Api.Modules.Traveler.Services;

public interface ITravelerService
{
    Task<List<TravelerResponse>> GetTravelersAsync(string? passportNumber = null);
    Task<List<TravelerHistoryListResponse>> GetHistoryListAsync();
    Task<TravelerDetailResponse?> GetTravelerAsync(int id);
    Task<TravelerResponse> CreateTravelerAsync(CreateTravelerRequest request);
}
