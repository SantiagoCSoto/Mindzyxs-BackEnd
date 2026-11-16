using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

[Collection("SqlIntegration")]
public class PruebasIntegracionSqlFlujoCompleto : IClassFixture<FabricaAplicacionWebApiSql>, IDisposable
{
    private readonly FabricaAplicacionWebApiSql _factory;
    private readonly HttpClient _client;
    private readonly SqlTestCleanup.TestScope _scope;

    public PruebasIntegracionSqlFlujoCompleto(FabricaAplicacionWebApiSql factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _scope = new SqlTestCleanup.TestScope();
    }

    public void Dispose()
    {
        // DryRun only - no actual deletion
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MindzyxsDbContext>();
        var result = SqlTestCleanup.DryRun(db, _scope);
        // Log dry run result for verification
        System.Diagnostics.Debug.WriteLine($"DryRun: Safe={result.Safe}, Users={result.UserIds.Count}, Sessions={result.SesionIds.Count}");
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task FlujoCompleto_RegistroVerificacionLogin()
    {
        var email = $"flujo{Guid.NewGuid():N}@test.com";
        var content = new MultipartFormDataContent();
        content.Add(new StringContent("Paciente"), "TipoUsuario");
        content.Add(new StringContent("Flujo"), "Nombre");
        content.Add(new StringContent("Test"), "Apellido");
        content.Add(new StringContent(email), "Correo");
        content.Add(new StringContent("Abc12345"), "Password");
        content.Add(new StringContent("2000-01-01"), "FechaNacimiento");
        content.Add(new StringContent("1"), "IdGenero");

        var regResp = await _client.PostAsync("/api/account/register", content);
        regResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // Read code from DB
        string code;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MindzyxsDbContext>();
            var row = db.Codigos.OrderByDescending(c => c.IdCodigo).FirstOrDefault(c => c.Tipo == "Registro");
            // find by user email
            var user = db.Usuarios.FirstOrDefault(u => u.Correo == email) ?? throw new InvalidOperationException("user not found");
            var c = db.Codigos.FirstOrDefault(x => x.IdUsuario == user.IdUsuario && x.Tipo == "Registro") ?? throw new InvalidOperationException("code not found");
            code = c.Codigo;
            _scope.UserIds.Add(user.IdUsuario);
            _scope.CodigoIds.Add(c.IdCodigo);
        }

        var verifyResp = await _client.PostAsJsonAsync("/api/account/verify", new { correo = email, codigo = code });
        verifyResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResp = await _client.PostAsJsonAsync("/api/account/login", new { correo = email, password = "Abc12345" });
        loginResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await loginResp.Content.ReadAsStringAsync();
        body.Should().Contain("token");

        // Register session ID
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MindzyxsDbContext>();
            var user = db.Usuarios.First(u => u.Correo == email);
            var sesion = db.Sesiones.FirstOrDefault(s => s.IdUsuario == user.IdUsuario);
            if (sesion != null) _scope.SesionIds.Add(sesion.IdSesion);
        }
    }
}



