using System.Net;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class PruebasIntegracionCatalogos : IClassFixture<FabricaAplicacionWebApi>
{
    private readonly HttpClient _client;

    public PruebasIntegracionCatalogos(FabricaAplicacionWebApi factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Catalogos_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/account/catalogos");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Catalogos_ReturnsJson()
    {
        var response = await _client.GetAsync("/api/account/catalogos");
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }
}


