using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Mindzyxs_API.Utilities;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class AdminServiceBranchTargetedTests
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

    public AdminServiceBranchTargetedTests()
    {
        _adminOptions.Setup(o => o.Value).Returns(new AdminOptions { BackupsPath = "C:\\temp\\backups", RetencionLogDias = 90, EnableNativeSqlBackup = true, BackupConnectionString = "Server=test" });
        var notifRepo = new Mock<INotificacionRepository>();
        _recovery = new Mock<PasswordRecoveryService>(_users.Object, new Mock<ICodigoRepository>().Object, new Mock<ISesionRepository>().Object, _authLogs.Object, new NotificationService(notifRepo.Object, _email.Object, _authLogs.Object), Microsoft.Extensions.Options.Options.Create(new RegistroOptions { CodigoExpiraMinutos = 15 }), new Mock<Microsoft.Extensions.Logging.ILogger<PasswordRecoveryService>>().Object);
        _login = new Mock<LoginService>(_users.Object, _catalogs.Object, new Mock<ISesionRepository>().Object, _authLogs.Object, Microsoft.Extensions.Options.Options.Create(new SesionOptions { ExpiraHoras = 8 }), new Mock<Microsoft.Extensions.Logging.ILogger<LoginService>>().Object);
    }

    private AdminService CreateSut(AdminOptions? opts = null)
    {
        _adminOptions.Setup(o => o.Value).Returns(opts ?? new AdminOptions { BackupsPath = "C:\\temp\\backups", RetencionLogDias = 90, EnableNativeSqlBackup = true, BackupConnectionString = "Server=test" });
        return new AdminService(_admin.Object, _users.Object, _catalogs.Object, _notificaciones.Object, _authLogs.Object, _login.Object, _recovery.Object, _email.Object, _tarjetas.Object, _adminOptions.Object, _logger.Object);
    }

    [Fact]
    public async Task CreateBackup_NativeSuccess_RetornaOk()
    {
        Directory.CreateDirectory("C:\\temp\\backups");
        _admin.Setup(a => a.CreateSqlBackupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((path, ct) => File.WriteAllText(path, "x"))
            .Returns(Task.CompletedTask);
        _admin.Setup(a => a.CrearRespaldoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.CreateBackupAsync(1, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Tipo.Should().Be("bak");
    }

    [Fact]
    public async Task CreateBackup_NativeFails_FallbackJson_RetornaOk()
    {
        Directory.CreateDirectory("C:\\temp\\backups");
        _admin.Setup(a => a.CreateSqlBackupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("boom"));
        _admin.Setup(a => a.ExportSnapshotJsonAsync(It.IsAny<CancellationToken>())).ReturnsAsync("{}");
        _admin.Setup(a => a.CrearRespaldoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.CreateBackupAsync(1, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Tipo.Should().Be("json");
    }

    [Fact]
    public async Task CreateBackup_NativeDisabled_UsaJson()
    {
        Directory.CreateDirectory("C:\\temp\\backups");
        _admin.Setup(a => a.ExportSnapshotJsonAsync(It.IsAny<CancellationToken>())).ReturnsAsync("{}");
        _admin.Setup(a => a.CrearRespaldoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(3);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = CreateSut(new AdminOptions { BackupsPath = "C:\\temp\\backups", RetencionLogDias = 90, EnableNativeSqlBackup = false, BackupConnectionString = "" });
        var result = await sut.CreateBackupAsync(1, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Tipo.Should().Be("json");
    }

    [Fact]
    public async Task RunMaintenance_Success_RetornaOk()
    {
        _admin.Setup(a => a.LimpiarTodoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new MantenimientoLimpiezaResult(1, 2, 3));
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.RunMaintenanceAsync(1, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.CodigosExpiradosEliminados.Should().Be(1);
    }

    [Fact]
    public async Task GetAdminId_SesionInvalida_RetornaNull()
    {
        _login.Setup(l => l.ValidateSessionAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<UsuarioDto>.Fail("expired"));

        var sut = CreateSut();
        var result = await sut.GetAdminIdAsync("token", "127.0.0.1", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAdminId_RolNoAdmin_RetornaNull()
    {
        var user = new UsuarioDto { IdUsuario = 7, IdRol = 1 };
        _login.Setup(l => l.ValidateSessionAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<UsuarioDto>.Ok(user, "ok"));
        _users.Setup(u => u.GetRolNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.GetAdminIdAsync("token", "127.0.0.1", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBackupFilePath_Encontrado_RetornaPath()
    {
        _admin.Setup(a => a.FindRespaldoByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new RespaldoDto(1, "file.bak", "1KB", DateTime.UtcNow, 1));

        var sut = CreateSut();
        var result = await sut.GetBackupFilePath(1, CancellationToken.None);

        result.Should().Contain("file.bak");
    }
}
