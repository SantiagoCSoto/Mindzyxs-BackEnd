using System.Net;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class PruebasControladorPsicologos : IClassFixture<FabricaAplicacionWebApiConDatos>
{
    private readonly HttpClient _client;

    public PruebasControladorPsicologos(FabricaAplicacionWebApiConDatos factory)
    {
        factory.SeedDatabase();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Directorio_SinToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/psicologos");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
