using System.Net;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class PruebasControladorPsicologoRegistros : IClassFixture<FabricaAplicacionWebApiConDatos>
{
    private readonly HttpClient _client;

    public PruebasControladorPsicologoRegistros(FabricaAplicacionWebApiConDatos factory)
    {
        factory.SeedDatabase();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Pacientes_SinToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/psicologo/pacientes");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
