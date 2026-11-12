using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Application.Models.Payments;
using RestaurantManagement.Domain.Entities;
using RestaurantManagement.Infrastructure.Persistence;

namespace RestaurantManagement.Infrastructure.Payments;

public sealed class BankTransferSettingsService : IBankTransferSettingsService
{
    private const string BankCodeKey = "TransferBankCode";
    private const string BankNameKey = "TransferBankName";
    private const string AccountNumberKey = "TransferAccountNumber";
    private const string AccountNameKey = "TransferAccountName";
    private const string BranchKey = "TransferBranch";

    private readonly RestaurantDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public BankTransferSettingsService(RestaurantDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<BankTransferSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var values = await _db.SystemParameters
            .AsNoTracking()
            .Where(p => !p.IsDeleted &&
                        (p.ParamKey == BankCodeKey ||
                         p.ParamKey == BankNameKey ||
                         p.ParamKey == AccountNumberKey ||
                         p.ParamKey == AccountNameKey ||
                         p.ParamKey == BranchKey))
            .ToDictionaryAsync(p => p.ParamKey, p => p.ParamValue, cancellationToken);

        return new BankTransferSettingsDto(
            GetValue(values, BankCodeKey),
            GetValue(values, BankNameKey),
            GetValue(values, AccountNumberKey),
            GetValue(values, AccountNameKey),
            GetValue(values, BranchKey));
    }

    public async Task SaveAsync(
        BankTransferSettingsDto settings,
        CancellationToken cancellationToken = default)
    {
        var values = new Dictionary<string, (string Value, string Description)>
        {
            [BankCodeKey] = (settings.BankCode.Trim(), "Mã BIN ngân hàng dùng tạo VietQR"),
            [BankNameKey] = (settings.BankName.Trim(), "Tên ngân hàng nhận chuyển khoản"),
            [AccountNumberKey] = (settings.AccountNumber.Trim(), "Số tài khoản nhận chuyển khoản"),
            [AccountNameKey] = (settings.AccountName.Trim(), "Tên chủ tài khoản nhận chuyển khoản"),
            [BranchKey] = (settings.Branch?.Trim() ?? string.Empty, "Chi nhánh ngân hàng"),
        };
        var keys = values.Keys.ToArray();
        var parameters = await _db.SystemParameters
            .Where(p => keys.Contains(p.ParamKey))
            .ToDictionaryAsync(p => p.ParamKey, cancellationToken);
        var now = DateTime.UtcNow;

        foreach (var (key, value) in values)
        {
            if (parameters.TryGetValue(key, out var parameter))
            {
                parameter.ParamValue = value.Value;
                parameter.Description = value.Description;
                parameter.IsDeleted = false;
                parameter.DeletedAt = null;
                parameter.UpdatedAt = now;
                parameter.UpdatedBy = _currentUser.UserId;
                continue;
            }

            _db.SystemParameters.Add(new SystemParameter
            {
                ParamKey = key,
                ParamValue = value.Value,
                DataType = "string",
                Description = value.Description,
                CreatedAt = now,
                CreatedBy = _currentUser.UserId,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string GetValue(IReadOnlyDictionary<string, string> values, string key)
        => values.TryGetValue(key, out var value) ? value : string.Empty;
}
