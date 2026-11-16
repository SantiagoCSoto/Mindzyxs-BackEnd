using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class HistorialServiceMoreTests
{
    private readonly Mock<IHistorialRepository> _historial = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();

    private HistorialService CreateSut() => new(
        _historial.Object, _users.Object, _sesiones.Object, _authLogs.Object);

    private void SetupSesion(int userId = 1)
    {
        var sesion = new SesionDto(1, userId, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var user = new UsuarioDto { IdUsuario = userId, IdRol = 1, IdEstadoCuenta = 1 };
        _users.Setup(u => u.FindByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");
        _users.Setup(u => u.GetEstadoCuentaNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
    }

    [Fact]
    public async Task Obtener_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var act = () => sut.ObtenerAsync(null, null, null, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Obtener_FechaDesdePosteriorHasta_LanzaInvalidOperation()
    {
        SetupSesion();
        var sut = CreateSut();
        var act = () => sut.ObtenerAsync("token", DateTime.UtcNow, DateTime.UtcNow.AddDays(-1), "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Desde* no puede ser posterior*");
    }

    [Fact]
    public async Task Obtener_SinEventos_RetornaListaVacia()
    {
        SetupSesion();
        _historial.Setup(h => h.GetEventosAsync(1, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(new List<EventoRow>());
        _historial.Setup(h => h.GetNivelesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<int, string>());
        _historial.Setup(h => h.GetCuestionariosTituloAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<int, string>());
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.ObtenerAsync("token", null, null, "127.0.0.1", CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Obtener_ConEventos_RetornaListaOrdenada()
    {
        SetupSesion();
        var eventos = new List<EventoRow>
        {
            new() { IdEvento = 1, IdPaciente = 1, IdTipoEvento = 1, PuntajeTotal = 4, FechaEvento = DateTime.UtcNow.AddDays(-1), Nota = "Nota 1" },
            new() { IdEvento = 2, IdPaciente = 1, IdTipoEvento = 1, PuntajeTotal = 3, FechaEvento = DateTime.UtcNow, Nota = null },
        };
        _historial.Setup(h => h.GetEventosAsync(1, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(eventos);
        _historial.Setup(h => h.GetNivelesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<int, string> { [1] = "Bajo" });
        _historial.Setup(h => h.GetCuestionariosTituloAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<int, string>());
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.ObtenerAsync("token", null, null, "127.0.0.1", CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task ObtenerDetalle_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var act = () => sut.ObtenerDetalleAsync(null, 1, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task ObtenerDetalle_EventoNoEncontrado_LanzaInvalidOperation()
    {
        SetupSesion();
        _historial.Setup(h => h.FindEventoAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((EventoRow?)null);

        var sut = CreateSut();
        var act = () => sut.ObtenerDetalleAsync("token", 99, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no encontrado*");
    }

    [Fact]
    public async Task ObtenerDetalle_EventoDeOtroPaciente_LanzaInvalidOperation()
    {
        SetupSesion();
        _historial.Setup(h => h.FindEventoAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync(
            new EventoRow { IdEvento = 99, IdPaciente = 999, IdTipoEvento = 1, FechaEvento = DateTime.UtcNow });

        var sut = CreateSut();
        var act = () => sut.ObtenerDetalleAsync("token", 99, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no encontrado*");
    }

    [Fact]
    public async Task ObtenerDetalle_Valido_RetornaDetalle()
    {
        SetupSesion();
        var evento = new EventoRow { IdEvento = 1, IdPaciente = 1, IdTipoEvento = 1, PuntajeTotal = 4, FechaEvento = DateTime.UtcNow, Nota = "Me sentí bien" };
        _historial.Setup(h => h.FindEventoAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(evento);
        _historial.Setup(h => h.GetNivelesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<int, string>());
        _historial.Setup(h => h.GetCuestionariosTituloAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<int, string>());
        _historial.Setup(h => h.GetRespuestasAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<RespuestaRow>());
        _historial.Setup(h => h.GetObservacionesAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<ObservacionRow>());
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.ObtenerDetalleAsync("token", 1, "127.0.0.1", CancellationToken.None);

        result.Should().NotBeNull();
        result.Nota.Should().Be("Me sentí bien");
    }
}

