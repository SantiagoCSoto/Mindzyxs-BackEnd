using System.Net;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class AvisoEndpointTests : IClassFixture<FabricaAplicacionWebApi>
{
    private readonly HttpClient _client;

    public AvisoEndpointTests(FabricaAplicacionWebApi factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Aviso_Get_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/aviso");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // Aviso visualizacion usa sp_auth_registrar (stored proc) que no funciona con InMemory DB.
    // Se prueba en unit tests del servicio.
}




