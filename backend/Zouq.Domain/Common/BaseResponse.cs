namespace Zouq.Domain.Common;

public class ApiResponse<T>
{
    public string Status { get; set; } = "success";
    public string? Message { get; set; }
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() { Status = "success", Message = message, Data = data };

    public static ApiResponse<T> Fail(string message) =>
        new() { Status = "error", Message = message, Data = default };
}

public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
}

public abstract class AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
}
