using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace Mindzyxs_API.Services;

public class FirebaseOptions
{
    public string ProjectId { get; set; } = "";
    public string DatabaseUrl { get; set; } = "";
}

/// <summary>
/// Chat realtime (PB-31): FirebaseAdmin solo emite custom tokens (Auth sí existe
/// en el SDK .NET; Database NO, por eso RTDB se escribe por REST).
/// Escribe el `meta` de cada conversación (los clientes no pueden por reglas).
/// Los mensajes los escribe el front directo en RTDB; las solicitudes y la
/// auditoría viven en SQL Server. La clave serviceAccountKey.json NUNCA va al
/// repo (GOOGLE_APPLICATION_CREDENTIALS).
/// </summary>
public class FirebaseService(
    IOptions<FirebaseOptions> options,
    ILogger<FirebaseService> logger,
    IHttpClientFactory httpFactory)
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool started;
    private string dbUrl = "";
    private GoogleCredential? credential;

    // Emite el token con el que el front hace signInWithCustomToken (expira en 1h).
    public async Task<string> CreateCustomTokenAsync(string uid, string rol, CancellationToken ct = default)
    {
        await EnsureStartedAsync(ct);
        var claims = new Dictionary<string, object> { ["rol"] = rol };
        return await FirebaseAuth.DefaultInstance.CreateCustomTokenAsync(uid, claims, ct);
    }

    // Escribe meta de la conversación por REST (OAuth2 de la service account). Solo back.
    public async Task WriteMetaAsync(int conversacionId, int idPaciente, int idPsicologo, string estado, int? bloqueador, CancellationToken ct = default)
    {
        await EnsureStartedAsync(ct);
        var sa = credential!.CreateScoped(
            "https://www.googleapis.com/auth/firebase.database",
            "https://www.googleapis.com/auth/userinfo.email").UnderlyingCredential as ServiceAccountCredential
            ?? throw new InvalidOperationException("La credencial de Firebase no es service account.");
        var accessToken = await sa.GetAccessTokenForRequestAsync("https://oauth2.googleapis.com/token", ct);
        var meta = new Dictionary<string, object?>
        {
            ["pacienteUid"] = idPaciente.ToString(),
            ["psicologoUid"] = idPsicologo.ToString(),
            ["estado"] = estado,
            ["bloqueadoPor"] = bloqueador?.ToString(),
            ["updatedAt"] = DateTime.UtcNow.ToString("o")
        };
        var client = httpFactory.CreateClient();
        using var content = JsonContent.Create(meta, options: JsonOpts);
        var url = $"{dbUrl}/conversaciones/{conversacionId}/meta.json?access_token={Uri.EscapeDataString(accessToken)}";
        using var resp = await client.PutAsync(url, content, ct);
        resp.EnsureSuccessStatusCode();
        logger.LogInformation("RTDB meta escrita para conversación {Id} (estado {Estado})", conversacionId, estado);
    }

    private async Task EnsureStartedAsync(CancellationToken ct)
    {
        if (started && FirebaseApp.DefaultInstance != null) return;
        await gate.WaitAsync(ct);
        try
        {
            if (started && FirebaseApp.DefaultInstance != null) return;
            var opt = options.Value;
            if (string.IsNullOrWhiteSpace(opt.DatabaseUrl))
                throw new InvalidOperationException("Firebase:DatabaseUrl no configurada en appsettings.");
            var keyPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
            if (string.IsNullOrWhiteSpace(keyPath))
                throw new InvalidOperationException("GOOGLE_APPLICATION_CREDENTIALS no definida (ruta al serviceAccountKey.json).");
            dbUrl = opt.DatabaseUrl.Trim().TrimEnd('/');
#pragma warning disable CS0618 // FromFile obsoleto pero funcional; alternativa: CredentialFactory
            credential = GoogleCredential.FromFile(keyPath);
#pragma warning restore CS0618
            FirebaseApp.Create(new AppOptions
            {
                Credential = credential,
                ProjectId = string.IsNullOrWhiteSpace(opt.ProjectId) ? null : opt.ProjectId.Trim()
            });
            started = true;
            logger.LogInformation("Firebase Admin iniciado (proyecto {Proyecto})", opt.ProjectId);
        }
        finally
        {
            gate.Release();
        }
    }
}
