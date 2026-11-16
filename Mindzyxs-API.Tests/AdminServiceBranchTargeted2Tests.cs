using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Mindzyxs_API.Utilities;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class AdminServiceBranchTargeted2Tests
{
    private readonly Mock<IAdminRepository> _admin = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICatalogRepository> _catalogs = new();
    private readonly Mock<INotificacionRepository> _notificaciones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();
    private readonly Mock<LoginService> _login;
    private readonly Mock<PasswordRecoveryService> _recovery;
    private readonly Mock<IEmailSender> _email = new();
    private readonly Mock<ITarjetaStorage> _tarjetas = new();
    private readonly Mock<Microsoft.Extensions.Options.IOptions<AdminOptions>> _adminOptions = new();
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<AdminService>> _logger = new();

    public AdminServiceBranchTargeted2Tests()
    {
        _adminOptions.Setup(o => o.Value).Returns(new AdminOptions { BackupsPath = "C:\\temp\\backups", RetencionLogDias = 90, EnableNativeSqlBackup = false, BackupConnectionString = "" });
        var notifRepo = new Mock<INotificacionRepository>();
        _recovery = new Mock<PasswordRecoveryService>(_users.Object, new Mock<ICodigoRepository>().Object, new Mock<ISesionRepository>().Object, _authLogs.Object, new NotificationService(notifRepo.Object, _email.Object, _authLogs.Object), Microsoft.Extensions.Options.Options.Create(new RegistroOptions { CodigoExpiraMinutos = 15 }), new Mock<Microsoft.Extensions.Logging.ILogger<PasswordRecoveryService>>().Object);
        _login = new Mock<LoginService>(_users.Object, _catalogs.Object, new Mock<ISesionRepository>().Object, _authLogs.Object, Microsoft.Extensions.Options.Options.Create(new SesionOptions { ExpiraHoras = 8 }), new Mock<Microsoft.Extensions.Logging.ILogger<LoginService>>().Object);
    }

    private AdminService CreateSut()
    {
        _adminOptions.Setup(o => o.Value).Returns(new AdminOptions { BackupsPath = "C:\\temp\\backups", RetencionLogDias = 90, EnableNativeSqlBackup = false, BackupConnectionString = "" });
        return new AdminService(_admin.Object, _users.Object, _catalogs.Object, _notificaciones.Object, _authLogs.Object, _login.Object, _recovery.Object, _email.Object, _tarjetas.Object, _adminOptions.Object, _logger.Object);
    }

    [Theory]
    [InlineData("CODE_SENT", "Verificación de cuenta")]
    [InlineData("PASSWORD_RECOVERY", "Recuperación de contraseña")]
    [InlineData("APPROVAL_GRANTED", "Aprobación de psicólogo")]
    [InlineData("APPROVAL_REJECTED", "Rechazo de psicólogo")]
    [InlineData("REMINDER", "Recordatorio diario")]
    [InlineData("RISK_HIGH", "Alerta de riesgo alto")]
    public async Task PreviewNotificacion_TodosLosTipos_RetornaOk(string tipo, string nombreEsperado)
    {
        _notificaciones.Setup(n => n.FindByTipoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new NotificacionRow { Tipo = tipo, Canales = "correo", Activo = true, Asunto = "a", Cuerpo = "b" });

        var sut = CreateSut();
        var vm = new PreviewNotificacionVm();
        var result = await sut.PreviewNotificacionAsync(tipo, vm, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Nombre.Should().Be(nombreEsperado);
    }

    [Theory]
    [InlineData("CODE_SENT")]
    [InlineData("PASSWORD_RECOVERY")]
    [InlineData("APPROVAL_GRANTED")]
    [InlineData("APPROVAL_REJECTED")]
    [InlineData("REMINDER")]
    [InlineData("RISK_HIGH")]
    public async Task DetalleNotificacion_TodosLosTipos_RetornaOk(string tipo)
    {
        _notificaciones.Setup(n => n.FindByTipoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new NotificacionRow { Tipo = tipo, Canales = "correo", Activo = true });

        var sut = CreateSut();
        var result = await sut.DetalleNotificacionAsync(tipo, CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [Theory]
    [InlineData("CODE_SENT")]
    [InlineData("PASSWORD_RECOVERY")]
    public async Task ActualizarNotificacion_TodosLosTipos_RetornaOk(string tipo)
    {
        _notificaciones.Setup(n => n.GuardarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = CreateSut();
        var vm = new ActualizarNotificacionVm { Canales = new List<string> { "correo" }, Activo = true, Cuerpo = "body", Asunto = "subj" };
        var result = await sut.ActualizarNotificacionAsync(1, tipo, vm, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task CambiarEstado_EstadoValido_RetornaOk()
    {
        var user = new UsuarioDto { IdUsuario = 5, IdRol = 1 };
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");
        _catalogs.Setup(c => c.GetEstadoIdAsync("Activa", It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _users.Setup(u => u.SetEstadoCuentaAsync(5, 1, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.CambiarEstadoAsync(1, 5, "Activa", "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ForzarReset_Valido_RetornaOk()
    {
        var user = new UsuarioDto { IdUsuario = 5, IdRol = 1, Correo = "u@test.com", Nombre = "U", Apellido = "P" };
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");
        _recovery.Setup(r => r.ForgotAsync("u@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(OperationResult.Ok("ok"));
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.ForzarResetAsync(1, 5, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task DetalleUsuario_Valido_RetornaOk()
    {
        var user = new UsuarioDto { IdUsuario = 5, IdRol = 1, Nombre = "Ana", Apellido = "Paz", Correo = "a@test.com", IdEstadoCuenta = 1, FechaRegistro = DateTime.UtcNow };
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");
        _users.Setup(u => u.GetEstadoCuentaNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
        _catalogs.Setup(c => c.GetGenerosAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<CatalogItemDto>());

        var sut = CreateSut();
        var result = await sut.DetalleUsuarioAsync(5, CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Usuarios_SinFiltros_RetornaLista()
    {
        _catalogs.Setup(c => c.GetRolIdAsync(AppRoles.Administrador, It.IsAny<CancellationToken>())).ReturnsAsync(3);
        _users.Setup(u => u.GetUsuariosAsync(3, It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UsuarioAdminRow> { new(1, "A", "B", "a@test.com", 1, "Paciente", 1, "Activa", null, null, DateTime.UtcNow) });

        var sut = CreateSut();
        var result = await sut.UsuariosAsync(null, null, null, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task Notificaciones_RetornaLista()
    {
        _notificaciones.Setup(n => n.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<NotificacionRow>
        {
            new() { Tipo = "CODE_SENT", Canales = "correo", Activo = true }
        });

        var sut = CreateSut();
        var result = await sut.NotificacionesAsync(CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
    }
}
