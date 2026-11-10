using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;

namespace Mindzyxs_API.Utilities;

public class AzureBlobTarjetaStorage(IOptions<StorageOptions> options, ILogger<AzureBlobTarjetaStorage> logger) : ITarjetaStorage
{
    private const long MaxBytes = 5 * 1024 * 1024;

    private BlobContainerClient GetContainer()
    {
        var o = options.Value;
        if (string.IsNullOrWhiteSpace(o.ConnectionString))
            throw new InvalidOperationException("Storage:ConnectionString no configurada.");
        if (string.IsNullOrWhiteSpace(o.ContainerName))
            throw new InvalidOperationException("Storage:ContainerName no configurada.");
        var service = new BlobServiceClient(o.ConnectionString);
        return service.GetBlobContainerClient(o.ContainerName);
    }

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
        var name = $"psy_{userId}_{Guid.NewGuid():N}.pdf";

        await using var input = file.OpenReadStream();
        var header = new byte[4];
        var read = await input.ReadAsync(header, ct);
        if (read < 4 || header[0] != 0x25 || header[1] != 0x50)
            throw new InvalidOperationException("El archivo debe estar en formato PDF");
        if (input.CanSeek) input.Position = 0;

        var container = GetContainer();
        await container.CreateIfNotExistsAsync(cancellationToken: ct);
        var blob = container.GetBlobClient(name);
        await blob.UploadAsync(input, overwrite: false, cancellationToken: ct);

        logger.LogInformation("Tarjeta guardada en Blob Storage: {Blob}", name);
        return name;
    }

    public async Task<Stream> OpenReadAsync(string storageId, CancellationToken ct = default)
    {
        if (IsLegacyPath(storageId))
        {
            if (!File.Exists(storageId))
                throw new FileNotFoundException("No se ha adjuntado tarjeta profesional.", storageId);
            return new FileStream(storageId, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        var container = GetContainer();
        var blob = container.GetBlobClient(storageId);
        if (!await blob.ExistsAsync(ct))
            throw new FileNotFoundException("No se ha adjuntado tarjeta profesional.", storageId);
        return await blob.OpenReadAsync(cancellationToken: ct);
    }

    public async Task DeleteAsync(string storageId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(storageId)) return;
        try
        {
            if (IsLegacyPath(storageId))
            {
                if (File.Exists(storageId)) File.Delete(storageId);
                return;
            }
            var container = GetContainer();
            var blob = container.GetBlobClient(storageId);
            await blob.DeleteIfExistsAsync(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No fue posible eliminar la tarjeta {StorageId}", storageId);
        }
    }

    public bool IsLegacyPath(string storageId)
        => !string.IsNullOrWhiteSpace(storageId) && (storageId.Contains('\\') || storageId.Contains('/') || Path.IsPathRooted(storageId));
}
