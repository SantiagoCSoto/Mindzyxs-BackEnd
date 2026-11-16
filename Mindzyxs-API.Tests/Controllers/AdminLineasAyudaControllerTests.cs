using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Mindzyxs_API.Controllers;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Services;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Controllers;

public class AdminLineasAyudaControllerTests
{
    private readonly Mock<AdminLineasAyudaService> _lineas;
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IUserRepository> _usuarios = new();

    public AdminLineasAyudaControllerTests()
    {
        var lineasDep = new object[]
        {
            new Mock<ILineaAyudaRepository>().Object,
            new Mock<IUserRepository>().Object,
            new Mock<ISesionRepository>().Object,
            new Mock<IAuthLogRepository>().Object
        };
        _lineas = new Mock<AdminLineasAyudaService>(lineasDep);
    }

    private AdminLineasAyudaController CreateSut() => new(_lineas.Object);

    private static void SetHttpContext(AdminLineasAyudaController sut, string? authHeader = null, string? ip = null)
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
    public async Task Listar_SinSesion_ReturnsUnauthorized()
    {
        _lineas.Setup(l => l.ListarAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Sesión requerida"));

        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.Listar(null, CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Listar_ConSesion_ReturnsOk()
    {
        _lineas.Setup(l => l.ListarAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LineaAyudaAdminListaDto>());

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Listar(null, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Listar_SinPermisos_ReturnsForbidden()
    {
        _lineas.Setup(l => l.ListarAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("No tienes permisos"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Listar(null, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>();
        ((ObjectResult)result).StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Detalle_SinSesion_ReturnsUnauthorized()
    {
        _lineas.Setup(l => l.ObtenerDetalleAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Sesión requerida"));

        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.Detalle(1, CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Detalle_NoEncontrada_ReturnsNotFound()
    {
        _lineas.Setup(l => l.ObtenerDetalleAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Línea de ayuda no encontrada."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Detalle(1, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Detalle_ConSesion_ReturnsOk()
    {
        _lineas.Setup(l => l.ObtenerDetalleAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LineaAyudaAdminDetalleDto { Id = 1, Nombre = "L1" });

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Detalle(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Detalle_OtroError_ReturnsBadRequest()
    {
        _lineas.Setup(l => l.ObtenerDetalleAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("otro error"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Detalle(1, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Crear_SinSesion_ReturnsUnauthorized()
    {
        _lineas.Setup(l => l.CrearAsync(It.IsAny<string?>(), It.IsAny<LineaAyudaAdminCrearDto>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Sesión requerida"));

        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.Crear(new LineaAyudaAdminCrearDto { Nombre = "T", Telefono = "123" }, CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Crear_Validacion_ReturnsBadRequest()
    {
        _lineas.Setup(l => l.CrearAsync(It.IsAny<string?>(), It.IsAny<LineaAyudaAdminCrearDto>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Debes ingresar"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Crear(new LineaAyudaAdminCrearDto { Nombre = "T", Telefono = "123" }, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Crear_ConSesion_ReturnsOk()
    {
        _lineas.Setup(l => l.CrearAsync(It.IsAny<string?>(), It.IsAny<LineaAyudaAdminCrearDto>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LineaAyudaAdminResultadoDto { Id = 1, Mensaje = "creada" });

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Crear(new LineaAyudaAdminCrearDto { Nombre = "T", Telefono = "123" }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Actualizar_NoEncontrada_ReturnsNotFound()
    {
        _lineas.Setup(l => l.ActualizarAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<LineaAyudaAdminCrearDto>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Línea de ayuda no encontrada."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Actualizar(1, new LineaAyudaAdminCrearDto { Nombre = "T", Telefono = "123" }, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Actualizar_Validacion_ReturnsBadRequest()
    {
        _lineas.Setup(l => l.ActualizarAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<LineaAyudaAdminCrearDto>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Debes ingresar"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Actualizar(1, new LineaAyudaAdminCrearDto { Nombre = "T", Telefono = "123" }, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Actualizar_ConSesion_ReturnsOk()
    {
        _lineas.Setup(l => l.ActualizarAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<LineaAyudaAdminCrearDto>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LineaAyudaAdminResultadoDto { Id = 1, Mensaje = "actualizada" });

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Actualizar(1, new LineaAyudaAdminCrearDto { Nombre = "T", Telefono = "123" }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Activar_NoEncontrada_ReturnsNotFound()
    {
        _lineas.Setup(l => l.ActivarAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Línea de ayuda no encontrada."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Activar(1, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Activar_Validacion_ReturnsBadRequest()
    {
        _lineas.Setup(l => l.ActivarAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("otro error"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Activar(1, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Activar_ConSesion_ReturnsOk()
    {
        _lineas.Setup(l => l.ActivarAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("activada");

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Activar(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Inactivar_NoEncontrada_ReturnsNotFound()
    {
        _lineas.Setup(l => l.InactivarAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Línea de ayuda no encontrada."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Inactivar(1, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Inactivar_Validacion_ReturnsBadRequest()
    {
        _lineas.Setup(l => l.InactivarAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("otro error"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Inactivar(1, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Inactivar_ConSesion_ReturnsOk()
    {
        _lineas.Setup(l => l.InactivarAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("inactivada");

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Inactivar(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Eliminar_NoEncontrada_ReturnsNotFound()
    {
        _lineas.Setup(l => l.EliminarAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Línea de ayuda no encontrada."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Eliminar(1, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Eliminar_Validacion_ReturnsBadRequest()
    {
        _lineas.Setup(l => l.EliminarAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("otro error"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Eliminar(1, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Eliminar_ConSesion_ReturnsOk()
    {
        _lineas.Setup(l => l.EliminarAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("eliminada");

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Eliminar(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Listar_TokenSinBearer_PasaTokenCompletoAlServicio()
    {
        _lineas.Setup(l => l.ListarAsync("token-crudo", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LineaAyudaAdminListaDto>());

        var sut = CreateSut();
        SetHttpContext(sut, "token-crudo");

        var result = await sut.Listar(null, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _lineas.Verify(l => l.ListarAsync("token-crudo", It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Crear_ConIpCliente_PasaIpAlServicio()
    {
        _lineas.Setup(l => l.CrearAsync("token", It.IsAny<LineaAyudaAdminCrearDto>(), "10.2.3.4", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LineaAyudaAdminResultadoDto { Id = 1, Mensaje = "creada" });

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token", "10.2.3.4");

        var result = await sut.Crear(new LineaAyudaAdminCrearDto { Nombre = "T", Telefono = "123" }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _lineas.Verify(l => l.CrearAsync("token", It.IsAny<LineaAyudaAdminCrearDto>(), "10.2.3.4", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Actualizar_ConIpCliente_PasaIpAlServicio()
    {
        _lineas.Setup(l => l.ActualizarAsync("token", 1, It.IsAny<LineaAyudaAdminCrearDto>(), "10.2.3.4", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LineaAyudaAdminResultadoDto { Id = 1, Mensaje = "actualizada" });

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token", "10.2.3.4");

        var result = await sut.Actualizar(1, new LineaAyudaAdminCrearDto { Nombre = "T", Telefono = "123" }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _lineas.Verify(l => l.ActualizarAsync("token", 1, It.IsAny<LineaAyudaAdminCrearDto>(), "10.2.3.4", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Activar_ConIpCliente_PasaIpAlServicio()
    {
        _lineas.Setup(l => l.ActivarAsync("token", 1, "10.2.3.4", It.IsAny<CancellationToken>()))
            .ReturnsAsync("activada");

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token", "10.2.3.4");

        var result = await sut.Activar(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _lineas.Verify(l => l.ActivarAsync("token", 1, "10.2.3.4", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Inactivar_ConIpCliente_PasaIpAlServicio()
    {
        _lineas.Setup(l => l.InactivarAsync("token", 1, "10.2.3.4", It.IsAny<CancellationToken>()))
            .ReturnsAsync("inactivada");

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token", "10.2.3.4");

        var result = await sut.Inactivar(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _lineas.Verify(l => l.InactivarAsync("token", 1, "10.2.3.4", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Eliminar_ConIpCliente_PasaIpAlServicio()
    {
        _lineas.Setup(l => l.EliminarAsync("token", 1, "10.2.3.4", It.IsAny<CancellationToken>()))
            .ReturnsAsync("eliminada");

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token", "10.2.3.4");

        var result = await sut.Eliminar(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _lineas.Verify(l => l.EliminarAsync("token", 1, "10.2.3.4", It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---- Authorization vacio o solo espacios: el servicio debe recibir token en blanco y rechazarlo ----

    [Fact]
    public async Task Listar_AuthorizationVacio_ReturnsUnauthorized()
    {
        _lineas.Setup(l => l.ListarAsync(It.Is<string?>(t => string.IsNullOrWhiteSpace(t)), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Tu sesión ha expirado. Inicia sesión nuevamente"));

        var sut = CreateSut();
        SetHttpContext(sut, string.Empty);

        var result = await sut.Listar(null, CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>();
        ErrorDe(result).Should().Be("Tu sesión ha expirado. Inicia sesión nuevamente");
    }

    [Fact]
    public async Task Listar_AuthorizationSoloEspacios_ReturnsUnauthorized()
    {
        _lineas.Setup(l => l.ListarAsync(It.Is<string?>(t => string.IsNullOrWhiteSpace(t)), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Tu sesión ha expirado. Inicia sesión nuevamente"));

        var sut = CreateSut();
        SetHttpContext(sut, "   ");

        var result = await sut.Listar(null, CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>();
        ErrorDe(result).Should().Be("Tu sesión ha expirado. Inicia sesión nuevamente");
    }
}
