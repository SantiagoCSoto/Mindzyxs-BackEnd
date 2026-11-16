using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class PruebasIntegracionSqlAdminAprobacion : IClassFixture<FabricaAplicacionWebApiSql>
{
    private readonly FabricaAplicacionWebApiSql _factory;
    private readonly HttpClient _client;

    public PruebasIntegracionSqlAdminAprobacion(FabricaAplicacionWebApiSql factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AprobarPsicologo_Endpoint_ReturnsOkYPersiste()
    {
        // Register psicologo
        var psiEmail = $"psi{Guid.NewGuid():N}@test.com";
        var psiContent = new MultipartFormDataContent();
        psiContent.Add(new StringContent("Psicologo"), "TipoUsuario");
        psiContent.Add(new StringContent("Psi"), "Nombre");
        psiContent.Add(new StringContent("Test"), "Apellido");
        psiContent.Add(new StringContent(psiEmail), "Correo");
        psiContent.Add(new StringContent("Abc12345"), "Password");
        psiContent.Add(new StringContent("1990-01-01"), "FechaNacimiento");
        psiContent.Add(new StringContent("2"), "IdGenero");
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };
        var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        psiContent.Add(fileContent, "TarjetaProfesional", "tarjeta.pdf");

        var regPsi = await _client.PostAsync("/api/account/register", psiContent);
        regPsi.StatusCode.Should().Be(HttpStatusCode.Created);

        int psiId;
        string psiCode;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MindzyxsDbContext>();
            var user = db.Usuarios.First(u => u.Correo == psiEmail);
            psiId = user.IdUsuario;
            psiCode = db.Codigos.First(c => c.IdUsuario == psiId && c.Tipo == "Registro").Codigo;
        }

        var verifyPsi = await _client.PostAsJsonAsync("/api/account/verify", new { correo = psiEmail, codigo = psiCode });
        verifyPsi.StatusCode.Should().Be(HttpStatusCode.OK);

        // Login as admin
        var adminLogin = await _client.PostAsJsonAsync("/api/account/login", new { correo = "admin@test.com", password = "Abc12345" });
        var adminBody = await adminLogin.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(adminBody);
        var adminToken = doc.RootElement.GetProperty("token").GetString()!;
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        // Approve psicologo
        var approve = await _client.PostAsync($"/api/admin/psicologos/{psiId}/aprobar", null);
        var approveBody = await approve.Content.ReadAsStringAsync();
        approve.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify persisted
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MindzyxsDbContext>();
            var user = db.Usuarios.First(u => u.IdUsuario == psiId);
            var estadoActiva = db.Estados.First(e => e.Nombre == "Activa").IdEstado;
            var estadoAprobado = db.Estados.First(e => e.Nombre == "Aprobado").IdEstado;
            user.IdEstadoCuenta.Should().Be(estadoActiva);
            user.IdEstadoAprobacion.Should().Be(estadoAprobado);
        }

        // Login as psicologo
        var psiLogin = await _client.PostAsJsonAsync("/api/account/login", new { correo = psiEmail, password = "Abc12345" });
        psiLogin.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
