namespace Mindzyxs_API.Dtos;

/// <summary>Resultado genérico de una operación de negocio.</summary>
public class OperationResult
{
    /// <summary>Indica si la operación terminó exitosamente.</summary>
    public bool Success { get; set; }

    /// <summary>Mensaje descriptivo del resultado.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Crea un resultado exitoso.</summary>
    public static OperationResult Ok(string message = "OK")
        => new() { Success = true, Message = message };

    /// <summary>Crea un resultado fallido.</summary>
    public static OperationResult Fail(string message)
        => new() { Success = false, Message = message };
}

/// <summary>Resultado genérico de una operación con dato asociado.</summary>
public class OperationResult<T> : OperationResult
{
    /// <summary>Dato devuelto por la operación (si fue exitosa).</summary>
    public T? Data { get; set; }

    /// <summary>Crea un resultado exitoso con dato.</summary>
    public static OperationResult<T> Ok(T data, string message = "OK")
        => new() { Success = true, Message = message, Data = data };

    /// <summary>Crea un resultado fallido.</summary>
    public static new OperationResult<T> Fail(string message)
        => new() { Success = false, Message = message };
}