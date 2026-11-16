using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class PruebasControladorPsicologoPerfil : IClassFixture<FabricaAplicacionWebApiConDatos>
{
    private readonly HttpClient _client;

    public PruebasControladorPsicologoPerfil(FabricaAplicacionWebApiConDatos factory)
    {
        factory.SeedDatabase();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Obtener_SinToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/psicologo/perfil-profesional");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Actualizar_SinToken_ReturnsUnauthorized()
    {
        var response = await _client.PutAsJsonAsync("/api/psicologo/perfil-profesional", new { });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
