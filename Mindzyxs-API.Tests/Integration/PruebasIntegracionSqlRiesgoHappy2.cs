using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class PruebasIntegracionSqlRiesgoHappy2 : IClassFixture<FabricaAplicacionWebApiSql>
{
    private readonly HttpClient _client;

    public PruebasIntegracionSqlRiesgoHappy2(FabricaAplicacionWebApiSql factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CalcularInvitado_ReturnsOk()
    {
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
        var response = await _client.PostAsJsonAsync("/api/riesgo/invitado/calcular", vm);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
