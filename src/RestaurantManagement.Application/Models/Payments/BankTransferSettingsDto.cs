namespace RestaurantManagement.Application.Models.Payments;

public sealed record BankTransferSettingsDto(
    string BankCode,
    string BankName,
    string AccountNumber,
    string AccountName,
    string? Branch)
{
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(BankCode) &&
        !string.IsNullOrWhiteSpace(BankName) &&
        !string.IsNullOrWhiteSpace(AccountNumber) &&
        !string.IsNullOrWhiteSpace(AccountName);
}
