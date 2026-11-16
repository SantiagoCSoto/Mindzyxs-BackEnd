using System.Net;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

[Collection("SqlIntegration")]
public class PruebasIntegracionSqlPsicologos : IClassFixture<FabricaAplicacionWebApiSql>
{
    private readonly FabricaAplicacionWebApiSql _factory;
    private readonly HttpClient _client;

    public PruebasIntegracionSqlPsicologos(FabricaAplicacionWebApiSql factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Directorio_Autenticado_ReturnsOk()
    {
        var email = $"psi{Guid.NewGuid():N}@test.com";
        var token = await AuthHelperSql.RegistrarYVerificarYLoginAsync(_factory, _client, email);
        _client.ConToken(token);

        var response = await _client.GetAsync("/api/psicologos");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}



