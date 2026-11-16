using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class PruebasIntegracionSqlRiesgoHappy : IClassFixture<FabricaAplicacionWebApiSql>
{
    private readonly FabricaAplicacionWebApiSql _factory;
    private readonly HttpClient _client;

    public PruebasIntegracionSqlRiesgoHappy(FabricaAplicacionWebApiSql factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AlertaRiesgo_Autenticado_ReturnsOk()
    {
        var email = $"rs{Guid.NewGuid():N}@test.com";
        var token = await AuthHelperSql.RegistrarYVerificarYLoginAsync(_factory, _client, email);
        _client.ConToken(token);

        var response = await _client.PostAsJsonAsync("/api/riesgo/paciente/alerta", new { accion = "vista", origen = "cuestionario" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
