namespace Mindzyxs_API.Dtos;

public class OperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    public static OperationResult Ok(string message = "OK")
        => new() { Success = true, Message = message };

    public static OperationResult Fail(string message)
        => new() { Success = false, Message = message };
}

public class OperationResult<T> : OperationResult
{
    public T? Data { get; set; }

    public static OperationResult<T> Ok(T data, string message = "OK")
        => new() { Success = true, Message = message, Data = data };

    public static new OperationResult<T> Fail(string message)
        => new() { Success = false, Message = message };
}
