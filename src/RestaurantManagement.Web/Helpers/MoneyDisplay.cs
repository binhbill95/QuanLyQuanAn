using System.Globalization;

namespace RestaurantManagement.Web.Helpers;

public static class MoneyDisplay
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    public static string Format(decimal value)
        => value.ToString("N0", Vietnamese) + " đ";
}
