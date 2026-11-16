using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class PruebasIntegracionSqlCuestionarioHappy : IClassFixture<FabricaAplicacionWebApiSql>
{
    private readonly FabricaAplicacionWebApiSql _factory;
    private readonly HttpClient _client;

    public PruebasIntegracionSqlCuestionarioHappy(FabricaAplicacionWebApiSql factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PendientePaciente_Autenticado_ReturnsOk()
    {
        var email = $"cq{Guid.NewGuid():N}@test.com";
        var token = await AuthHelperSql.RegistrarYVerificarYLoginAsync(_factory, _client, email);
        _client.ConToken(token);

        var response = await _client.GetAsync("/api/cuestionario/paciente/pendiente");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AsignadosPaciente_Autenticado_ReturnsOk()
    {
        var email = $"cq2{Guid.NewGuid():N}@test.com";
        var token = await AuthHelperSql.RegistrarYVerificarYLoginAsync(_factory, _client, email);
        _client.ConToken(token);

        var response = await _client.GetAsync("/api/cuestionario/paciente/asignados");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
