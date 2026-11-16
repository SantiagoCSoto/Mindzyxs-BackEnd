using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mindzyxs_API.Controllers;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Services;
using Mindzyxs_API.Utilities;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Controllers;

public class AdminControllerTests
{
    private readonly Mock<AdminService> _admin;

    public AdminControllerTests()
    {
        var adminDeps = new object[]
        {
            new Mock<IAdminRepository>().Object, new Mock<IUserRepository>().Object,
            new Mock<ICatalogRepository>().Object, new Mock<INotificacionRepository>().Object,
            new Mock<IAuthLogRepository>().Object,
            new Mock<LoginService>(new Mock<IUserRepository>().Object, new Mock<ICatalogRepository>().Object, new Mock<ISesionRepository>().Object, new Mock<IAuthLogRepository>().Object, Microsoft.Extensions.Options.Options.Create(new SesionOptions { ExpiraHoras = 8 }), new Mock<Microsoft.Extensions.Logging.ILogger<LoginService>>().Object).Object,
            new Mock<PasswordRecoveryService>(new Mock<IUserRepository>().Object, new Mock<ICodigoRepository>().Object, new Mock<ISesionRepository>().Object, new Mock<IAuthLogRepository>().Object, new Mock<NotificationService>(new Mock<INotificacionRepository>().Object, new Mock<IEmailSender>().Object, new Mock<IAuthLogRepository>().Object).Object, Microsoft.Extensions.Options.Options.Create(new RegistroOptions { CodigoExpiraMinutos = 15 }), new Mock<Microsoft.Extensions.Logging.ILogger<PasswordRecoveryService>>().Object).Object,
            new Mock<IEmailSender>().Object, new Mock<ITarjetaStorage>().Object,
            Microsoft.Extensions.Options.Options.Create(new AdminOptions { BackupsPath = "C:\\temp\\backups", RetencionLogDias = 90 }),
            new Mock<Microsoft.Extensions.Logging.ILogger<AdminService>>().Object
        };
        _admin = new Mock<AdminService>(adminDeps) { CallBase = false };
    }

    private AdminController CreateSut() => new(_admin.Object);

    private static void SetHttpContext(AdminController sut, string? authHeader = null, string? ip = null)
    {
        var httpContext = new DefaultHttpContext();
        if (authHeader != null) httpContext.Request.Headers.Authorization = authHeader;
        if (ip != null) httpContext.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    private static string? ErrorDe(IActionResult result)
    {
        var value = (result as ObjectResult)?.Value;
        return value?.GetType().GetProperty("error")?.GetValue(value)?.ToString();
    }

    private void ConfigurarAdminAutenticado()
    {
        _admin.Setup(a => a.GetAdminIdAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    // ---- Pendientes de validacion: GET /api/admin/psicologos/pendientes ----

    [Fact]
    public async Task PendientesValidacion_SinToken_ReturnsForbidden()
    {
        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.PendientesValidacion(CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(403);
        ErrorDe(result).Should().Be("Tu sesión ha expirado. Inicia sesión nuevamente");
    }

    [Fact]
    public async Task PendientesValidacion_ConAdmin_ReturnsOk()
    {
        ConfigurarAdminAutenticado();
        _admin.Setup(a => a.PendientesValidacionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<List<PsicologoPendienteDto>>.Ok(new List<PsicologoPendienteDto>(), "OK"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.PendientesValidacion(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task PendientesValidacion_TokenInvalido_ReturnsForbiddenSinPermisos()
    {
        _admin.Setup(a => a.GetAdminIdAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token-invalido");

        var result = await sut.PendientesValidacion(CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(403);
        ErrorDe(result).Should().Be("No tienes permisos para acceder a esta función");
        _admin.Verify(a => a.PendientesValidacionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Backup: POST /api/admin/backup ----

    [Fact]
    public async Task Backup_SinToken_ReturnsForbidden()
    {
        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.Backup(CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(403);
        ErrorDe(result).Should().Be("Tu sesión ha expirado. Inicia sesión nuevamente");
    }

    [Fact]
    public async Task Backup_ConAdmin_ReturnsCreated()
    {
        ConfigurarAdminAutenticado();
        _admin.Setup(a => a.CreateBackupAsync(1, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<BackupResultDto>.Ok(new BackupResultDto { IdRespaldo = 7, NombreArchivo = "mindzyx_full.bak", Tipo = "bak" }, "Respaldo creado correctamente"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token", "10.0.0.7");

        var result = await sut.Backup(CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(201);
        ErrorDe(result).Should().BeNull();
        _admin.Verify(a => a.CreateBackupAsync(1, "10.0.0.7", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Backup_FalloDelServicio_ReturnsInternalServerError()
    {
        ConfigurarAdminAutenticado();
        _admin.Setup(a => a.CreateBackupAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<BackupResultDto>.Fail("No fue posible crear el respaldo. Intente nuevamente"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Backup(CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(500);
        ErrorDe(result).Should().Be("No fue posible crear el respaldo. Intente nuevamente");
    }

    // ---- List backups: GET /api/admin/backups ----

    [Fact]
    public async Task ListBackups_SinToken_ReturnsForbidden()
    {
        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.List(CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(403);
        ErrorDe(result).Should().Be("Tu sesión ha expirado. Inicia sesión nuevamente");
    }

    [Fact]
    public async Task ListBackups_ConAdmin_ReturnsOk()
    {
        ConfigurarAdminAutenticado();
        _admin.Setup(a => a.ListBackupsAsync(1, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<List<BackupListItemDto>>.Ok(new List<BackupListItemDto>(), "OK"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.List(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ListBackups_FalloDelServicio_ReturnsInternalServerError()
    {
        ConfigurarAdminAutenticado();
        _admin.Setup(a => a.ListBackupsAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<List<BackupListItemDto>>.Fail("No fue posible consultar los respaldos"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.List(CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(500);
        ErrorDe(result).Should().Be("No fue posible consultar los respaldos");
    }

    // ---- Maintenance: POST /api/admin/maintenance ----

    [Fact]
    public async Task Maintenance_SinToken_ReturnsForbidden()
    {
        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.Maintenance(CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(403);
        ErrorDe(result).Should().Be("Tu sesión ha expirado. Inicia sesión nuevamente");
    }

    [Fact]
    public async Task Maintenance_ConAdmin_ReturnsOk()
    {
        ConfigurarAdminAutenticado();
        _admin.Setup(a => a.RunMaintenanceAsync(1, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<MaintenanceResultDto>.Ok(new MaintenanceResultDto { CodigosExpiradosEliminados = 2, SesionesExpiradasEliminadas = 1, LogsAntiguosEliminados = 3 }, "Mantenimiento completado"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token", "10.0.0.7");

        var result = await sut.Maintenance(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _admin.Verify(a => a.RunMaintenanceAsync(1, "10.0.0.7", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Maintenance_FalloDelServicio_ReturnsInternalServerError()
    {
        ConfigurarAdminAutenticado();
        _admin.Setup(a => a.RunMaintenanceAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<MaintenanceResultDto>.Fail("No fue posible ejecutar el mantenimiento. Intente nuevamente"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Maintenance(CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(500);
        ErrorDe(result).Should().Be("No fue posible ejecutar el mantenimiento. Intente nuevamente");
    }

    // ---- Download backup: GET /api/admin/backups/{id}/download ----

    [Fact]
    public async Task Download_SinToken_ReturnsForbidden()
    {
        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.Download(1, CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(403);
        ErrorDe(result).Should().Be("Tu sesión ha expirado. Inicia sesión nuevamente");
    }

    [Fact]
    public async Task Download_ArchivoInexistente_ReturnsNotFound()
    {
        ConfigurarAdminAutenticado();
        _admin.Setup(a => a.GetBackupFilePath(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Download(99, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
        ErrorDe(result).Should().Be("Archivo de respaldo no encontrado");
    }

    [Fact]
    public async Task Download_ArchivoEnDisco_ReturnsPhysicalFile()
    {
        var temp = Path.Combine(Path.GetTempPath(), "mindzyxs-backup-test.bak");
        File.WriteAllText(temp, "contenido de prueba");

        ConfigurarAdminAutenticado();
        _admin.Setup(a => a.GetBackupFilePath(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(temp);

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Download(7, CancellationToken.None);

        result.Should().BeOfType<PhysicalFileResult>();
        var file = (PhysicalFileResult)result;
        file.FileName.Should().Be(temp);
        file.ContentType.Should().Be("application/octet-stream");

        File.Delete(temp);
    }

    [Fact]
    public async Task Download_ArchivoJson_ReturnsJsonContentType()
    {
        var temp = Path.Combine(Path.GetTempPath(), "mindzyxs-backup-test.json");
        File.WriteAllText(temp, "{}");

        ConfigurarAdminAutenticado();
        _admin.Setup(a => a.GetBackupFilePath(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(temp);

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Download(8, CancellationToken.None);

        result.Should().BeOfType<PhysicalFileResult>();
        var file = (PhysicalFileResult)result;
        file.ContentType.Should().Be("application/json");

        File.Delete(temp);
    }

    // ---- TarjetaPendientes: GET /api/admin/psicologos/tarjeta-pendientes ----

    [Fact]
    public async Task TarjetaPendientes_SinToken_ReturnsForbidden()
    {
        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.TarjetaPendientes(CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(403);
        ErrorDe(result).Should().Be("Tu sesión ha expirado. Inicia sesión nuevamente");
    }

    [Fact]
    public async Task TarjetaPendientes_ConAdmin_ReturnsOk()
    {
        ConfigurarAdminAutenticado();
        _admin.Setup(a => a.PendientesTarjetaAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<List<PsicologoTarjetaPendienteDto>>.Ok(new List<PsicologoTarjetaPendienteDto>(), "OK"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.TarjetaPendientes(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    // ---- VerificarTarjeta: PUT /api/admin/psicologos/{id}/tarjeta-verificada ----

    [Fact]
    public async Task VerificarTarjeta_SinToken_ReturnsForbidden()
    {
        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.VerificarTarjeta(5, CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(403);
        ErrorDe(result).Should().Be("Tu sesión ha expirado. Inicia sesión nuevamente");
    }

    [Fact]
    public async Task VerificarTarjeta_ConAdmin_ReturnsOk()
    {
        ConfigurarAdminAutenticado();
        _admin.Setup(a => a.VerificarTarjetaAsync(1, 5, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<string>.Ok("Tarjeta verificada correctamente. 3 especialidades visibles para los usuarios.", "OK"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.VerificarTarjeta(5, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task VerificarTarjeta_NoEncontrado_ReturnsNotFound()
    {
        ConfigurarAdminAutenticado();
        _admin.Setup(a => a.VerificarTarjetaAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<string>.Fail("Psicólogo no encontrado."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.VerificarTarjeta(99, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
        ErrorDe(result).Should().Be("Psicólogo no encontrado.");
    }

    // ---- DetalleValidacion: GET /api/admin/psicologos/{id}/validacion ----

    [Fact]
    public async Task DetalleValidacion_SinToken_ReturnsForbidden()
    {
        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.DetalleValidacion(5, CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(403);
        ErrorDe(result).Should().Be("Tu sesión ha expirado. Inicia sesión nuevamente");
    }

    [Fact]
    public async Task DetalleValidacion_ConAdmin_ReturnsOk()
    {
        ConfigurarAdminAutenticado();
        _admin.Setup(a => a.DetalleValidacionAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<ValidacionDetalleDto>.Ok(new ValidacionDetalleDto { Id = 5 }, "OK"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.DetalleValidacion(5, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DetalleValidacion_NoEncontrado_ReturnsNotFound()
    {
        ConfigurarAdminAutenticado();
        _admin.Setup(a => a.DetalleValidacionAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<ValidacionDetalleDto>.Fail("Psicólogo no encontrado."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.DetalleValidacion(5, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
        ErrorDe(result).Should().Be("Psicólogo no encontrado.");
    }

    // ---- DescargarTarjeta: GET /api/admin/psicologos/{id}/tarjeta ----

    [Fact]
    public async Task DescargarTarjeta_SinToken_ReturnsForbidden()
    {
        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.DescargarTarjeta(5, CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(403);
        ErrorDe(result).Should().Be("Tu sesión ha expirado. Inicia sesión nuevamente");
    }

    [Fact]
    public async Task DescargarTarjeta_ConAdmin_ReturnsFile()
    {
        var ms = new MemoryStream(new byte[] { 1, 2, 3 });
        ConfigurarAdminAutenticado();
        _admin.Setup(a => a.TarjetaStreamAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<Stream>.Ok(ms, "OK"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.DescargarTarjeta(5, CancellationToken.None);

        result.Should().BeOfType<FileStreamResult>();
        _admin.Verify(a => a.TarjetaStreamAsync(5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DescargarTarjeta_NoEncontrado_ReturnsNotFound()
    {
        ConfigurarAdminAutenticado();
        _admin.Setup(a => a.TarjetaStreamAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<Stream>.Fail("No se ha adjuntado tarjeta profesional."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.DescargarTarjeta(5, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
        ErrorDe(result).Should().Be("No se ha adjuntado tarjeta profesional.");
    }

    [Fact]
    public async Task DescargarTarjeta_FalloSinDatos_ReturnsNotFound()
    {
        ConfigurarAdminAutenticado();
        _admin.Setup(a => a.TarjetaStreamAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<Stream>.Ok(null!, "OK"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.DescargarTarjeta(5, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
        ErrorDe(result).Should().Be("OK");
    }

    // ---- AprobarPsicologo: POST /api/admin/psicologos/{id}/aprobar ----

    [Fact]
    public async Task AprobarPsicologo_SinToken_ReturnsForbidden()
    {
        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.AprobarPsicologo(5, CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(403);
        ErrorDe(result).Should().Be("Tu sesión ha expirado. Inicia sesión nuevamente");
    }

    [Fact]
    public async Task AprobarPsicologo_ConAdmin_ReturnsOk()
    {
        ConfigurarAdminAutenticado();
        _admin.Setup(a => a.AprobarPsicologoAsync(1, 5, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<string>.Ok("Validación registrada correctamente.", "OK"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.AprobarPsicologo(5, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AprobarPsicologo_NoEncontrado_ReturnsNotFound()
    {
        ConfigurarAdminAutenticado();
        _admin.Setup(a => a.AprobarPsicologoAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<string>.Fail("Psicólogo no encontrado."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.AprobarPsicologo(5, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
        ErrorDe(result).Should().Be("Psicólogo no encontrado.");
    }

    // ---- RechazarPsicologo: POST /api/admin/psicologos/{id}/rechazar ----

    [Fact]
    public async Task RechazarPsicologo_SinToken_ReturnsForbidden()
    {
        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.RechazarPsicologo(5, null, CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(403);
        ErrorDe(result).Should().Be("Tu sesión ha expirado. Inicia sesión nuevamente");
    }

    [Fact]
    public async Task RechazarPsicologo_ConAdmin_ReturnsOk()
    {
        ConfigurarAdminAutenticado();
        _admin.Setup(a => a.RechazarPsicologoAsync(1, 5, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<string>.Ok("Validación registrada correctamente.", "OK"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.RechazarPsicologo(5, new RechazarPsicologoVm { Motivo = "Documentacion incompleta" }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task RechazarPsicologo_NoEncontrado_ReturnsNotFound()
    {
        ConfigurarAdminAutenticado();
        _admin.Setup(a => a.RechazarPsicologoAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<string>.Fail("Psicólogo no encontrado."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.RechazarPsicologo(5, null, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
        ErrorDe(result).Should().Be("Psicólogo no encontrado.");
    }
}
