using Microsoft.Extensions.Options;

namespace Mindzyxs_API.Utilities;

public class StorageOptions
{
    public string Provider { get; set; } = "FileSystem";
    public string TarjetasPath { get; set; } = @"C:\uploads\mindzyxs\tarjetas";
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "tarjetas-profesionales";
}

// HU3-PB1: solo PDF, máximo 5MB. Se guarda en disco y en dbo.usuarios.file_path va la ruta.
public interface ITarjetaStorage
{
    string? Validate(IFormFile? file, bool required, out string? error);
    Task<string> SaveAsync(IFormFile file, int userId, CancellationToken ct = default);
    Task<Stream> OpenReadAsync(string storageId, CancellationToken ct = default);
    Task DeleteAsync(string storageId, CancellationToken ct = default);
    bool IsLegacyPath(string storageId);
}

public class FileSystemTarjetaStorage(IOptions<StorageOptions> options) : ITarjetaStorage
{
    private const long MaxBytes = 5 * 1024 * 1024;

    public string? Validate(IFormFile? file, bool required, out string? error)
    {
        error = null;
        if (file is null || file.Length == 0)
        {
            if (required) error = "La tarjeta profesional es obligatoria para registrarse como psicólogo";
            return error;
        }
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".pdf" || !string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            error = "El archivo debe estar en formato PDF";
        else if (file.Length > MaxBytes)
            error = "El archivo supera el tamaño máximo permitido de 5MB";
        return error;
    }

    public async Task<string> SaveAsync(IFormFile file, int userId, CancellationToken ct = default)
    {
        var root = options.Value.TarjetasPath;
        Directory.CreateDirectory(root);
        var name = $"psy_{userId}_{Guid.NewGuid():N}.pdf";
        var full = Path.Combine(root, name);

        await using (var fs = new FileStream(full, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            await file.CopyToAsync(fs, ct);

        // Verificación mínima de firma PDF (%PDF) tras guardar.
        await using (var fs = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var header = new byte[4];
            if (await fs.ReadAsync(header, ct) < 4 || header[0] != 0x25 || header[1] != 0x50)
            {
                File.Delete(full);
                throw new InvalidOperationException("El archivo debe estar en formato PDF");
            }
        }
        return full;
    }

    public Task<Stream> OpenReadAsync(string storageId, CancellationToken ct = default)
    {
        if (!File.Exists(storageId))
            throw new FileNotFoundException("No se ha adjuntado tarjeta profesional.", storageId);
        return Task.FromResult<Stream>(new FileStream(storageId, FileMode.Open, FileAccess.Read, FileShare.Read));
    }

    public Task DeleteAsync(string storageId, CancellationToken ct = default)
    {
        try
        {
            if (File.Exists(storageId)) File.Delete(storageId);
        }
        catch
        {
            // Best-effort: no bloquear el reemplazo por fallos de limpieza.
        }
        return Task.CompletedTask;
    }

    public bool IsLegacyPath(string storageId)
        => !string.IsNullOrWhiteSpace(storageId) && (storageId.Contains('\\') || storageId.Contains('/') || Path.IsPathRooted(storageId));
}
