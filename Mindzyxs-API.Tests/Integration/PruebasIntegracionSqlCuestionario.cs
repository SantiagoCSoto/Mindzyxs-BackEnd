using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class PruebasIntegracionSqlCuestionario : IClassFixture<FabricaAplicacionWebApiSql>
{
    private readonly HttpClient _client;

    public PruebasIntegracionSqlCuestionario(FabricaAplicacionWebApiSql factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PendienteInvitado_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/cuestionario/invitado");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
