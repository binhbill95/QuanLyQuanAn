namespace RestaurantManagement.Application.Models.Settings;

public sealed record SupportContactSettingsDto(
    string HotlineNumber,
    string ZaloUrl)
{
    public bool HasHotline => !string.IsNullOrWhiteSpace(HotlineNumber);

    public bool HasZalo => !string.IsNullOrWhiteSpace(ZaloUrl);
}
