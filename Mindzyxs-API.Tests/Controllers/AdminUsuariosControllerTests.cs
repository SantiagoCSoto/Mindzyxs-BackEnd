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

public class AdminUsuariosControllerTests
{
    private readonly Mock<AdminService> _admin;

    public AdminUsuariosControllerTests()
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

    private AdminUsuariosController CreateSut() => new(_admin.Object);

    private static void SetHttpContext(AdminUsuariosController sut, string? authHeader = null, string? ip = null)
    {
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        if (authHeader != null) httpContext.Request.Headers.Authorization = authHeader;
        if (ip != null) httpContext.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    private static string? ErrorDe(IActionResult result)
    {
        var value = (result as ObjectResult)?.Value;
        return value?.GetType().GetProperty("error")?.GetValue(value)?.ToString();
    }

    [Fact]
    public async Task Listar_SinToken_ReturnsForbidden()
    {
        _admin.Setup(a => a.GetAdminIdAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);

        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.Listar(null, null, null, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>();
        ((ObjectResult)result).StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Listar_ConAdmin_ReturnsOk()
    {
        _admin.Setup(a => a.GetAdminIdAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _admin.Setup(a => a.UsuariosAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<List<UsuarioAdminDto>>.Ok(new List<UsuarioAdminDto>(), "OK"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Listar(null, null, null, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Detalle_SinToken_ReturnsForbidden()
    {
        _admin.Setup(a => a.GetAdminIdAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);

        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.Detalle(1, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>();
        ((ObjectResult)result).StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Detalle_ConAdmin_ReturnsOk()
    {
        _admin.Setup(a => a.GetAdminIdAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _admin.Setup(a => a.DetalleUsuarioAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<UsuarioDetalleDto>.Ok(new UsuarioDetalleDto { Id = 1, Nombre = "Test" }, "OK"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Detalle(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Detalle_NoEncontrado_ReturnsNotFound()
    {
        _admin.Setup(a => a.GetAdminIdAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _admin.Setup(a => a.DetalleUsuarioAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<UsuarioDetalleDto>.Fail("Usuario no encontrado."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Detalle(1, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
        ErrorDe(result).Should().Be("Usuario no encontrado.");
    }

    [Fact]
    public async Task CambiarEstado_SinToken_ReturnsForbidden()
    {
        _admin.Setup(a => a.GetAdminIdAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);

        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.CambiarEstado(1, new CambiarEstadoVm { NuevoEstado = "Activa" }, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>();
        ((ObjectResult)result).StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task CambiarEstado_ConAdmin_ReturnsOk()
    {
        _admin.Setup(a => a.GetAdminIdAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _admin.Setup(a => a.CambiarEstadoAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationResult<string> { Success = true, Message = "Estado actualizado correctamente." });

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.CambiarEstado(1, new CambiarEstadoVm { NuevoEstado = "Activa" }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CambiarEstado_Fails_ReturnsBadRequest()
    {
        _admin.Setup(a => a.GetAdminIdAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _admin.Setup(a => a.CambiarEstadoAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<string>.Fail("Debes seleccionar un estado."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.CambiarEstado(1, new CambiarEstadoVm { NuevoEstado = "Activa" }, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ForzarReset_SinToken_ReturnsForbidden()
    {
        _admin.Setup(a => a.GetAdminIdAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);

        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.ForzarReset(1, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>();
        ((ObjectResult)result).StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task ForzarReset_ConAdmin_ReturnsOk()
    {
        _admin.Setup(a => a.GetAdminIdAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _admin.Setup(a => a.ForzarResetAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationResult<string> { Success = true, Message = "Se envió un código." });

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.ForzarReset(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ForzarReset_Fails_ReturnsBadRequest()
    {
        _admin.Setup(a => a.GetAdminIdAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _admin.Setup(a => a.ForzarResetAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<string>.Fail("No fue posible enviar el código. Intente nuevamente."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.ForzarReset(1, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Listar_ServicioDevuelveFallo_ReturnsBadRequest()
    {
        _admin.Setup(a => a.GetAdminIdAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _admin.Setup(a => a.UsuariosAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<List<UsuarioAdminDto>>.Fail("No puedes administrar usuarios administradores."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Listar("ana", "Administrador", null, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        ErrorDe(result).Should().Be("No puedes administrar usuarios administradores.");
    }

    [Fact]
    public async Task Listar_ExcepcionDeFiltro_ReturnsBadRequestFiltroNoValido()
    {
        _admin.Setup(a => a.GetAdminIdAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _admin.Setup(a => a.UsuariosAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Estado no registrado en catalogo."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Listar(null, null, "EstadoInexistente", CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        ErrorDe(result).Should().Be("Filtro no válido.");
    }

    [Fact]
    public async Task Listar_TokenSinEsquemaBearer_PasaTokenCompletoAlServicio()
    {
        _admin.Setup(a => a.GetAdminIdAsync("token-crudo", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _admin.Setup(a => a.UsuariosAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<List<UsuarioAdminDto>>.Ok(new List<UsuarioAdminDto>(), "OK"));

        var sut = CreateSut();
        SetHttpContext(sut, "token-crudo");

        var result = await sut.Listar(null, null, null, CancellationToken.None);

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

        var result = await sut.Listar(null, null, null, CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(403);
        ErrorDe(result).Should().Be("No tienes permisos para acceder a esta función");
        _admin.Verify(a => a.UsuariosAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Detalle_UsuarioAdministrador_ReturnsForbidden()
    {
        _admin.Setup(a => a.GetAdminIdAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _admin.Setup(a => a.DetalleUsuarioAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<UsuarioDetalleDto>.Fail("No puedes administrar usuarios administradores."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Detalle(2, CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(403);
        ErrorDe(result).Should().Be("No puedes administrar usuarios administradores.");
    }

    [Fact]
    public async Task CambiarEstado_UsuarioNoEncontrado_ReturnsNotFound()
    {
        _admin.Setup(a => a.GetAdminIdAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _admin.Setup(a => a.CambiarEstadoAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<string>.Fail("Usuario no encontrado."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.CambiarEstado(99, new CambiarEstadoVm { NuevoEstado = "Activa" }, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
        ErrorDe(result).Should().Be("Usuario no encontrado.");
    }

    [Fact]
    public async Task CambiarEstado_UsuarioAdministrador_ReturnsForbidden()
    {
        _admin.Setup(a => a.GetAdminIdAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _admin.Setup(a => a.CambiarEstadoAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<string>.Fail("No puedes administrar usuarios administradores."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.CambiarEstado(2, new CambiarEstadoVm { NuevoEstado = "Suspendida" }, CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(403);
        ErrorDe(result).Should().Be("No puedes administrar usuarios administradores.");
    }

    [Fact]
    public async Task CambiarEstado_ConIpCliente_PasaIpAlServicio()
    {
        _admin.Setup(a => a.GetAdminIdAsync(It.IsAny<string?>(), "10.1.2.3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _admin.Setup(a => a.CambiarEstadoAsync(1, 5, "Activa", "10.1.2.3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationResult<string> { Success = true, Message = "Estado actualizado correctamente." });

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token", "10.1.2.3");

        var result = await sut.CambiarEstado(5, new CambiarEstadoVm { NuevoEstado = "Activa" }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _admin.Verify(a => a.CambiarEstadoAsync(1, 5, "Activa", "10.1.2.3", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ForzarReset_UsuarioNoEncontrado_ReturnsNotFound()
    {
        _admin.Setup(a => a.GetAdminIdAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _admin.Setup(a => a.ForzarResetAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<string>.Fail("Usuario no encontrado."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.ForzarReset(99, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
        ErrorDe(result).Should().Be("Usuario no encontrado.");
    }

    [Fact]
    public async Task ForzarReset_UsuarioAdministrador_ReturnsForbidden()
    {
        _admin.Setup(a => a.GetAdminIdAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _admin.Setup(a => a.ForzarResetAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<string>.Fail("No puedes administrar usuarios administradores."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.ForzarReset(2, CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(403);
        ErrorDe(result).Should().Be("No puedes administrar usuarios administradores.");
    }

    [Fact]
    public async Task ForzarReset_ConIpCliente_PasaIpAlServicio()
    {
        _admin.Setup(a => a.GetAdminIdAsync(It.IsAny<string?>(), "10.1.2.3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _admin.Setup(a => a.ForzarResetAsync(1, 5, "10.1.2.3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationResult<string> { Success = true, Message = "Se envió un código." });

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token", "10.1.2.3");

        var result = await sut.ForzarReset(5, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _admin.Verify(a => a.ForzarResetAsync(1, 5, "10.1.2.3", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Listar_AuthorizationVacio_ReturnsForbiddenSesionExpirada()
    {
        var sut = CreateSut();
        SetHttpContext(sut, string.Empty);

        var result = await sut.Listar(null, null, null, CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(403);
        ErrorDe(result).Should().Be("Tu sesión ha expirado. Inicia sesión nuevamente");
        _admin.Verify(a => a.UsuariosAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Listar_AuthorizationSoloEspacios_ReturnsForbiddenSesionExpirada()
    {
        var sut = CreateSut();
        SetHttpContext(sut, "   ");

        var result = await sut.Listar(null, null, null, CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(403);
        ErrorDe(result).Should().Be("Tu sesión ha expirado. Inicia sesión nuevamente");
        _admin.Verify(a => a.UsuariosAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
