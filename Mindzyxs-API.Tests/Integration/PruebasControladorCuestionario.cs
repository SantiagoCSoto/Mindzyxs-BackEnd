using System.Net;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class PruebasControladorCuestionario : IClassFixture<FabricaAplicacionWebApiConDatos>
{
    private readonly HttpClient _client;

    public PruebasControladorCuestionario(FabricaAplicacionWebApiConDatos factory)
    {
        factory.SeedDatabase();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PendienteInvitado_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/cuestionario/invitado");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PendientePaciente_SinToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/cuestionario/paciente/pendiente");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
