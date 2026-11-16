using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class PruebasServicioObservaciones
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICatalogRepository> _catalogs = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IHistorialRepository> _historial = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();

    private ObservacionesService CreateSut() => new(
        _users.Object, _catalogs.Object, _sesiones.Object, _historial.Object, _authLogs.Object);

    private void SetupSesion(int userId = 5)
    {
        var sesion = new SesionDto(1, userId, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var user = new UsuarioDto { IdUsuario = userId, IdRol = 2, IdEstadoCuenta = 1, IdEstadoAprobacion = 1, Nombre = "Laura", Apellido = "Gomez" };
        _users.Setup(u => u.FindByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Psicologo");
        _users.Setup(u => u.GetEstadoCuentaNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
        _catalogs.Setup(c => c.GetEstadoNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Aprobado");
    }

    [Fact]
    public async Task Crear_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var dto = new ObservacionCrearDto { IdPaciente = 1, Contenido = "Test" };
        var act = () => sut.CrearAsync(null, dto, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Crear_ContenidoVacio_LanzaInvalidOperation()
    {
        SetupSesion();
        var sut = CreateSut();
        var dto = new ObservacionCrearDto { IdPaciente = 1, Contenido = "" };
        var act = () => sut.CrearAsync("token", dto, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*obligatoria*");
    }

    [Fact]
    public async Task Crear_PacienteNoEncontrado_LanzaKeyNotFound()
    {
        SetupSesion();
        _users.Setup(u => u.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var dto = new ObservacionCrearDto { IdPaciente = 1, Contenido = "Test" };
        var act = () => sut.CrearAsync("token", dto, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*Paciente no encontrado*");
    }

    [Fact]
    public async Task Crear_SinAutorizacion_LanzaInvalidOperation()
    {
        SetupSesion();
        var paciente = new UsuarioDto { IdUsuario = 1, Nombre = "Camila", Apellido = "Rojas" };
        _users.Setup(u => u.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(paciente);
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>())).ReturnsAsync(8);
        _historial.Setup(h => h.EstaAutorizadoAsync(1, 5, 8, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var sut = CreateSut();
        var dto = new ObservacionCrearDto { IdPaciente = 1, Contenido = "Test" };
        var act = () => sut.CrearAsync("token", dto, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*autorización*");
    }

    [Fact]
    public async Task Crear_EventoNoEncontrado_LanzaKeyNotFound()
    {
        SetupSesion();
        var paciente = new UsuarioDto { IdUsuario = 1, Nombre = "Camila", Apellido = "Rojas" };
        _users.Setup(u => u.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(paciente);
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>())).ReturnsAsync(8);
        _historial.Setup(h => h.EstaAutorizadoAsync(1, 5, 8, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _historial.Setup(h => h.FindEventoAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((EventoRow?)null);

        var sut = CreateSut();
        var dto = new ObservacionCrearDto { IdPaciente = 1, IdEvento = 99, Contenido = "Test" };
        var act = () => sut.CrearAsync("token", dto, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*Registro no encontrado*");
    }

    [Fact]
    public async Task Crear_EventoDeOtroPaciente_LanzaKeyNotFound()
    {
        SetupSesion();
        var paciente = new UsuarioDto { IdUsuario = 1, Nombre = "Camila", Apellido = "Rojas" };
        _users.Setup(u => u.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(paciente);
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>())).ReturnsAsync(8);
        _historial.Setup(h => h.EstaAutorizadoAsync(1, 5, 8, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _historial.Setup(h => h.FindEventoAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync(
            new EventoRow { IdEvento = 99, IdPaciente = 2, IdTipoEvento = 1, FechaEvento = DateTime.UtcNow });

        var sut = CreateSut();
        var dto = new ObservacionCrearDto { IdPaciente = 1, IdEvento = 99, Contenido = "Test" };
        var act = () => sut.CrearAsync("token", dto, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*Registro no encontrado*");
    }

    [Fact]
    public async Task Crear_Valido_GuardaYRetornaMensaje()
    {
        SetupSesion();
        var paciente = new UsuarioDto { IdUsuario = 1, Nombre = "Camila", Apellido = "Rojas" };
        _users.Setup(u => u.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(paciente);
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>())).ReturnsAsync(8);
        _historial.Setup(h => h.EstaAutorizadoAsync(1, 5, 8, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _historial.Setup(h => h.GuardarObservacionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var dto = new ObservacionCrearDto { IdPaciente = 1, Contenido = "El paciente muestra mejoría" };
        var result = await sut.CrearAsync("token", dto, "127.0.0.1", CancellationToken.None);

        result.Should().Be("Observación guardada correctamente");
    }
}

