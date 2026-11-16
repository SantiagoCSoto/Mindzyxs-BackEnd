using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Mindzyxs_API.Utilities;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class AdminServiceBranchTargeted3Tests
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

    public AdminServiceBranchTargeted3Tests()
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

    [Fact]
    public async Task DetalleValidacion_PerfilNull_RetornaFail()
    {
        var user = new UsuarioDto { IdUsuario = 5, IdRol = 2 };
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Psicologo");
        _users.Setup(u => u.FindPerfilProfesionalAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var result = await sut.DetalleValidacionAsync(5, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("no encontrado");
    }

    [Fact]
    public async Task DetalleValidacion_SinAprobacion_RetornaOk()
    {
        var user = new UsuarioDto { IdUsuario = 5, IdRol = 2 };
        var perfil = new UsuarioDto { IdUsuario = 5, IdRol = 2, IdEstadoAprobacion = null, Nombre = "Laura", Apellido = "Gomez", Correo = "l@test.com", FechaRegistro = DateTime.UtcNow, Especialidades = new List<string>() };
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Psicologo");
        _users.Setup(u => u.FindPerfilProfesionalAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(perfil);

        var sut = CreateSut();
        var result = await sut.DetalleValidacionAsync(5, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.EstadoAprobacion.Should().Be("");
    }

    [Fact]
    public async Task TarjetaPath_Valido_RetornaPath()
    {
        _users.Setup(u => u.FindPerfilProfesionalAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new UsuarioDto { IdUsuario = 5, FilePath = "path/to/tarjeta.pdf" });

        var sut = CreateSut();
        var result = await sut.TarjetaPathAsync(5, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().Be("path/to/tarjeta.pdf");
    }

    [Fact]
    public async Task TarjetaPath_SinTarjeta_RetornaFail()
    {
        _users.Setup(u => u.FindPerfilProfesionalAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new UsuarioDto { IdUsuario = 5, FilePath = null });

        var sut = CreateSut();
        var result = await sut.TarjetaPathAsync(5, CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task TarjetaStream_Valido_RetornaStream()
    {
        _users.Setup(u => u.FindPerfilProfesionalAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new UsuarioDto { IdUsuario = 5, FilePath = "path.pdf" });
        var ms = new MemoryStream(new byte[] { 1, 2, 3 });
        _tarjetas.Setup(t => t.OpenReadAsync("path.pdf", It.IsAny<CancellationToken>())).ReturnsAsync(ms);

        var sut = CreateSut();
        var result = await sut.TarjetaStreamAsync(5, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task TarjetaStream_FileNotFound_RetornaFail()
    {
        _users.Setup(u => u.FindPerfilProfesionalAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new UsuarioDto { IdUsuario = 5, FilePath = "path.pdf" });
        _tarjetas.Setup(t => t.OpenReadAsync("path.pdf", It.IsAny<CancellationToken>())).ThrowsAsync(new FileNotFoundException());

        var sut = CreateSut();
        var result = await sut.TarjetaStreamAsync(5, CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CambiarEstado_Valido_RetornaOk()
    {
        var user = new UsuarioDto { IdUsuario = 5, IdRol = 1 };
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");
        _catalogs.Setup(c => c.GetEstadoIdAsync("Activa", It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _users.Setup(u => u.SetEstadoCuentaAsync(5, 1, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

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
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.ForzarResetAsync(1, 5, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeTrue();
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

    [Fact]
    public async Task CreateBackup_Error_RetornaFail()
    {
        _admin.Setup(a => a.ExportSnapshotJsonAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("boom"));

        var sut = CreateSut();
        var result = await sut.CreateBackupAsync(1, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task RunMaintenance_Error_RetornaFail()
    {
        _admin.Setup(a => a.LimpiarTodoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("boom"));

        var sut = CreateSut();
        var result = await sut.RunMaintenanceAsync(1, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
    }
}
