using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Tests.Integration;

namespace Mindzyxs_API.Tests.Integration;

public static class AuthHelperSql
{
    public static async Task<string> RegistrarYVerificarYLoginAsync(FabricaAplicacionWebApiSql factory, HttpClient client, string email, string password = "Abc12345", string tipo = "Paciente", SqlTestCleanup.TestScope scope = null)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(tipo), "TipoUsuario");
        content.Add(new StringContent("Test"), "Nombre");
        content.Add(new StringContent("User"), "Apellido");
        content.Add(new StringContent(email), "Correo");
        content.Add(new StringContent(password), "Password");
        content.Add(new StringContent("2000-01-01"), "FechaNacimiento");
        content.Add(new StringContent("1"), "IdGenero");

        var reg = await client.PostAsync("/api/account/register", content);
        reg.EnsureSuccessStatusCode();

        int userId;
        string code;
        int? codigoId = null;
        using (var scopeDb = factory.Services.CreateScope())
        {
            var db = scopeDb.ServiceProvider.GetRequiredService<MindzyxsDbContext>();
            var user = db.Usuarios.First(u => u.Correo == email);
            userId = user.IdUsuario;
            var c = db.Codigos.First(x => x.IdUsuario == userId && x.Tipo == "Registro");
            code = c.Codigo;
            codigoId = c.IdCodigo;
            if (scope != null)
            {
                scope.UserIds.Add(userId);
                scope.CodigoIds.Add(codigoId.Value);
            }
        }

        var verify = await client.PostAsJsonAsync("/api/account/verify", new { correo = email, codigo = code });
        verify.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/account/login", new { correo = email, password });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(body);

        // Register session ID
        using (var scopeDb = factory.Services.CreateScope())
        {
            var db = scopeDb.ServiceProvider.GetRequiredService<MindzyxsDbContext>();
            var user = db.Usuarios.First(u => u.Correo == email);
            var sesion = db.Sesiones.FirstOrDefault(s => s.IdUsuario == user.IdUsuario);
            if (sesion != null && scope != null)
            {
                scope.SesionIds.Add(sesion.IdSesion);
            }
        }

        return doc.RootElement.GetProperty("token").GetString()!;
    }

    public static async Task<string> RegistrarPsicologoYActivarAsync(FabricaAplicacionWebApiSql factory, HttpClient client, string email, string password = "Abc12345")
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent("Psicologo"), "TipoUsuario");
        content.Add(new StringContent("Psi"), "Nombre");
        content.Add(new StringContent("Test"), "Apellido");
        content.Add(new StringContent(email), "Correo");
        content.Add(new StringContent(password), "Password");
        content.Add(new StringContent("1990-01-01"), "FechaNacimiento");
        content.Add(new StringContent("2"), "IdGenero");
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };
        var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "TarjetaProfesional", "tarjeta.pdf");

        var reg = await client.PostAsync("/api/account/register", content);
        reg.EnsureSuccessStatusCode();

        int userId;
        string code;
        using (var scopeDb = factory.Services.CreateScope())
        {
            var db = scopeDb.ServiceProvider.GetRequiredService<MindzyxsDbContext>();
            var user = db.Usuarios.First(u => u.Correo == email);
            userId = user.IdUsuario;
            code = db.Codigos.First(c => c.IdUsuario == userId && c.Tipo == "Registro").Codigo;
            db.Database.ExecuteSqlRaw($"UPDATE dbo.usuarios SET id_estado_cuenta=1, id_estado_aprobacion=6 WHERE id_usuario={userId}");
        }

        var verify = await client.PostAsJsonAsync("/api/account/verify", new { correo = email, codigo = code });
        verify.EnsureSuccessStatusCode();

        using (var scopeDb = factory.Services.CreateScope())
        {
            var db = scopeDb.ServiceProvider.GetRequiredService<MindzyxsDbContext>();
            db.Database.ExecuteSqlRaw($"UPDATE dbo.usuarios SET id_estado_cuenta=1, id_estado_aprobacion=6 WHERE id_usuario={userId}");
        }

        var login = await client.PostAsJsonAsync("/api/account/login", new { correo = email, password });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("token").GetString()!;
    }

    public static HttpClient ConToken(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
