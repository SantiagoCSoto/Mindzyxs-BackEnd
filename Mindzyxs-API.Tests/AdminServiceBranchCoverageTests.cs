using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Mindzyxs_API.Utilities;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class AdminServiceBranchCoverageTests
{
    private readonly Mock<IAdminRepository> _admin = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICatalogRepository> _catalogs = new();
    private readonly Mock<INotificacionRepository> _notificaciones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();
    private readonly Mock<LoginService> _login;
    private readonly Mock<PasswordRecoveryService> _recovery;

    public AdminServiceBranchCoverageTests()
    {
        _login = new Mock<LoginService>(_users.Object, _catalogs.Object, new Mock<ISesionRepository>().Object, _authLogs.Object, Microsoft.Extensions.Options.Options.Create(new SesionOptions { ExpiraHoras = 8 }), new Mock<Microsoft.Extensions.Logging.ILogger<LoginService>>().Object);
        var notifRepo = new Mock<INotificacionRepository>();
        _recovery = new Mock<PasswordRecoveryService>(_users.Object, new Mock<ICodigoRepository>().Object, new Mock<ISesionRepository>().Object, _authLogs.Object, new NotificationService(notifRepo.Object, _email.Object, _authLogs.Object), Microsoft.Extensions.Options.Options.Create(new RegistroOptions { CodigoExpiraMinutos = 15 }), new Mock<Microsoft.Extensions.Logging.ILogger<PasswordRecoveryService>>().Object);
    }
    private readonly Mock<IEmailSender> _email = new();
    private readonly Mock<ITarjetaStorage> _tarjetas = new();
    private readonly Mock<Microsoft.Extensions.Options.IOptions<AdminOptions>> _adminOptions = new();
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<AdminService>> _logger = new();

    private AdminService CreateSut()
    {
        _adminOptions.Setup(o => o.Value).Returns(new AdminOptions { BackupsPath = "C:\\temp\\backups", RetencionLogDias = 90, EnableNativeSqlBackup = false });
        return new AdminService(_admin.Object, _users.Object, _catalogs.Object, _notificaciones.Object, _authLogs.Object, _login.Object, _recovery.Object, _email.Object, _tarjetas.Object, _adminOptions.Object, _logger.Object);
    }

    [Fact]
    public async Task GetAdminId_ConSesionValidaAdmin_RetornaId()
    {
        var user = new UsuarioDto { IdUsuario = 5, IdRol = 3 };
        _login.Setup(l => l.ValidateSessionAsync("token", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<UsuarioDto>.Ok(user, "ok"));
        _users.Setup(u => u.GetRolNombreAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync("Administrador");
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.GetAdminIdAsync("token", "127.0.0.1", CancellationToken.None);

        result.Should().Be(5);
    }

    [Fact]
    public async Task GetAdminId_ConSesionValidaNoAdmin_RetornaNull()
    {
        var user = new UsuarioDto { IdUsuario = 7, IdRol = 1 };
        _login.Setup(l => l.ValidateSessionAsync("token", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<UsuarioDto>.Ok(user, "ok"));
        _users.Setup(u => u.GetRolNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.GetAdminIdAsync("token", "127.0.0.1", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAdminId_SesionInvalida_RetornaNull()
    {
        _login.Setup(l => l.ValidateSessionAsync("token", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<UsuarioDto>.Fail("expired"));

        var sut = CreateSut();
        var result = await sut.GetAdminIdAsync("token", "127.0.0.1", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task PendientesTarjeta_RetornaLista()
    {
        _catalogs.Setup(c => c.GetRolIdAsync(AppRoles.Psicologo, It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _users.Setup(u => u.GetPsicologosTarjetaPendienteAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(new List<PsicologoTarjetaPendienteRow>
        {
            new(5, "Laura", "l@test.com", false, 0)
        });
        _users.Setup(u => u.GetEspecialidadesPendientesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new List<string> { "Ansiedad" });

        var sut = CreateSut();
        var result = await sut.PendientesTarjetaAsync(CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task VerificarTarjeta_NoEsPsicologo_RetornaFail()
    {
        var user = new UsuarioDto { IdUsuario = 5, IdRol = 1 };
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");

        var sut = CreateSut();
        var result = await sut.VerificarTarjetaAsync(1, 5, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("no es psicólogo");
    }

    [Fact]
    public async Task VerificarTarjeta_NoEncontrado_RetornaFail()
    {
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var result = await sut.VerificarTarjetaAsync(1, 5, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("no encontrado");
    }

    [Fact]
    public async Task PendientesValidacion_RetornaLista()
    {
        _catalogs.Setup(c => c.GetRolIdAsync(AppRoles.Psicologo, It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _catalogs.Setup(c => c.GetEstadoIdAsync(AccountStates.PendienteAprobacion, It.IsAny<CancellationToken>())).ReturnsAsync(4);
        _users.Setup(u => u.GetPsicologosPendientesAsync(2, 4, It.IsAny<CancellationToken>())).ReturnsAsync(new List<PsicologoPendienteRow>
        {
            new(5, "Laura", "l@test.com", DateTime.UtcNow)
        });

        var sut = CreateSut();
        var result = await sut.PendientesValidacionAsync(CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task AprobarPsicologo_NoEncontrado_RetornaFail()
    {
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var result = await sut.AprobarPsicologoAsync(1, 5, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("no encontrado");
    }

    [Fact]
    public async Task AprobarPsicologo_NoEsPsicologo_RetornaFail()
    {
        var user = new UsuarioDto { IdUsuario = 5, IdRol = 1 };
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");

        var sut = CreateSut();
        var result = await sut.AprobarPsicologoAsync(1, 5, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("no es psicólogo");
    }

    [Fact]
    public async Task DetalleValidacion_NoEncontrado_RetornaFail()
    {
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var result = await sut.DetalleValidacionAsync(5, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("no encontrado");
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
    public async Task TarjetaStream_SinTarjeta_RetornaFail()
    {
        _users.Setup(u => u.FindPerfilProfesionalAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var result = await sut.TarjetaStreamAsync(5, CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task RechazarPsicologo_NoEncontrado_RetornaFail()
    {
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var result = await sut.RechazarPsicologoAsync(1, 5, "motivo", "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("no encontrado");
    }

    [Fact]
    public async Task RechazarPsicologo_NoEsPsicologo_RetornaFail()
    {
        var user = new UsuarioDto { IdUsuario = 5, IdRol = 1 };
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");

        var sut = CreateSut();
        var result = await sut.RechazarPsicologoAsync(1, 5, "motivo", "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("no es psicólogo");
    }

    [Fact]
    public async Task Usuarios_ConRolAdmin_RetornaFail()
    {
        _catalogs.Setup(c => c.GetRolIdAsync(AppRoles.Administrador, It.IsAny<CancellationToken>())).ReturnsAsync(3);
        _catalogs.Setup(c => c.GetRolIdAsync("Administrador", It.IsAny<CancellationToken>())).ReturnsAsync(3);

        var sut = CreateSut();
        var result = await sut.UsuariosAsync(null, "Administrador", null, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("No puedes administrar");
    }

    [Fact]
    public async Task DetalleUsuario_NoEncontrado_RetornaFail()
    {
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var result = await sut.DetalleUsuarioAsync(5, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("no encontrado");
    }

    [Fact]
    public async Task DetalleUsuario_Admin_RetornaFail()
    {
        var user = new UsuarioDto { IdUsuario = 5, IdRol = 3 };
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Administrador");

        var sut = CreateSut();
        var result = await sut.DetalleUsuarioAsync(5, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("No puedes administrar");
    }

    [Fact]
    public async Task CambiarEstado_SinEstado_RetornaFail()
    {
        var sut = CreateSut();
        var result = await sut.CambiarEstadoAsync(1, 5, "  ", "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Debes seleccionar");
    }

    [Fact]
    public async Task CambiarEstado_UsuarioNoEncontrado_RetornaFail()
    {
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var result = await sut.CambiarEstadoAsync(1, 5, "Activa", "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("no encontrado");
    }

    [Fact]
    public async Task CambiarEstado_Admin_RetornaFail()
    {
        var user = new UsuarioDto { IdUsuario = 5, IdRol = 3 };
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Administrador");

        var sut = CreateSut();
        var result = await sut.CambiarEstadoAsync(1, 5, "Activa", "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("No puedes administrar");
    }

    [Fact]
    public async Task CambiarEstado_EstadoInvalido_RetornaFail()
    {
        var user = new UsuarioDto { IdUsuario = 5, IdRol = 1 };
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");
        _catalogs.Setup(c => c.GetEstadoIdAsync("Invalido", It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("not found"));

        var sut = CreateSut();
        var result = await sut.CambiarEstadoAsync(1, 5, "Invalido", "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Estado no válido");
    }

    [Fact]
    public async Task ForzarReset_UsuarioNoEncontrado_RetornaFail()
    {
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var result = await sut.ForzarResetAsync(1, 5, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("no encontrado");
    }

    [Fact]
    public async Task ForzarReset_Admin_RetornaFail()
    {
        var user = new UsuarioDto { IdUsuario = 5, IdRol = 3 };
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Administrador");

        var sut = CreateSut();
        var result = await sut.ForzarResetAsync(1, 5, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("No puedes administrar");
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
    public async Task DetalleNotificacion_TipoInvalido_RetornaFail()
    {
        var sut = CreateSut();
        var result = await sut.DetalleNotificacionAsync("INVALIDO", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("no encontrado");
    }

    [Fact]
    public async Task DetalleNotificacion_NoEncontrado_RetornaFail()
    {
        _notificaciones.Setup(n => n.FindByTipoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((NotificacionRow?)null);

        var sut = CreateSut();
        var result = await sut.DetalleNotificacionAsync("CODE_SENT", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("no encontrado");
    }

    [Fact]
    public async Task ActualizarNotificacion_TipoInvalido_RetornaFail()
    {
        var sut = CreateSut();
        var vm = new ActualizarNotificacionVm { Canales = new List<string> { "correo" }, Activo = true, Cuerpo = "body", Asunto = "subj" };
        var result = await sut.ActualizarNotificacionAsync(1, "INVALIDO", vm, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("no encontrado");
    }

    [Fact]
    public async Task ActualizarNotificacion_ActivoSinCanales_RetornaFail()
    {
        var sut = CreateSut();
        var vm = new ActualizarNotificacionVm { Canales = new List<string>(), Activo = true, Cuerpo = "body", Asunto = "subj" };
        var result = await sut.ActualizarNotificacionAsync(1, "CODE_SENT", vm, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("al menos un canal");
    }

    [Fact]
    public async Task ActualizarNotificacion_ActivoSinCuerpo_RetornaFail()
    {
        var sut = CreateSut();
        var vm = new ActualizarNotificacionVm { Canales = new List<string> { "correo" }, Activo = true, Cuerpo = "", Asunto = "subj" };
        var result = await sut.ActualizarNotificacionAsync(1, "CODE_SENT", vm, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("plantilla no puede estar vacía");
    }

    [Fact]
    public async Task ActualizarNotificacion_ActivoSinAsunto_RetornaFail()
    {
        var sut = CreateSut();
        var vm = new ActualizarNotificacionVm { Canales = new List<string> { "correo" }, Activo = true, Cuerpo = "body", Asunto = "" };
        var result = await sut.ActualizarNotificacionAsync(1, "CODE_SENT", vm, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("asunto es obligatorio");
    }

    [Fact]
    public async Task PreviewNotificacion_TipoInvalido_RetornaFail()
    {
        var sut = CreateSut();
        var vm = new PreviewNotificacionVm();
        var result = await sut.PreviewNotificacionAsync("INVALIDO", vm, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("no encontrado");
    }

    [Fact]
    public async Task PreviewNotificacion_NoEncontrado_RetornaFail()
    {
        _notificaciones.Setup(n => n.FindByTipoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((NotificacionRow?)null);

        var sut = CreateSut();
        var vm = new PreviewNotificacionVm();
        var result = await sut.PreviewNotificacionAsync("CODE_SENT", vm, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("no encontrado");
    }

    [Fact]
    public async Task TarjetaStream_FileNotFound_RetornaFail()
    {
        _users.Setup(u => u.FindPerfilProfesionalAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new UsuarioDto { IdUsuario = 5, FilePath = "path.pdf" });
        _tarjetas.Setup(t => t.OpenReadAsync("path.pdf", It.IsAny<CancellationToken>())).ThrowsAsync(new FileNotFoundException());

        var sut = CreateSut();
        var result = await sut.TarjetaStreamAsync(5, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("No se ha adjuntado");
    }

    [Fact]
    public async Task CreateBackup_Error_RetornaFail()
    {
        _admin.Setup(a => a.ExportSnapshotJsonAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("boom"));

        var sut = CreateSut();
        var result = await sut.CreateBackupAsync(1, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("No fue posible");
    }

    [Fact]
    public async Task RunMaintenance_Error_RetornaFail()
    {
        _admin.Setup(a => a.LimpiarTodoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("boom"));

        var sut = CreateSut();
        var result = await sut.RunMaintenanceAsync(1, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("No fue posible");
    }
}
