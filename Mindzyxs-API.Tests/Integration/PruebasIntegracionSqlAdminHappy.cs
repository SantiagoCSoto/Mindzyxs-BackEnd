using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class PruebasIntegracionSqlAdminHappy : IClassFixture<FabricaAplicacionWebApiSql>
{
    private readonly FabricaAplicacionWebApiSql _factory;
    private readonly HttpClient _client;

    public PruebasIntegracionSqlAdminHappy(FabricaAplicacionWebApiSql factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListBackups_Autenticado_ReturnsOk()
    {
        var login = await _client.PostAsJsonAsync("/api/account/login", new { correo = "admin@test.com", password = "Abc12345" });
        var body = await login.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(body);
        var token = doc.RootElement.GetProperty("token").GetString()!;
        _client.ConToken(token);

        var response = await _client.GetAsync("/api/admin/backups");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PendientesValidacion_Autenticado_ReturnsOk()
    {
        var login = await _client.PostAsJsonAsync("/api/account/login", new { correo = "admin@test.com", password = "Abc12345" });
        var body = await login.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(body);
        var token = doc.RootElement.GetProperty("token").GetString()!;
        _client.ConToken(token);

        var response = await _client.GetAsync("/api/admin/psicologos/pendientes");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TarjetaPendientes_Autenticado_ReturnsOk()
    {
        var login = await _client.PostAsJsonAsync("/api/account/login", new { correo = "admin@test.com", password = "Abc12345" });
        var body = await login.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(body);
        var token = doc.RootElement.GetProperty("token").GetString()!;
        _client.ConToken(token);

        var response = await _client.GetAsync("/api/admin/psicologos/tarjeta-pendientes");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
