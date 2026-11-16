using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Services;
using Mindzyxs_API.Utilities;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class PruebasServicioInvitado
{
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();

    private InvitadoService CreateSut() => new(_sesiones.Object, _authLogs.Object);

    [Fact]
    public async Task ObtenerPanel_ReturnsPanelConOpciones()
    {
        var sut = CreateSut();
        var panel = await sut.ObtenerPanelAsync(CancellationToken.None);

        panel.Should().NotBeNull();
        panel.AvisoObligatorio.Should().BeTrue();
        panel.Disponibles.Should().HaveCount(3);
        panel.Exclusivas.Should().HaveCount(4);
    }

    [Fact]
    public async Task RegistrarAcceso_SinToken_RegistraComoAnonimo()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));
        _authLogs.Setup(a => a.LogAsync(null, null, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        await sut.RegistrarAccesoAsync(null, "landing", "ok", "127.0.0.1", CancellationToken.None);

        _authLogs.Verify(a => a.LogAsync(null, null, LogAcciones.InvitadoAcceso, It.IsAny<string>(), "127.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarAcceso_ResultadoError_RegistraError()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));
        _authLogs.Setup(a => a.LogAsync(null, null, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        await sut.RegistrarAccesoAsync(null, "landing", "error", "127.0.0.1", CancellationToken.None);

        _authLogs.Verify(a => a.LogAsync(null, null, LogAcciones.InvitadoAccesoError, It.IsAny<string>(), "127.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarBloqueo_RegistraBloqueo()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));
        _authLogs.Setup(a => a.LogAsync(null, null, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        await sut.RegistrarBloqueoAsync(null, "Historial emocional", "127.0.0.1", CancellationToken.None);

        _authLogs.Verify(a => a.LogAsync(null, null, LogAcciones.InvitadoBloqueo, It.IsAny<string>(), "127.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarSalida_RegistraSalida()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));
        _authLogs.Setup(a => a.LogAsync(null, null, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        await sut.RegistrarSalidaAsync(null, "panel", "127.0.0.1", CancellationToken.None);

        _authLogs.Verify(a => a.LogAsync(null, null, LogAcciones.InvitadoSalida, It.IsAny<string>(), "127.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarAcceso_ConTokenValido_AsociaUsuario()
    {
        var sesion = new SesionDto(1, 7, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        _authLogs.Setup(a => a.LogAsync(7, 7, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        await sut.RegistrarAccesoAsync("token", "landing", "ok", "127.0.0.1", CancellationToken.None);

        _authLogs.Verify(a => a.LogAsync(7, 7, LogAcciones.InvitadoAcceso, It.IsAny<string>(), "127.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarBloqueo_ConTokenValido_AsociaUsuario()
    {
        var sesion = new SesionDto(1, 7, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        _authLogs.Setup(a => a.LogAsync(7, 7, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        await sut.RegistrarBloqueoAsync("token", "Historial emocional", "127.0.0.1", CancellationToken.None);

        _authLogs.Verify(a => a.LogAsync(7, 7, LogAcciones.InvitadoBloqueo, It.IsAny<string>(), "127.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarSalida_ConTokenValido_AsociaUsuario()
    {
        var sesion = new SesionDto(1, 7, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        _authLogs.Setup(a => a.LogAsync(7, 7, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        await sut.RegistrarSalidaAsync("token", "panel", "127.0.0.1", CancellationToken.None);

        _authLogs.Verify(a => a.LogAsync(7, 7, LogAcciones.InvitadoSalida, It.IsAny<string>(), "127.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarAcceso_OrigenVacio_UsaNoEspecificado()
    {
        _authLogs.Setup(a => a.LogAsync(null, null, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        await sut.RegistrarAccesoAsync(null, null, "ok", "127.0.0.1", CancellationToken.None);

        _authLogs.Verify(a => a.LogAsync(null, null, LogAcciones.InvitadoAcceso, It.Is<string>(d => d.Contains("no especificado")), "127.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
    }
}

