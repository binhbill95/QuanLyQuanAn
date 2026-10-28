namespace RestaurantManagement.Application.Common;

/// <summary>
/// Kết quả xử lý của tầng Application/Infrastructure.
/// Dùng cho các thao tác ghi (tạo/sửa/xóa) để controller hiển thị lỗi nghiệp vụ.
/// </summary>
public sealed class OperationResult<T>
{
    private OperationResult(bool succeeded, T? value, string? error)
    {
        Succeeded = succeeded;
        Value = value;
        Error = error;
    }

    public bool Succeeded { get; }

    public T? Value { get; }

    public string? Error { get; }

    public static OperationResult<T> Success(T value) => new(true, value, null);

    public static OperationResult<T> Failure(string error) => new(false, default, error);
}
