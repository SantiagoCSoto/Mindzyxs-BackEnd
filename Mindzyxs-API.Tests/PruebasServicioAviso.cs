using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Services;
using Mindzyxs_API.Utilities;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class PruebasServicioAviso
{
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();

    private AvisoService CreateSut() => new(_sesiones.Object, _authLogs.Object);

    [Fact]
    public async Task RegistrarVisualizacion_ConSesion_RegistraConActor()
    {
        var sesion = new SesionDto(1, 5, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        _authLogs.Setup(a => a.LogAsync(5, 5, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        await sut.RegistrarVisualizacionAsync("token", "enlace", "ok", "127.0.0.1", CancellationToken.None);

        _authLogs.Verify(a => a.LogAsync(5, 5, LogAcciones.AvisoAclaratorio, It.IsAny<string>(), "127.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarVisualizacion_SinSesion_RegistraComoNull()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));
        _authLogs.Setup(a => a.LogAsync(null, null, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        await sut.RegistrarVisualizacionAsync(null, "auto:cuestionario", "error", "127.0.0.1", CancellationToken.None);

        _authLogs.Verify(a => a.LogAsync(null, null, LogAcciones.AvisoAclaratorioError, It.IsAny<string>(), "127.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAviso_RetornaAvisoConTexto()
    {
        var sut = CreateSut();
        var aviso = await sut.GetAvisoAsync(CancellationToken.None);

        aviso.Should().NotBeNull();
        aviso.Titulo.Should().Be("Mensaje aclaratorio");
        aviso.Texto.Should().Contain("Mindzyx");
    }
}

