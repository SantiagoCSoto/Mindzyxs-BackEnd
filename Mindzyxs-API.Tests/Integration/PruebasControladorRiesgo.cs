using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class PruebasControladorRiesgo : IClassFixture<FabricaAplicacionWebApiConDatos>
{
    private readonly HttpClient _client;

    public PruebasControladorRiesgo(FabricaAplicacionWebApiConDatos factory)
    {
        factory.SeedDatabase();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AlertaRiesgo_SinToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/riesgo/paciente/alerta", new { accion = "vista" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
