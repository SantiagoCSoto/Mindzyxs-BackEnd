using System.Net;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

[Collection("SqlIntegration")]
public class PruebasIntegracionSqlAccesos : IClassFixture<FabricaAplicacionWebApiSql>
{
    private readonly FabricaAplicacionWebApiSql _factory;
    private readonly HttpClient _client;

    public PruebasIntegracionSqlAccesos(FabricaAplicacionWebApiSql factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task MisAccesos_Autenticado_ReturnsOk()
    {
        var email = $"acc{Guid.NewGuid():N}@test.com";
        var token = await AuthHelperSql.RegistrarYVerificarYLoginAsync(_factory, _client, email);
        _client.ConToken(token);

        var response = await _client.GetAsync("/api/accesos");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Autorizar_PsicologoInexistente_ReturnsNotFound()
    {
        var email = $"acc2{Guid.NewGuid():N}@test.com";
        var token = await AuthHelperSql.RegistrarYVerificarYLoginAsync(_factory, _client, email);
        _client.ConToken(token);

        var response = await _client.PostAsync("/api/accesos/999999/autorizar", null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}



