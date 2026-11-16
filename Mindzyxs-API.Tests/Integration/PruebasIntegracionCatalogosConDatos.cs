using System.Net;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class PruebasIntegracionCatalogosConDatos : IClassFixture<FabricaAplicacionWebApiConDatos>
{
    private readonly HttpClient _client;
    private readonly FabricaAplicacionWebApiConDatos _factory;

    public PruebasIntegracionCatalogosConDatos(FabricaAplicacionWebApiConDatos factory)
    {
        _factory = factory;
        _factory.SeedDatabase();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Catalogos_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/account/catalogos");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}


