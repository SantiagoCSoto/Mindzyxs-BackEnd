using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Mindzyxs_API.Utilities;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class AvisoServiceTests
{
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();

    private AvisoService CreateSut() => new(_sesiones.Object, _authLogs.Object);

    [Fact]
    public async Task GetAviso_ReturnsAvisoWithTexto()
    {
        var sut = CreateSut();
        var aviso = await sut.GetAvisoAsync(CancellationToken.None);

        aviso.Should().NotBeNull();
        aviso.Titulo.Should().Be("Mensaje aclaratorio");
        aviso.Texto.Should().Contain("Mindzyx");
        aviso.Texto.Should().Contain("diagnósticos clínicos");
    }

    [Fact]
    public async Task RegistrarVisualizacion_SinToken_RegistraComoAnonimo()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));
        _authLogs.Setup(a => a.LogAsync(null, null, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        await sut.RegistrarVisualizacionAsync(null, "enlace", "ok", "127.0.0.1", CancellationToken.None);

        _authLogs.Verify(a => a.LogAsync(null, null, LogAcciones.AvisoAclaratorio, It.IsAny<string>(), "127.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarVisualizacion_ResultadoError_RegistraError()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));
        _authLogs.Setup(a => a.LogAsync(null, null, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        await sut.RegistrarVisualizacionAsync(null, "enlace", "error", "127.0.0.1", CancellationToken.None);

        _authLogs.Verify(a => a.LogAsync(null, null, LogAcciones.AvisoAclaratorioError, It.IsAny<string>(), "127.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class CatalogServiceTests
{
    private readonly Mock<ICatalogRepository> _catalogs = new();

    private CatalogService CreateSut() => new(_catalogs.Object);

    [Fact]
    public async Task GetRegistro_RetornaCatalogos()
    {
        _catalogs.Setup(c => c.GetTiposCuentaAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<CatalogItemDto> { new() { Id = 1, Nombre = "Paciente" }, new() { Id = 2, Nombre = "Psicologo" } });
        _catalogs.Setup(c => c.GetGenerosAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<CatalogItemDto> { new() { Id = 1, Nombre = "Masculino" } });
        _catalogs.Setup(c => c.GetEspecialidadesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<CatalogItemDto>());

        var sut = CreateSut();
        var result = await sut.GetRegistroAsync(CancellationToken.None);

        result.TiposCuenta.Should().HaveCount(2);
        result.Generos.Should().HaveCount(1);
    }
}

