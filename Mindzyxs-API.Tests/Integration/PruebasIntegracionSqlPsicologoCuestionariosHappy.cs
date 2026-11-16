using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mindzyxs_API.Repositories.Models;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class PruebasIntegracionSqlPsicologoCuestionariosHappy : IClassFixture<FabricaAplicacionWebApiSql>
{
    private readonly FabricaAplicacionWebApiSql _factory;
    private readonly HttpClient _client;

    public PruebasIntegracionSqlPsicologoCuestionariosHappy(FabricaAplicacionWebApiSql factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Listar_Autenticado_ReturnsOk()
    {
        var email = $"psi{Guid.NewGuid():N}@test.com";
        var token = await RegistrarPsicologoAprobadoAsync(_factory, _client, email);
        _client.ConToken(token);

        var response = await _client.GetAsync("/api/psicologo/cuestionarios");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Crear_Autenticado_ReturnsOk()
    {
        var email = $"psi{Guid.NewGuid():N}@test.com";
        var token = await RegistrarPsicologoAprobadoAsync(_factory, _client, email);
        _client.ConToken(token);

        var dto = new
        {
            titulo = "Test Cuest",
            preguntas = new[]
            {
                new { texto = "P1", opciones = new[] { new { texto = "O1", valor = 1 }, new { texto = "O2", valor = 2 } } }
            }
        };
        var response = await _client.PostAsJsonAsync("/api/psicologo/cuestionarios", dto);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<string> RegistrarPsicologoAprobadoAsync(FabricaAplicacionWebApiSql factory, HttpClient client, string email)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent("Psicologo"), "TipoUsuario");
        content.Add(new StringContent("Psi"), "Nombre");
        content.Add(new StringContent("Test"), "Apellido");
        content.Add(new StringContent(email), "Correo");
        content.Add(new StringContent("Abc12345"), "Password");
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
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MindzyxsDbContext>();
            var user = db.Usuarios.First(u => u.Correo == email);
            userId = user.IdUsuario;
            code = db.Codigos.First(c => c.IdUsuario == userId && c.Tipo == "Registro").Codigo;
            db.Database.ExecuteSqlRaw("UPDATE dbo.usuarios SET id_estado_cuenta=1, id_estado_aprobacion=6 WHERE id_usuario = @p0", new Microsoft.Data.SqlClient.SqlParameter("@p0", userId));
        }

        var verify = await client.PostAsJsonAsync("/api/account/verify", new { correo = email, codigo = code });
        verify.EnsureSuccessStatusCode();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MindzyxsDbContext>();
            db.Database.ExecuteSqlRaw("UPDATE dbo.usuarios SET id_estado_cuenta=1, id_estado_aprobacion=6 WHERE id_usuario = @p0", new Microsoft.Data.SqlClient.SqlParameter("@p0", userId));
        }

        var login = await client.PostAsJsonAsync("/api/account/login", new { correo = email, password = "Abc12345" });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("token").GetString()!;
    }
}
