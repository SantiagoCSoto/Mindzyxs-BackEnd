using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class PruebasIntegracionSqlHistorial : IClassFixture<FabricaAplicacionWebApiSql>
{
    private readonly FabricaAplicacionWebApiSql _factory;
    private readonly HttpClient _client;

    public PruebasIntegracionSqlHistorial(FabricaAplicacionWebApiSql factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Historial_Mio_Autenticado_ReturnsOk()
    {
        var email = $"hist{Guid.NewGuid():N}@test.com";
        var token = await AuthHelperSql.RegistrarYVerificarYLoginAsync(_factory, _client, email);
        _client.ConToken(token);

        var response = await _client.GetAsync("/api/historial");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Historial_Registrar_Autenticado_ReturnsOk()
    {
        var email = $"hist2{Guid.NewGuid():N}@test.com";
        var token = await AuthHelperSql.RegistrarYVerificarYLoginAsync(_factory, _client, email);
        _client.ConToken(token);

        var response = await _client.PostAsJsonAsync("/api/historial", new { puntaje = 4, nota = "nota" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Historial_Registrar_SinPuntaje_ReturnsBadRequest()
    {
        var email = $"hist3{Guid.NewGuid():N}@test.com";
        var token = await AuthHelperSql.RegistrarYVerificarYLoginAsync(_factory, _client, email);
        _client.ConToken(token);

        var response = await _client.PostAsJsonAsync("/api/historial", new { nota = "nota" });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Historial_Detalle_Inexistente_ReturnsNotFound()
    {
        var email = $"hist4{Guid.NewGuid():N}@test.com";
        var token = await AuthHelperSql.RegistrarYVerificarYLoginAsync(_factory, _client, email);
        _client.ConToken(token);

        var response = await _client.GetAsync("/api/historial/999999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
