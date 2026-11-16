using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class PruebasIntegracionSqlCuestionarioHappy2 : IClassFixture<FabricaAplicacionWebApiSql>
{
    private readonly FabricaAplicacionWebApiSql _factory;
    private readonly HttpClient _client;

    public PruebasIntegracionSqlCuestionarioHappy2(FabricaAplicacionWebApiSql factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task FinalizarPaciente_Autenticado_ReturnsOk()
    {
        var email = $"cq{Guid.NewGuid():N}@test.com";
        var token = await AuthHelperSql.RegistrarYVerificarYLoginAsync(_factory, _client, email);
        _client.ConToken(token);

        var vm = new
        {
            cuestionarioId = 3,
            respuestas = new[]
            {
                new { preguntaId = 3, opcionId = 1 },
                new { preguntaId = 4, opcionId = 2 },
                new { preguntaId = 5, opcionId = 3 }
            }
        };
        var response = await _client.PostAsJsonAsync("/api/cuestionario/paciente/finalizar", vm);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
