using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class PruebasControladorAccesos : IClassFixture<FabricaAplicacionWebApiConDatos>
{
    private readonly HttpClient _client;

    public PruebasControladorAccesos(FabricaAplicacionWebApiConDatos factory)
    {
        factory.SeedDatabase();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task MisAccesos_SinToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/accesos");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Autorizar_SinToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync("/api/accesos/1/autorizar", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Revocar_SinToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync("/api/accesos/1/revocar", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
