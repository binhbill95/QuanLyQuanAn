using RestaurantManagement.Application.Models.Payments;

namespace RestaurantManagement.Application.Abstractions;

public interface IBankTransferSettingsService
{
    Task<BankTransferSettingsDto> GetAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(
        BankTransferSettingsDto settings,
        CancellationToken cancellationToken = default);
}
