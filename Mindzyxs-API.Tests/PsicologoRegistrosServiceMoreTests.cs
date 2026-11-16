using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class PsicologoRegistrosServiceMoreTests
{
    private readonly Mock<IHistorialRepository> _historial = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICatalogRepository> _catalogs = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();

    private PsicologoRegistrosService CreateSut() => new(
        _historial.Object, _users.Object, _catalogs.Object, _sesiones.Object, _authLogs.Object);

    private void SetupSesion(int userId = 5)
    {
        var sesion = new SesionDto(1, userId, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var user = new UsuarioDto { IdUsuario = userId, IdRol = 2, IdEstadoCuenta = 1, IdEstadoAprobacion = 1 };
        _users.Setup(u => u.FindByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Psicologo");
        _users.Setup(u => u.GetEstadoCuentaNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
        _catalogs.Setup(c => c.GetEstadoNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Aprobado");
    }

    [Fact]
    public async Task Pacientes_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var act = () => sut.PacientesAsync(null, null, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Pacientes_ConBusqueda_FiltraPorNombre()
    {
        SetupSesion();
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>())).ReturnsAsync(8);
        _historial.Setup(h => h.GetPacientesAutorizadosAsync(5, 8, It.IsAny<CancellationToken>())).ReturnsAsync(
            new List<UsuarioDto>
            {
                new() { IdUsuario = 1, Nombre = "Camila", Apellido = "Rojas", Correo = "camila@test.com" },
                new() { IdUsuario = 2, Nombre = "Juan", Apellido = "Perez", Correo = "juan@test.com" }
            });
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.PacientesAsync("token", "Camila", "127.0.0.1", CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Nombre.Should().Be("Camila Rojas");
    }

    [Fact]
    public async Task Registros_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var act = () => sut.RegistrosAsync(null, 1, null, null, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Registros_FechaDesdePosteriorHasta_LanzaInvalidOperation()
    {
        SetupSesion();
        var sut = CreateSut();
        var act = () => sut.RegistrosAsync("token", 1, DateTime.UtcNow, DateTime.UtcNow.AddDays(-1), "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Desde* no puede ser posterior*");
    }

    [Fact]
    public async Task Registros_PacienteNoAutorizado_LanzaInvalidOperation()
    {
        SetupSesion();
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>())).ReturnsAsync(8);
        _historial.Setup(h => h.EstaAutorizadoAsync(1, 5, 8, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var sut = CreateSut();
        var act = () => sut.RegistrosAsync("token", 1, null, null, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*autorización*");
    }

    [Fact]
    public async Task Registros_SinEventos_RetornaListaVacia()
    {
        SetupSesion();
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>())).ReturnsAsync(8);
        _historial.Setup(h => h.EstaAutorizadoAsync(1, 5, 8, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _historial.Setup(h => h.GetEventosAsync(1, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(new List<EventoRow>());
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.RegistrosAsync("token", 1, null, null, "127.0.0.1", CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Detalle_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var act = () => sut.DetalleAsync(null, 1, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Detalle_EventoNoEncontrado_LanzaInvalidOperation()
    {
        SetupSesion();
        _historial.Setup(h => h.FindEventoAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((EventoRow?)null);

        var sut = CreateSut();
        var act = () => sut.DetalleAsync("token", 99, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Registro no encontrado*");
    }

    [Fact]
    public async Task Detalle_EventoDeOtroPaciente_LanzaInvalidOperation()
    {
        SetupSesion();
        _historial.Setup(h => h.FindEventoAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync(
            new EventoRow { IdEvento = 99, IdPaciente = 999, IdTipoEvento = 1, FechaEvento = DateTime.UtcNow });

        var sut = CreateSut();
        var act = () => sut.DetalleAsync("token", 99, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*autorización*");
    }

    [Fact]
    public async Task Pacientes_HappyPath_RetornaLista()
    {
        SetupSesion();
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>())).ReturnsAsync(8);
        _historial.Setup(h => h.GetPacientesAutorizadosAsync(5, 8, It.IsAny<CancellationToken>())).ReturnsAsync(
            new List<UsuarioDto>
            {
                new() { IdUsuario = 1, Nombre = "Camila", Apellido = "Rojas", Correo = "camila@test.com" }
            });
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.PacientesAsync("token", null, "127.0.0.1", CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(1);
    }

    [Fact]
    public async Task Registros_HappyPath_RetornaLista()
    {
        SetupSesion();
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>())).ReturnsAsync(8);
        _historial.Setup(h => h.EstaAutorizadoAsync(1, 5, 8, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _historial.Setup(h => h.GetEventosAsync(1, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(
            new List<EventoRow> { new() { IdEvento = 10, IdPaciente = 1, IdTipoEvento = 1, FechaEvento = DateTime.UtcNow, PuntajeTotal = 4, IdNivelRiesgo = 2 } });
        _historial.Setup(h => h.GetNivelesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<int, string> { [2] = "Medio" });
        _historial.Setup(h => h.GetCuestionariosTituloAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<int, string>());
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.RegistrosAsync("token", 1, null, null, "127.0.0.1", CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Puntaje.Should().Be(4);
    }

    [Fact]
    public async Task Detalle_HappyPath_RetornaDetalle()
    {
        SetupSesion();
        _historial.Setup(h => h.FindEventoAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(
            new EventoRow { IdEvento = 10, IdPaciente = 1, IdTipoEvento = 1, FechaEvento = DateTime.UtcNow, PuntajeTotal = 4, IdNivelRiesgo = 2, Nota = "nota" });
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>())).ReturnsAsync(8);
        _historial.Setup(h => h.EstaAutorizadoAsync(1, 5, 8, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _historial.Setup(h => h.GetNivelesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<int, string> { [2] = "Medio" });
        _historial.Setup(h => h.GetCuestionariosTituloAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<int, string>());
        _historial.Setup(h => h.GetRespuestasAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new List<RespuestaRow>());
        _historial.Setup(h => h.GetObservacionesAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new List<ObservacionRow>());
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.DetalleAsync("token", 10, "127.0.0.1", CancellationToken.None);

        result.Id.Should().Be(10);
        result.Nota.Should().Be("nota");
        result.Observaciones.Should().BeEmpty();
    }
}
