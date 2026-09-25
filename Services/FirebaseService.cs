using FirebaseAdmin;
using FirebaseAdmin.Auth;
using FirebaseAdmin.Database;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;

namespace Mindzyxs_API.Services;

public class FirebaseOptions
{
    public string ProjectId { get; set; } = "";
    public string DatabaseUrl { get; set; } = "";
}

/// <summary>
/// Chat realtime (PB-31): Admin SDK de Firebase. Emite custom tokens para el
/// front (uid = id_usuario, claim rol) y escribe el `meta` de cada conversación
/// (los clientes no pueden escribirlo por reglas). Los mensajes los escribe el
/// front directo en RTDB; las solicitudes y la auditoría viven en SQL Server.
/// La clave serviceAccountKey.json NUNCA va al repo (GOOGLE_APPLICATION_CREDENTIALS).
/// </summary>
public class FirebaseService(IOptions<FirebaseOptions> options, ILogger<FirebaseService> logger)
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool started;
    private string dbUrl = "";

    // Emite el token con el que el front hace signInWithCustomToken (expira en 1h).
    public async Task<string> CreateCustomTokenAsync(string uid, string rol, CancellationToken ct = default)
    {
        var app = await EnsureStartedAsync(ct);
        _ = app;
        var claims = new Dictionary<string, object> { ["rol"] = rol };
        return await FirebaseAuth.DefaultInstance.CreateCustomTokenAsync(uid, claims, ct);
    }

    // Escribe meta de la conversación (participantes, estado, bloqueo). Solo back.
    public async Task WriteMetaAsync(int conversacionId, int idPaciente, int idPsicologo, string estado, int? bloqueador, CancellationToken ct = default)
    {
        await EnsureStartedAsync(ct);
        var db = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance, dbUrl);
        var meta = new Dictionary<string, object?>
        {
            ["pacienteUid"] = idPaciente.ToString(),
            ["psicologoUid"] = idPsicologo.ToString(),
            ["estado"] = estado,
            ["bloqueadoPor"] = bloqueador?.ToString(),
            ["updatedAt"] = ServerValue.Timestamp
        };
        await db.GetReference($"conversaciones/{conversacionId}/meta").SetValueAsync(meta);
        logger.LogInformation("RTDB meta escrita para conversación {Id} (estado {Estado})", conversacionId, estado);
    }

    private async Task<FirebaseApp> EnsureStartedAsync(CancellationToken ct)
    {
        if (started && FirebaseApp.DefaultInstance != null) return FirebaseApp.DefaultInstance;
        await gate.WaitAsync(ct);
        try
        {
            if (started && FirebaseApp.DefaultInstance != null) return FirebaseApp.DefaultInstance;
            var opt = options.Value;
            if (string.IsNullOrWhiteSpace(opt.DatabaseUrl))
                throw new InvalidOperationException("Firebase:DatabaseUrl no configurada en appsettings.");
            var keyPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
            if (string.IsNullOrWhiteSpace(keyPath))
                throw new InvalidOperationException("GOOGLE_APPLICATION_CREDENTIALS no definida (ruta al serviceAccountKey.json).");
            dbUrl = opt.DatabaseUrl.Trim();
            var app = FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(keyPath),
                ProjectId = string.IsNullOrWhiteSpace(opt.ProjectId) ? null : opt.ProjectId.Trim()
            });
            started = true;
            logger.LogInformation("Firebase Admin iniciado (proyecto {Proyecto})", opt.ProjectId);
            return app;
        }
        finally
        {
            gate.Release();
        }
    }
}
