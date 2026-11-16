using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Mindzyxs_API.Utilities;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class AdminServiceHappyPathsTests
{
    private readonly Mock<IAdminRepository> _admin = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICatalogRepository> _catalogs = new();
    private readonly Mock<INotificacionRepository> _notificaciones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();
    private readonly Mock<LoginService> _login = new();
    private readonly Mock<IEmailSender> _email = new();
    private readonly Mock<ITarjetaStorage> _tarjetas = new();
    private readonly Mock<Microsoft.Extensions.Options.IOptions<AdminOptions>> _adminOptions = new();
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<AdminService>> _logger = new();

    private AdminService CreateSut()
    {
        _adminOptions.Setup(o => o.Value).Returns(new AdminOptions { BackupsPath = "C:\\temp\\backups", RetencionLogDias = 90 });
        var notifRepo = new Mock<INotificacionRepository>();
        var recovery = new PasswordRecoveryService(_users.Object, new Mock<ICodigoRepository>().Object, new Mock<ISesionRepository>().Object, _authLogs.Object, new NotificationService(notifRepo.Object, _email.Object, _authLogs.Object), Microsoft.Extensions.Options.Options.Create(new RegistroOptions { CodigoExpiraMinutos = 15 }), new Mock<Microsoft.Extensions.Logging.ILogger<PasswordRecoveryService>>().Object);
        var login = new LoginService(_users.Object, _catalogs.Object, new Mock<ISesionRepository>().Object, _authLogs.Object, Microsoft.Extensions.Options.Options.Create(new SesionOptions { ExpiraHoras = 8 }), new Mock<Microsoft.Extensions.Logging.ILogger<LoginService>>().Object);
        return new AdminService(_admin.Object, _users.Object, _catalogs.Object, _notificaciones.Object, _authLogs.Object, login, recovery, _email.Object, _tarjetas.Object, _adminOptions.Object, _logger.Object);
    }

    [Fact]
    public async Task VerificarTarjeta_Valido_RetornaExito()
    {
        var user = new UsuarioDto { IdUsuario = 5, IdRol = 2 };
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Psicologo");
        _users.Setup(u => u.VerificarEspecialidadesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _users.Setup(u => u.SetTarjetaVerificadaAsync(5, true, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.VerificarTarjetaAsync(1, 5, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("verificada");
    }

    [Fact]
    public async Task AprobarPsicologo_Valido_RetornaExito()
    {
        var user = new UsuarioDto { IdUsuario = 5, IdRol = 2, Nombre = "Laura", Apellido = "Gomez", Correo = "laura@test.com" };
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Psicologo");
        _catalogs.Setup(c => c.GetEstadoIdAsync("PendienteAprobacion", It.IsAny<CancellationToken>())).ReturnsAsync(4);
        _catalogs.Setup(c => c.GetEstadoIdAsync("Aprobado", It.IsAny<CancellationToken>())).ReturnsAsync(5);
        _catalogs.Setup(c => c.GetEstadoIdAsync("Activa", It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _users.Setup(u => u.AprobarPsicologoAsync(5, 4, 5, 1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _email.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.AprobarPsicologoAsync(1, 5, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task RechazarPsicologo_Valido_RetornaExito()
    {
        var user = new UsuarioDto { IdUsuario = 5, IdRol = 2, Nombre = "Laura", Apellido = "Gomez", Correo = "laura@test.com" };
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Psicologo");
        _catalogs.Setup(c => c.GetEstadoIdAsync("PendienteAprobacion", It.IsAny<CancellationToken>())).ReturnsAsync(4);
        _catalogs.Setup(c => c.GetEstadoIdAsync("Rechazado", It.IsAny<CancellationToken>())).ReturnsAsync(6);
        _catalogs.Setup(c => c.GetEstadoIdAsync("Inactiva", It.IsAny<CancellationToken>())).ReturnsAsync(7);
        _users.Setup(u => u.RechazarPsicologoAsync(5, 4, 6, 7, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _email.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.RechazarPsicologoAsync(1, 5, "motivo", "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task DetalleValidacion_Valido_RetornaDetalle()
    {
        var user = new UsuarioDto { IdUsuario = 5, IdRol = 2, Nombre = "Laura", Apellido = "Gomez", Correo = "laura@test.com" };
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Psicologo");
        _catalogs.Setup(c => c.GetEstadoIdAsync("PendienteAprobacion", It.IsAny<CancellationToken>())).ReturnsAsync(4);
        _users.Setup(u => u.FindPerfilProfesionalAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new UsuarioDto { IdUsuario = 5, FilePath = "path/to/tarjeta.pdf" });

        var sut = CreateSut();
        var result = await sut.DetalleValidacionAsync(5, CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task CambiarEstado_Valido_RetornaExito()
    {
        var user = new UsuarioDto { IdUsuario = 2, IdRol = 1, IdEstadoCuenta = 1 };
        _users.Setup(u => u.FindByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");
        _catalogs.Setup(c => c.GetEstadoIdAsync("Suspendida", It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _users.Setup(u => u.SetEstadoCuentaAsync(2, 2, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.CambiarEstadoAsync(1, 2, "Suspendida", "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ForzarReset_Valido_RetornaExito()
    {
        var user = new UsuarioDto { IdUsuario = 2, IdRol = 1, Correo = "paciente@test.com", Nombre = "Camila" };
        _users.Setup(u => u.FindByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");
        _users.Setup(u => u.FindByCorreoAsync("paciente@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.ForzarResetAsync(1, 2, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task TarjetaPath_Valido_RetornaPath()
    {
        var perfil = new UsuarioDto { IdUsuario = 5, FilePath = "psy_5_abc.pdf" };
        _users.Setup(u => u.FindPerfilProfesionalAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(perfil);

        var sut = CreateSut();
        var result = await sut.TarjetaPathAsync(5, CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task TarjetaPath_SinTarjeta_RetornaFail()
    {
        _users.Setup(u => u.FindPerfilProfesionalAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var result = await sut.TarjetaPathAsync(5, CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Notificaciones_RetornaListaConDatos()
    {
        var notifs = new List<NotificacionRow> { new() { IdConfig = 1, IdAdmin = 1, Tipo = "CODE_SENT", Canales = "correo", Activo = true } };
        _notificaciones.Setup(n => n.GetAllAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(notifs));

        var sut = CreateSut();
        var result = await sut.NotificacionesAsync(CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task DetalleNotificacion_Valido_RetornaDetalle()
    {
        var notif = new NotificacionRow { IdConfig = 1, IdAdmin = 1, Tipo = "CODE_SENT", Canales = "correo", Asunto = "Test", Activo = true };
        _notificaciones.Setup(n => n.FindByTipoAsync("CODE_SENT", It.IsAny<CancellationToken>())).ReturnsAsync(notif);

        var sut = CreateSut();
        var result = await sut.DetalleNotificacionAsync("CODE_SENT", CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ActualizarNotificacion_Valido_RetornaExito()
    {
        _notificaciones.Setup(n => n.FindByTipoAsync("CODE_SENT", It.IsAny<CancellationToken>())).ReturnsAsync(
            new NotificacionRow { IdConfig = 1, IdAdmin = 1, Tipo = "CODE_SENT", Canales = "correo", Asunto = "Test", Activo = true });
        _notificaciones.Setup(n => n.GuardarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var vm = new ActualizarNotificacionVm { Activo = true, Canales = new List<string> { "correo" }, Asunto = "Test", Cuerpo = "Body" };
        var result = await sut.ActualizarNotificacionAsync(1, "CODE_SENT", vm, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Notificación actualizada correctamente.");
    }

    [Fact]
    public async Task PreviewNotificacion_Valido_RetornaPreview()
    {
        var notif = new NotificacionRow { IdConfig = 1, IdAdmin = 1, Tipo = "CODE_SENT", Canales = "correo", Asunto = "Hola {nombre}", Cuerpo = "Código: {codigo}", Activo = true };
        _notificaciones.Setup(n => n.FindByTipoAsync("CODE_SENT", It.IsAny<CancellationToken>())).ReturnsAsync(notif);

        var sut = CreateSut();
        var vm = new PreviewNotificacionVm { Nombre = "Camila", Codigo = "123456" };
        var result = await sut.PreviewNotificacionAsync("CODE_SENT", vm, CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Usuarios_ConDatos_RetornaLista()
    {
        var usuarios = new List<UsuarioAdminRow> { new(1, "Admin", "Test", "admin@test.com", 3, "Administrador", 1, "Activa", null, null, DateTime.UtcNow) };
        _users.Setup(u => u.GetUsuariosAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuarios);

        var sut = CreateSut();
        var result = await sut.UsuariosAsync(null, null, null, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task DetalleUsuario_Valido_RetornaDetalle()
    {
        var user = new UsuarioDto { IdUsuario = 2, Nombre = "Camila", Apellido = "Rojas", Correo = "camila@test.com", IdRol = 1, IdEstadoCuenta = 1, FechaRegistro = DateTime.UtcNow };
        _users.Setup(u => u.FindByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");
        _users.Setup(u => u.GetEstadoCuentaNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Activa");

        var sut = CreateSut();
        var result = await sut.DetalleUsuarioAsync(2, CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    // GetAdminIdAsync tests removed because LoginService.ValidateSessionAsync is not mockable
    // (non-virtual method). Tested indirectly through integration tests.
}
