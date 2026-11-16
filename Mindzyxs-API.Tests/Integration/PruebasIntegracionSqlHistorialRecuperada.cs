using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class PruebasIntegracionSqlHistorialRecuperada : IClassFixture<FabricaAplicacionWebApiSql>
{
    private readonly FabricaAplicacionWebApiSql _factory;
    private readonly HttpClient _client;

    public PruebasIntegracionSqlHistorialRecuperada(FabricaAplicacionWebApiSql factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Registrar_Autenticado_ReturnsOkYPersiste()
    {
        var email = $"hr{Guid.NewGuid():N}@test.com";
        var token = await AuthHelperSql.RegistrarYVerificarYLoginAsync(_factory, _client, email);
        _client.ConToken(token);

        var response = await _client.PostAsJsonAsync("/api/historial", new { puntaje = 4, nota = "nota de prueba" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify persisted
        var listResp = await _client.GetAsync("/api/historial");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await listResp.Content.ReadAsStringAsync();
        body.Should().Contain("Registro emocional");
    }

    [Fact]
    public async Task Mio_ConRegistros_ReturnsOk()
    {
        var email = $"hm{Guid.NewGuid():N}@test.com";
        var token = await AuthHelperSql.RegistrarYVerificarYLoginAsync(_factory, _client, email);
        _client.ConToken(token);

        await _client.PostAsJsonAsync("/api/historial", new { puntaje = 3, nota = "registro 1" });

        var response = await _client.GetAsync("/api/historial");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBe("[]");
    }
}
