using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class PruebasServicioProgreso
{
    private readonly Mock<IHistorialRepository> _historial = new();
    private readonly Mock<IUserRepository> _usuarios = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();

    private ProgresoService CreateSut() => new(
        _historial.Object, _usuarios.Object, _sesiones.Object, _authLogs.Object);

    [Fact]
    public async Task ObtenerAsync_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var act = () => sut.ObtenerAsync(null, 1, null, null, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task ObtenerAsync_PacienteNoAutorizado_LanzaInvalidOperation()
    {
        var sesion = new SesionDto(1, 5, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var user = new UsuarioDto { IdUsuario = 5, IdRol = 2, IdEstadoCuenta = 1 };
        _usuarios.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _usuarios.Setup(u => u.GetRolNombreAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync("Psicologo");
        _usuarios.Setup(u => u.GetEstadoCuentaNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>())).ReturnsAsync(8);
        _historial.Setup(h => h.EstaAutorizadoAsync(1, 5, 8, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var sut = CreateSut();
        var act = () => sut.ObtenerAsync("token", 1, null, null, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*autorización*");
    }

    [Fact]
    public async Task ObtenerAsync_SinEventos_RetornaTotalCero()
    {
        var sesion = new SesionDto(1, 5, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var user = new UsuarioDto { IdUsuario = 5, IdRol = 2, IdEstadoCuenta = 1 };
        _usuarios.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _usuarios.Setup(u => u.GetRolNombreAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync("Psicologo");
        _usuarios.Setup(u => u.GetEstadoCuentaNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>())).ReturnsAsync(8);
        _historial.Setup(h => h.EstaAutorizadoAsync(1, 5, 8, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _historial.Setup(h => h.GetEventosAsync(1, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(new List<EventoRow>());
        _historial.Setup(h => h.GetNivelesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<int, string>());
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.ObtenerAsync("token", 1, null, null, "127.0.0.1", CancellationToken.None);

        result.TotalRegistros.Should().Be(0);
        result.Serie.Should().BeEmpty();
    }

    [Fact]
    public async Task ObtenerAsync_ConEventos_CalculaPromedioYTendencia()
    {
        var sesion = new SesionDto(1, 5, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var user = new UsuarioDto { IdUsuario = 5, IdRol = 2, IdEstadoCuenta = 1 };
        _usuarios.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _usuarios.Setup(u => u.GetRolNombreAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync("Psicologo");
        _usuarios.Setup(u => u.GetEstadoCuentaNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>())).ReturnsAsync(8);
        _historial.Setup(h => h.EstaAutorizadoAsync(1, 5, 8, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var eventos = new List<EventoRow>
        {
            new() { IdEvento = 1, IdPaciente = 1, IdTipoEvento = 1, PuntajeTotal = 2, FechaEvento = DateTime.UtcNow.AddDays(-3) },
            new() { IdEvento = 2, IdPaciente = 1, IdTipoEvento = 1, PuntajeTotal = 3, FechaEvento = DateTime.UtcNow.AddDays(-2) },
            new() { IdEvento = 3, IdPaciente = 1, IdTipoEvento = 1, PuntajeTotal = 4, FechaEvento = DateTime.UtcNow.AddDays(-1) },
            new() { IdEvento = 4, IdPaciente = 1, IdTipoEvento = 1, PuntajeTotal = 5, FechaEvento = DateTime.UtcNow },
        };
        _historial.Setup(h => h.GetEventosAsync(1, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(eventos);
        _historial.Setup(h => h.GetNivelesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<int, string>());
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.ObtenerAsync("token", 1, null, null, "127.0.0.1", CancellationToken.None);

        result.TotalRegistros.Should().Be(4);
        result.Promedio.Should().Be(3.5);
        result.Serie.Should().HaveCount(4);
        result.Suficientes.Should().BeTrue();
    }
}

