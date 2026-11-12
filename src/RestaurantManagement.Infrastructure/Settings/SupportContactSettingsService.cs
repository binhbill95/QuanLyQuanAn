using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RestaurantManagement.Application.Abstractions;
using RestaurantManagement.Application.Models.Settings;
using RestaurantManagement.Domain.Entities;
using RestaurantManagement.Infrastructure.Persistence;

namespace RestaurantManagement.Infrastructure.Settings;

public sealed class SupportContactSettingsService : ISupportContactSettingsService
{
    private const string HotlineKey = "SupportHotline";
    private const string ZaloUrlKey = "SupportZaloUrl";
    private const string CacheKey = "SupportContactSettings";

    private readonly RestaurantDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMemoryCache _cache;

    public SupportContactSettingsService(
        RestaurantDbContext db,
        ICurrentUserService currentUser,
        IMemoryCache cache)
    {
        _db = db;
        _currentUser = currentUser;
        _cache = cache;
    }

    public async Task<SupportContactSettingsDto> GetAsync(
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out SupportContactSettingsDto? cachedSettings))
        {
            return cachedSettings!;
        }

        var values = await _db.SystemParameters
            .AsNoTracking()
            .Where(parameter => !parameter.IsDeleted &&
                                (parameter.ParamKey == HotlineKey ||
                                 parameter.ParamKey == ZaloUrlKey))
            .ToDictionaryAsync(parameter => parameter.ParamKey, parameter => parameter.ParamValue, cancellationToken);

        var settings = new SupportContactSettingsDto(
            GetValue(values, HotlineKey),
            GetValue(values, ZaloUrlKey));
        _cache.Set(CacheKey, settings, TimeSpan.FromMinutes(5));
        return settings;
    }

    public async Task SaveAsync(
        SupportContactSettingsDto settings,
        CancellationToken cancellationToken = default)
    {
        var values = new Dictionary<string, (string Value, string Description)>
        {
            [HotlineKey] = (settings.HotlineNumber.Trim(), "Số điện thoại hotline hỗ trợ"),
            [ZaloUrlKey] = (settings.ZaloUrl.Trim(), "Đường dẫn Zalo hỗ trợ"),
        };
        var keys = values.Keys.ToArray();
        var parameters = await _db.SystemParameters
            .Where(parameter => keys.Contains(parameter.ParamKey))
            .ToDictionaryAsync(parameter => parameter.ParamKey, cancellationToken);
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
        _cache.Remove(CacheKey);
    }

    private static string GetValue(IReadOnlyDictionary<string, string> values, string key)
        => values.TryGetValue(key, out var value) ? value : string.Empty;
}
