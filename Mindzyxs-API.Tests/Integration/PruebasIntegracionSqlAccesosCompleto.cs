using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class PruebasIntegracionSqlAccesosCompleto : IClassFixture<FabricaAplicacionWebApiSql>
{
    private readonly FabricaAplicacionWebApiSql _factory;
    private readonly HttpClient _client;

    public PruebasIntegracionSqlAccesosCompleto(FabricaAplicacionWebApiSql factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AutorizarYRevocar_PsicologoAprobado()
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
            db.Database.ExecuteSqlRaw($"UPDATE dbo.usuarios SET id_estado_cuenta=1, id_estado_aprobacion=6 WHERE id_usuario={psiId}");
        }

        var verifyPsi = await _client.PostAsJsonAsync("/api/account/verify", new { correo = psiEmail, codigo = psiCode });
        verifyPsi.StatusCode.Should().Be(HttpStatusCode.OK);

        // Re-ensure active/approved via raw SQL
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MindzyxsDbContext>();
            var user = db.Usuarios.First(u => u.Correo == psiEmail);
            db.Database.ExecuteSqlRaw($"UPDATE dbo.usuarios SET id_estado_cuenta=1, id_estado_aprobacion=6 WHERE id_usuario={user.IdUsuario}");
        }

        // Register patient
        var patEmail = $"pat{Guid.NewGuid():N}@test.com";
        var patToken = await AuthHelperSql.RegistrarYVerificarYLoginAsync(_factory, _client, patEmail);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", patToken);

        // Authorize
        var authResp = await _client.PostAsync($"/api/accesos/{psiId}/autorizar", null);
        authResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify persisted
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MindzyxsDbContext>();
            var patId = db.Usuarios.First(u => u.Correo == patEmail).IdUsuario;
            var exists = db.Autorizaciones.Any(a => a.IdPaciente == patId && a.IdPsicologo == psiId);
            exists.Should().BeTrue();
        }

        // Revoke
        var revResp = await _client.PostAsync($"/api/accesos/{psiId}/revocar", null);
        revResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify revoked
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MindzyxsDbContext>();
            var patId = db.Usuarios.First(u => u.Correo == patEmail).IdUsuario;
            var estado = db.Autorizaciones.First(a => a.IdPaciente == patId && a.IdPsicologo == psiId).IdEstado;
            var revocadoId = db.Estados.First(e => e.Nombre == "Revocado").IdEstado;
            estado.Should().Be(revocadoId);
        }
    }
}
