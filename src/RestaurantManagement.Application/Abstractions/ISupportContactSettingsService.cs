using RestaurantManagement.Application.Models.Settings;

namespace RestaurantManagement.Application.Abstractions;

public interface ISupportContactSettingsService
{
    Task<SupportContactSettingsDto> GetAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(
        SupportContactSettingsDto settings,
        CancellationToken cancellationToken = default);
}
