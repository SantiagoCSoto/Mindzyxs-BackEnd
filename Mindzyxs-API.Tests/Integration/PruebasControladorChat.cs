using System.Net;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class PruebasControladorChat : IClassFixture<FabricaAplicacionWebApiConDatos>
{
    private readonly HttpClient _client;

    public PruebasControladorChat(FabricaAplicacionWebApiConDatos factory)
    {
        factory.SeedDatabase();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task MisConversaciones_SinToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/chat/conversaciones");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TokenFirebase_SinToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync("/api/chat/token", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
