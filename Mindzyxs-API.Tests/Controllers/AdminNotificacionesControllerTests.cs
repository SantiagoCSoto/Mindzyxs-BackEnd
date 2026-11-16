using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Mindzyxs_API.Controllers;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Services;
using Mindzyxs_API.Utilities;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Controllers;

public class AdminNotificacionesControllerTests
{
    private readonly Mock<AdminService> _admin;

    public AdminNotificacionesControllerTests()
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

    private AdminNotificacionesController CreateSut() => new(_admin.Object);

    private static void SetHttpContext(AdminNotificacionesController sut, string? authHeader = null, string? ip = null)
    {
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        if (authHeader != null) httpContext.Request.Headers["Authorization"] = authHeader;
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

    // ---- Listar: GET /api/admin/notificaciones ----

    [Fact]
    public async Task Listar_SinToken_ReturnsForbidden()
    {
        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.Listar(CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(403);
        ErrorDe(result).Should().Be("Tu sesión ha expirado. Inicia sesión nuevamente");
    }

    [Fact]
    public async Task Listar_ConAdmin_ReturnsOk()
    {
        ConfigurarAdminAutenticado();
        _admin.Setup(a => a.NotificacionesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<List<NotificacionDto>>.Ok(new List<NotificacionDto>(), "OK"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Listar(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Listar_TokenSinEsquemaBearer_PasaTokenCompletoAlServicio()
    {
        _admin.Setup(a => a.GetAdminIdAsync("token-crudo", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _admin.Setup(a => a.NotificacionesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<List<NotificacionDto>>.Ok(new List<NotificacionDto>(), "OK"));

        var sut = CreateSut();
        SetHttpContext(sut, "token-crudo");

        var result = await sut.Listar(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _admin.Verify(a => a.GetAdminIdAsync("token-crudo", It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Listar_TokenInvalido_ReturnsForbiddenSinPermisos()
    {
        _admin.Setup(a => a.GetAdminIdAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token-invalido");

        var result = await sut.Listar(CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(403);
        ErrorDe(result).Should().Be("No tienes permisos para acceder a esta función");
        _admin.Verify(a => a.NotificacionesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Detalle: GET /api/admin/notificaciones/{tipo} ----

    [Fact]
    public async Task Detalle_SinToken_ReturnsForbidden()
    {
        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.Detalle("CODE_SENT", CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Detalle_ConAdmin_ReturnsOk()
    {
        ConfigurarAdminAutenticado();
        _admin.Setup(a => a.DetalleNotificacionAsync("CODE_SENT", It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<NotificacionDto>.Ok(new NotificacionDto
            {
                Tipo = "CODE_SENT",
                Nombre = "Codigo enviado",
                Canales = new List<string> { "correo" },
                Activo = true
            }, "OK"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Detalle("CODE_SENT", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Detalle_EventoInexistente_ReturnsNotFound()
    {
        ConfigurarAdminAutenticado();
        _admin.Setup(a => a.DetalleNotificacionAsync("NO_EXISTE", It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<NotificacionDto>.Fail("Evento no encontrado."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Detalle("NO_EXISTE", CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
        ErrorDe(result).Should().Be("Evento no encontrado.");
    }

    // ---- Actualizar: PUT /api/admin/notificaciones/{tipo} ----

    [Fact]
    public async Task Actualizar_SinToken_ReturnsForbidden()
    {
        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.Actualizar("CODE_SENT", new ActualizarNotificacionVm { Activo = true }, CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Actualizar_ConAdmin_ReturnsOk()
    {
        ConfigurarAdminAutenticado();
        _admin.Setup(a => a.ActualizarNotificacionAsync(1, "CODE_SENT", It.IsAny<ActualizarNotificacionVm>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationResult<string> { Success = true, Message = "Notificación actualizada correctamente." });

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var vm = new ActualizarNotificacionVm { Activo = true, Canales = new List<string> { "correo" }, Asunto = "Tu codigo", Cuerpo = "Hola {nombre}" };
        var result = await sut.Actualizar("CODE_SENT", vm, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Actualizar_ConIpCliente_PasaIpAlServicio()
    {
        _admin.Setup(a => a.GetAdminIdAsync(It.IsAny<string?>(), "10.0.0.9", It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _admin.Setup(a => a.ActualizarNotificacionAsync(1, "CODE_SENT", It.IsAny<ActualizarNotificacionVm>(), "10.0.0.9", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationResult<string> { Success = true, Message = "Notificación actualizada correctamente." });

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token", "10.0.0.9");

        var result = await sut.Actualizar("CODE_SENT", new ActualizarNotificacionVm { Activo = false }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _admin.Verify(a => a.ActualizarNotificacionAsync(1, "CODE_SENT", It.IsAny<ActualizarNotificacionVm>(), "10.0.0.9", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Actualizar_ValidacionFallida_ReturnsBadRequest()
    {
        ConfigurarAdminAutenticado();
        _admin.Setup(a => a.ActualizarNotificacionAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<ActualizarNotificacionVm>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<string>.Fail("Debes habilitar al menos un canal para este evento."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Actualizar("CODE_SENT", new ActualizarNotificacionVm { Activo = true }, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        ErrorDe(result).Should().Be("Debes habilitar al menos un canal para este evento.");
    }

    [Fact]
    public async Task Actualizar_EventoInexistente_ReturnsNotFound()
    {
        ConfigurarAdminAutenticado();
        _admin.Setup(a => a.ActualizarNotificacionAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<ActualizarNotificacionVm>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<string>.Fail("Evento no encontrado."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Actualizar("NO_EXISTE", new ActualizarNotificacionVm { Activo = false }, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
        ErrorDe(result).Should().Be("Evento no encontrado.");
    }

    // ---- Preview: POST /api/admin/notificaciones/{tipo}/preview ----

    [Fact]
    public async Task Preview_SinToken_ReturnsForbidden()
    {
        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.Preview("CODE_SENT", new PreviewNotificacionVm(), CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Preview_ConAdmin_ReturnsOk()
    {
        ConfigurarAdminAutenticado();
        var vm = new PreviewNotificacionVm { Nombre = "Ana", Codigo = "123456", Fecha = "01/01/2026", Motivo = "Prueba" };
        _admin.Setup(a => a.PreviewNotificacionAsync("CODE_SENT", vm, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<NotificacionDto>.Ok(new NotificacionDto { Tipo = "CODE_SENT", Asunto = "Tu codigo", Cuerpo = "Hola Ana" }, "OK"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Preview("CODE_SENT", vm, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _admin.Verify(a => a.PreviewNotificacionAsync("CODE_SENT", vm, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Preview_SinCuerpo_UsaVmPorDefecto()
    {
        ConfigurarAdminAutenticado();
        _admin.Setup(a => a.PreviewNotificacionAsync("CODE_SENT", It.IsNotNull<PreviewNotificacionVm>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<NotificacionDto>.Ok(new NotificacionDto { Tipo = "CODE_SENT" }, "OK"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Preview("CODE_SENT", null, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _admin.Verify(a => a.PreviewNotificacionAsync("CODE_SENT", It.IsNotNull<PreviewNotificacionVm>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Preview_EventoInexistente_ReturnsNotFound()
    {
        ConfigurarAdminAutenticado();
        _admin.Setup(a => a.PreviewNotificacionAsync("NO_EXISTE", It.IsAny<PreviewNotificacionVm>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<NotificacionDto>.Fail("Evento no encontrado."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Preview("NO_EXISTE", new PreviewNotificacionVm(), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
        ErrorDe(result).Should().Be("Evento no encontrado.");
    }

    // ---- Authorization vacio o solo espacios (equivale a no enviar token) ----

    [Fact]
    public async Task Listar_AuthorizationVacio_ReturnsForbiddenSesionExpirada()
    {
        var sut = CreateSut();
        SetHttpContext(sut, string.Empty);

        var result = await sut.Listar(CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(403);
        ErrorDe(result).Should().Be("Tu sesión ha expirado. Inicia sesión nuevamente");
        _admin.Verify(a => a.NotificacionesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Listar_AuthorizationSoloEspacios_ReturnsForbiddenSesionExpirada()
    {
        var sut = CreateSut();
        SetHttpContext(sut, "   ");

        var result = await sut.Listar(CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(403);
        ErrorDe(result).Should().Be("Tu sesión ha expirado. Inicia sesión nuevamente");
        _admin.Verify(a => a.NotificacionesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
