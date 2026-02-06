namespace Application.Common.Models;

public class ServiceResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; }

    public static ServiceResult<T> SuccessResult(T data, string message = "Success", int statusCode = 200)
    {
        return new ServiceResult<T> { Success = true, Data = data, Message = message, StatusCode = statusCode };
    }

    public static ServiceResult<T> Failure(string message, int statusCode = 400)
    {
        return new ServiceResult<T> { Success = false, Message = message, StatusCode = statusCode };
    }
}
