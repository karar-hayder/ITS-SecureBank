namespace Application.Common.Models;

public class ServiceResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }

    public static ServiceResult<T> Ok(T data) => new() { Success = true, Data = data };
    public static ServiceResult<T> Fail(string message, List<string>? errors = null) => new() { Success = false, Message = message, Errors = errors };
}
