namespace RestaurantManagement.Web.Helpers;

/// <summary>
/// Nhãn hiển thị cho các giá trị tinyint trong CSDL (Employee.Gender, Employee.Status…).
/// </summary>
public static class EmployeeDisplay
{
    public static string StatusLabel(byte status) => status switch
    {
        0 => "Đang làm",
        1 => "Tạm nghỉ",
        2 => "Đã nghỉ",
        _ => "Không xác định",
    };

    public static string StatusBadgeClass(byte status) => status switch
    {
        0 => "bg-success",
        1 => "bg-warning text-dark",
        2 => "bg-secondary",
        _ => "bg-light text-dark",
    };

    public static string GenderLabel(byte? gender) => gender switch
    {
        0 => "Nữ",
        1 => "Nam",
        2 => "Khác",
        _ => "—",
    };
}
