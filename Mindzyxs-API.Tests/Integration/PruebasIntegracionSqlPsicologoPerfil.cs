using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class PruebasIntegracionSqlPsicologoPerfil : IClassFixture<FabricaAplicacionWebApiSql>
{
    private readonly FabricaAplicacionWebApiSql _factory;
    private readonly HttpClient _client;

    public PruebasIntegracionSqlPsicologoPerfil(FabricaAplicacionWebApiSql factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Obtener_Autenticado_ReturnsOk()
    {
        var email = $"pp{Guid.NewGuid():N}@test.com";
        var token = await AuthHelperSql.RegistrarPsicologoYActivarAsync(_factory, _client, email);
        _client.ConToken(token);

        var putResp = await _client.PutAsJsonAsync("/api/psicologo/perfil-profesional", new { AniosExperiencia = 5, DescripcionProfesional = "Desc" });
        putResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await _client.GetAsync("/api/psicologo/perfil-profesional");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
