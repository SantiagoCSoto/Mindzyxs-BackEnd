using System.Net;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class PruebasControladorAdmin : IClassFixture<FabricaAplicacionWebApiConDatos>
{
    private readonly HttpClient _client;

    public PruebasControladorAdmin(FabricaAplicacionWebApiConDatos factory)
    {
        factory.SeedDatabase();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Backup_SinToken_ReturnsForbidden()
    {
        var response = await _client.PostAsync("/api/admin/backup", null);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListBackups_SinToken_ReturnsForbidden()
    {
        var response = await _client.GetAsync("/api/admin/backups");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
