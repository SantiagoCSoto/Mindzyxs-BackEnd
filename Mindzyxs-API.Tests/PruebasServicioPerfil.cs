using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Mindzyxs_API.Utilities;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class PruebasServicioPerfil
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICatalogRepository> _catalogs = new();
    private readonly Mock<ICodigoRepository> _codes = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();
    private readonly Mock<INotificacionRepository> _notificacionesRepo = new();
    private readonly Mock<IEmailSender> _emailSender = new();

    private PerfilService CreateSut()
    {
        var notificaciones = new NotificationService(_notificacionesRepo.Object, _emailSender.Object, _authLogs.Object);
        var opts = Microsoft.Extensions.Options.Options.Create(new RegistroOptions { CodigoExpiraMinutos = 15 });
        return new PerfilService(_users.Object, _catalogs.Object, _codes.Object, _sesiones.Object, _authLogs.Object, notificaciones, opts);
    }

    private void SetupSesion(int userId = 1)
    {
        var sesion = new SesionDto(1, userId, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var user = new UsuarioDto { IdUsuario = userId, IdRol = 1, IdEstadoCuenta = 1, Correo = "paciente@test.com", Nombre = "Camila", IdGenero = 1 };
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
        var act = () => sut.ObtenerAsync(null, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Obtener_Valido_RetornaPerfil()
    {
        SetupSesion();
        _catalogs.Setup(c => c.GetGenerosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CatalogItemDto> { new() { Id = 1, Nombre = "Masculino" } });

        var sut = CreateSut();
        var result = await sut.ObtenerAsync("token", CancellationToken.None);

        result.Nombres.Should().Be("Camila");
        result.Correo.Should().Be("paciente@test.com");
        result.Genero.Should().Be("Masculino");
    }

    [Fact]
    public async Task Actualizar_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var vm = new ActualizarPerfilVm { Nombres = "Camila", Apellidos = "Rojas" };
        var act = () => sut.ActualizarAsync(null, vm, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Actualizar_NombresVacios_LanzaInvalidOperation()
    {
        SetupSesion();
        var sut = CreateSut();
        var vm = new ActualizarPerfilVm { Nombres = "", Apellidos = "Rojas" };
        var act = () => sut.ActualizarAsync("token", vm, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*obligatorio*");
    }

    [Fact]
    public async Task Actualizar_ApellidosVacios_LanzaInvalidOperation()
    {
        SetupSesion();
        var sut = CreateSut();
        var vm = new ActualizarPerfilVm { Nombres = "Camila", Apellidos = "" };
        var act = () => sut.ActualizarAsync("token", vm, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*obligatorio*");
    }

    [Fact]
    public async Task Actualizar_FechaFutura_LanzaInvalidOperation()
    {
        SetupSesion();
        var sut = CreateSut();
        var vm = new ActualizarPerfilVm { Nombres = "Camila", Apellidos = "Rojas", FechaNacimiento = DateTime.UtcNow.AddDays(1) };
        var act = () => sut.ActualizarAsync("token", vm, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*fecha v*");
    }

    [Fact]
    public async Task Actualizar_MenorDe14_LanzaInvalidOperation()
    {
        SetupSesion();
        var sut = CreateSut();
        var vm = new ActualizarPerfilVm { Nombres = "Camila", Apellidos = "Rojas", FechaNacimiento = DateTime.UtcNow.AddYears(-10) };
        var act = () => sut.ActualizarAsync("token", vm, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*edad v*");
    }

    [Fact]
    public async Task Actualizar_GeneroInvalido_LanzaInvalidOperation()
    {
        SetupSesion();
        _catalogs.Setup(c => c.ExisteGeneroAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var sut = CreateSut();
        var vm = new ActualizarPerfilVm { Nombres = "Camila", Apellidos = "Rojas", IdGenero = 99 };
        var act = () => sut.ActualizarAsync("token", vm, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*g*nero v*");
    }

    [Fact]
    public async Task Actualizar_Valido_RetornaMensajeExito()
    {
        SetupSesion();
        _users.Setup(u => u.ActualizarPerfilAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var vm = new ActualizarPerfilVm { Nombres = "Camila", Apellidos = "Rojas" };
        var result = await sut.ActualizarAsync("token", vm, "127.0.0.1", CancellationToken.None);

        result.Should().Be("Perfil actualizado correctamente");
    }

    [Fact]
    public async Task SolicitarCambio_CorreoInvalido_LanzaInvalidOperation()
    {
        SetupSesion();
        var sut = CreateSut();
        var act = () => sut.SolicitarCambioAsync("token", "invalid", "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*correo v*");
    }

    [Fact]
    public async Task SolicitarCambio_CorreoDuplicado_LanzaInvalidOperation()
    {
        SetupSesion();
        _users.Setup(u => u.ExistsByCorreoAsync("dup@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var sut = CreateSut();
        var act = () => sut.SolicitarCambioAsync("token", "dup@test.com", "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*ya se encuentra registrado*");
    }

    [Fact]
    public async Task ConfirmarCambio_CorreoInvalido_LanzaInvalidOperation()
    {
        SetupSesion();
        var sut = CreateSut();
        var act = () => sut.ConfirmarCambioAsync("token", "invalid", "123456", "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*correo v*");
    }

    [Fact]
    public async Task ConfirmarCambio_CorreoDuplicado_LanzaInvalidOperation()
    {
        SetupSesion();
        _users.Setup(u => u.ExistsByCorreoAsync("dup@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var sut = CreateSut();
        var act = () => sut.ConfirmarCambioAsync("token", "dup@test.com", "123456", "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*ya se encuentra registrado*");
    }

    [Fact]
    public async Task ConfirmarCambio_SinCodigoPrevio_LanzaInvalidOperation()
    {
        SetupSesion();
        _codes.Setup(c => c.UltimoAsync(1, "CambioCorreo", It.IsAny<CancellationToken>())).Returns(Task.FromResult<mindzyxsCode?>(null));

        var sut = CreateSut();
        var act = () => sut.ConfirmarCambioAsync("token", "nuevo@test.com", "123456", "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Código incorrecto*");
    }

    [Fact]
    public async Task ConfirmarCambio_CodigoExpirado_LanzaInvalidOperation()
    {
        SetupSesion();
        _codes.Setup(c => c.UltimoAsync(1, "CambioCorreo", It.IsAny<CancellationToken>())).Returns(
            Task.FromResult<mindzyxsCode?>(new mindzyxsCode(1, "123456", DateTime.UtcNow.AddMinutes(-5))));
        _users.Setup(u => u.ExistsByCorreoAsync("nuevo@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var sut = CreateSut();
        var act = () => sut.ConfirmarCambioAsync("token", "nuevo@test.com", "123456", "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*expiró*");
    }

    [Fact]
    public async Task ConfirmarCambio_CodigoIncorrecto_LanzaInvalidOperation()
    {
        SetupSesion();
        _codes.Setup(c => c.UltimoAsync(1, "CambioCorreo", It.IsAny<CancellationToken>())).Returns(
            Task.FromResult<mindzyxsCode?>(new mindzyxsCode(1, "654321", DateTime.UtcNow.AddMinutes(10))));
        _users.Setup(u => u.ExistsByCorreoAsync("nuevo@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var sut = CreateSut();
        var act = () => sut.ConfirmarCambioAsync("token", "nuevo@test.com", "123456", "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Código incorrecto*");
    }

    [Fact]
    public async Task ConfirmarCambio_Valido_ActualizaCorreo()
    {
        SetupSesion();
        _codes.Setup(c => c.UltimoAsync(1, "CambioCorreo", It.IsAny<CancellationToken>())).Returns(
            Task.FromResult<mindzyxsCode?>(new mindzyxsCode(1, "123456", DateTime.UtcNow.AddMinutes(10))));
        _users.Setup(u => u.ExistsByCorreoAsync("nuevo@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _codes.Setup(c => c.ConsumirAsync(1, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _users.Setup(u => u.ActualizarCorreoAsync(1, "nuevo@test.com", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.ConfirmarCambioAsync("token", "nuevo@test.com", "123456", "127.0.0.1", CancellationToken.None);

        result.Should().Be("Correo actualizado correctamente");
    }

    [Fact]
    public async Task Reanudar_CorreoInvalido_RetornaGenerico()
    {
        var sut = CreateSut();
        var (requiere, mensaje) = await sut.ReanudarAsync("invalid", CancellationToken.None);

        requiere.Should().BeFalse();
        mensaje.Should().Contain("Si el correo");
    }

    [Fact]
    public async Task Reanudar_UsuarioNoEncontrado_RetornaGenerico()
    {
        _users.Setup(u => u.FindByCorreoAsync("noexiste@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var (requiere, mensaje) = await sut.ReanudarAsync("noexiste@test.com", CancellationToken.None);

        requiere.Should().BeFalse();
    }

    [Fact]
    public async Task Reanudar_EstadoNoPendiente_RetornaGenerico()
    {
        var user = new UsuarioDto { IdUsuario = 5, IdEstadoCuenta = 1, Correo = "u@test.com", Nombre = "U" };
        _users.Setup(u => u.FindByCorreoAsync("u@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetEstadoCuentaNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Activa");

        var sut = CreateSut();
        var (requiere, mensaje) = await sut.ReanudarAsync("u@test.com", CancellationToken.None);

        requiere.Should().BeFalse();
    }

    [Fact]
    public async Task Reanudar_PendienteVerificacion_EnviaCodigoYRetornaTrue()
    {
        var user = new UsuarioDto { IdUsuario = 5, IdEstadoCuenta = 2, Correo = "u@test.com", Nombre = "U" };
        _users.Setup(u => u.FindByCorreoAsync("u@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetEstadoCuentaNombreAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync("PendienteVerificacion");
        _codes.Setup(c => c.CrearAsync(5, It.IsAny<string>(), "Registro", It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _notificacionesRepo.Setup(n => n.FindByTipoAsync("CODE_SENT", It.IsAny<CancellationToken>())).ReturnsAsync((NotificacionRow?)null);
        _emailSender.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = CreateSut();
        var (requiere, mensaje) = await sut.ReanudarAsync("u@test.com", CancellationToken.None);

        requiere.Should().BeTrue();
        mensaje.Should().Contain("nuevo código");
        _codes.Verify(c => c.CrearAsync(5, It.IsAny<string>(), "Registro", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
