using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mindzyxs_API.Controllers;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Services;
using Mindzyxs_API.Utilities;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Controllers;

public class AccountControllerTests
{
    private readonly Mock<AuthService> _auth;
    private readonly Mock<VerificationService> _verification;
    private readonly Mock<CatalogService> _catalogs;
    private readonly Mock<LoginService> _login;
    private readonly Mock<PasswordRecoveryService> _recovery;
    private readonly Mock<PerfilService> _perfil;

    public AccountControllerTests()
    {
        var notificaciones = new NotificationService(
            new Mock<INotificacionRepository>().Object,
            new Mock<IEmailSender>().Object,
            new Mock<IAuthLogRepository>().Object);

        _auth = new Mock<AuthService>(
            new Mock<IUserRepository>().Object,
            new Mock<ICatalogRepository>().Object,
            new Mock<ICodigoRepository>().Object,
            new Mock<ITarjetaStorage>().Object,
            notificaciones,
            Options.Create(new RegistroOptions { CodigoExpiraMinutos = 15 }),
            new Mock<ILogger<AuthService>>().Object) { CallBase = false };

        _verification = new Mock<VerificationService>(
            new Mock<IUserRepository>().Object,
            new Mock<ICatalogRepository>().Object,
            new Mock<ICodigoRepository>().Object,
            notificaciones,
            Options.Create(new RegistroOptions { CodigoExpiraMinutos = 15 })) { CallBase = false };

        _catalogs = new Mock<CatalogService>(new Mock<ICatalogRepository>().Object) { CallBase = false };

        _login = new Mock<LoginService>(
            new Mock<IUserRepository>().Object,
            new Mock<ICatalogRepository>().Object,
            new Mock<ISesionRepository>().Object,
            new Mock<IAuthLogRepository>().Object,
            Options.Create(new SesionOptions { ExpiraHoras = 8 }),
            new Mock<ILogger<LoginService>>().Object) { CallBase = false };

        _recovery = new Mock<PasswordRecoveryService>(
            new Mock<IUserRepository>().Object,
            new Mock<ICodigoRepository>().Object,
            new Mock<ISesionRepository>().Object,
            new Mock<IAuthLogRepository>().Object,
            notificaciones,
            Options.Create(new RegistroOptions { CodigoExpiraMinutos = 15 }),
            new Mock<ILogger<PasswordRecoveryService>>().Object) { CallBase = false };

        _perfil = new Mock<PerfilService>(
            new Mock<IUserRepository>().Object,
            new Mock<ICatalogRepository>().Object,
            new Mock<ICodigoRepository>().Object,
            new Mock<ISesionRepository>().Object,
            new Mock<IAuthLogRepository>().Object,
            notificaciones,
            Options.Create(new RegistroOptions { CodigoExpiraMinutos = 15 })) { CallBase = false };
    }

    private AccountController CreateSut()
        => new(_auth.Object, _verification.Object, _catalogs.Object, _login.Object, _recovery.Object, _perfil.Object);

    private static void SetHttpContext(AccountController sut, string? authHeader = null, string? ip = null)
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

    // ModelStateDictionary.AddModelError rechaza mensajes nulos; el ModelError sin mensaje se agrega
    // directamente a la colección para ejercitar la rama por defecto "Solicitud inválida.".
    private static void AgregarErrorSinMensaje(ModelStateDictionary state, string key)
    {
        state.AddModelError(key, "temporal");
        state[key]!.Errors.Clear();
        state[key]!.Errors.Add(new ModelError((string)null!));
    }

    // ---- Me: GET /api/account/me ----

    [Fact]
    public async Task Me_AuthorizationSinEsquemaBearer_NoEnviaTokenYResponde401()
    {
        _login.Setup(l => l.ValidateSessionAsync(null, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<UsuarioDto>.Fail("Tu sesión ha expirado. Inicia sesión nuevamente"));

        var sut = CreateSut();
        SetHttpContext(sut, "Basic dXN1YXJpbzpjbGF2ZQ==");

        var result = await sut.Me(CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>();
        ErrorDe(result).Should().Be("Tu sesión ha expirado. Inicia sesión nuevamente");
    }

    [Fact]
    public async Task Me_BearerConComillas_PasaTokenLimpioAlServicio()
    {
        _login.Setup(l => l.ValidateSessionAsync("abc123", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<UsuarioDto>.Fail("Tu sesión ha expirado. Inicia sesión nuevamente"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer \"abc123\"");

        var result = await sut.Me(CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>();
        _login.Verify(l => l.ValidateSessionAsync("abc123", It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Me_SesionValidaConIp_DevuelveDatosDelUsuario()
    {
        var usuario = new UsuarioDto { IdUsuario = 42, Correo = "ana@correo.com", Nombre = "Ana", Apellido = "Perez" };
        _login.Setup(l => l.ValidateSessionAsync("tok", "10.0.0.7", It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<UsuarioDto>.Ok(usuario, "OK"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer tok", "10.0.0.7");

        var result = await sut.Me(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _login.Verify(l => l.ValidateSessionAsync("tok", "10.0.0.7", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Me_CuentaSuspendida_ReturnsForbidden()
    {
        _login.Setup(l => l.ValidateSessionAsync("tok", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<UsuarioDto>.Fail("Tu cuenta se encuentra suspendida. Contacta al administrador"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer tok");

        var result = await sut.Me(CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(403);
        ErrorDe(result).Should().Be("Tu cuenta se encuentra suspendida. Contacta al administrador");
    }

    // ---- Logout: POST /api/account/logout ----

    [Fact]
    public async Task Logout_ConIp_PasaTokenEIpAlServicio()
    {
        _login.Setup(l => l.LogoutAsync("tok", "10.0.0.7", It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult.Ok("Sesión cerrada correctamente"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer tok", "10.0.0.7");

        var result = await sut.Logout(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _login.Verify(l => l.LogoutAsync("tok", "10.0.0.7", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Logout_FalloDelServicio_ReturnsInternalServerError()
    {
        _login.Setup(l => l.LogoutAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult.Fail("No fue posible cerrar sesión"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer tok");

        var result = await sut.Logout(CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(500);
        ErrorDe(result).Should().Be("No fue posible cerrar sesión");
    }

    // ---- Login: POST /api/account/login ----

    [Fact]
    public async Task Login_ModeloInvalido_ReturnsBadRequestConPrimerError()
    {
        var sut = CreateSut();
        SetHttpContext(sut);
        sut.ModelState.AddModelError("Correo", "Ingresa un correo válido");

        var result = await sut.Login(new LoginVm { Correo = "x", Password = "" }, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        ErrorDe(result).Should().Be("Ingresa un correo válido");
        _login.Verify(l => l.LoginAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Login_CredencialesCorrectasConIp_DevuelveTokenYUsuario()
    {
        var dto = new LoginResponseDto
        {
            IdUsuario = 42,
            Correo = "ana@correo.com",
            Rol = "Paciente",
            Panel = "paciente",
            EstadoCuenta = "Activa",
            Token = "tok-nuevo",
            ExpiraEn = DateTime.UtcNow.AddHours(8)
        };
        _login.Setup(l => l.LoginAsync("ana@correo.com", "Secreta1", "10.0.0.7", It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<LoginResponseDto>.Ok(dto, "Inicio de sesión exitoso"));

        var sut = CreateSut();
        SetHttpContext(sut, ip: "10.0.0.7");

        var result = await sut.Login(new LoginVm { Correo = "ana@correo.com", Password = "Secreta1" }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _login.Verify(l => l.LoginAsync("ana@correo.com", "Secreta1", "10.0.0.7", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_CampoObligatorio_ReturnsBadRequest()
    {
        _login.Setup(l => l.LoginAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<LoginResponseDto>.Fail("Este campo es obligatorio"));

        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.Login(new LoginVm { Correo = "", Password = "" }, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        ErrorDe(result).Should().Be("Este campo es obligatorio");
    }

    [Fact]
    public async Task Login_CorreoInvalido_ReturnsBadRequest()
    {
        _login.Setup(l => l.LoginAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<LoginResponseDto>.Fail("Ingresa un correo válido"));

        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.Login(new LoginVm { Correo = "no-es-correo", Password = "x" }, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        ErrorDe(result).Should().Be("Ingresa un correo válido");
    }

    [Fact]
    public async Task Login_CredencialesIncorrectas_ReturnsUnauthorized()
    {
        _login.Setup(l => l.LoginAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<LoginResponseDto>.Fail("Correo o contraseña incorrectos"));

        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.Login(new LoginVm { Correo = "ana@correo.com", Password = "mala" }, CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>();
        ErrorDe(result).Should().Be("Correo o contraseña incorrectos");
    }

    [Fact]
    public async Task Login_CuentaSuspendida_ReturnsForbidden()
    {
        _login.Setup(l => l.LoginAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<LoginResponseDto>.Fail("Tu cuenta se encuentra suspendida. Contacta al administrador"));

        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.Login(new LoginVm { Correo = "ana@correo.com", Password = "Secreta1" }, CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(403);
        ErrorDe(result).Should().Be("Tu cuenta se encuentra suspendida. Contacta al administrador");
    }

    [Fact]
    public async Task Login_CuentaPendienteAprobacion_ReturnsForbidden()
    {
        _login.Setup(l => l.LoginAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<LoginResponseDto>.Fail("Tu cuenta está pendiente de aprobación por el administrador. Intenta más tarde"));

        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.Login(new LoginVm { Correo = "psi@correo.com", Password = "Secreta1" }, CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(403);
        ErrorDe(result).Should().Be("Tu cuenta está pendiente de aprobación por el administrador. Intenta más tarde");
    }

    [Fact]
    public async Task Login_ErrorNoMapeado_ReturnsBadRequest()
    {
        _login.Setup(l => l.LoginAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<LoginResponseDto>.Fail("Error inesperado en el servidor"));

        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.Login(new LoginVm { Correo = "ana@correo.com", Password = "Secreta1" }, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        ErrorDe(result).Should().Be("Error inesperado en el servidor");
    }

    // ---- Register: POST /api/account/register ----

    [Fact]
    public async Task Register_ModeloInvalido_ReturnsBadRequestConPrimerError()
    {
        var sut = CreateSut();
        SetHttpContext(sut);
        sut.ModelState.AddModelError("Correo", "El correo es obligatorio");

        var result = await sut.Register(new RegisterVm(), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        ErrorDe(result).Should().Be("El correo es obligatorio");
        _auth.Verify(a => a.RegisterAsync(It.IsAny<RegisterVm>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Verify: POST /api/account/verify ----

    [Fact]
    public async Task Verify_ModeloInvalido_ReturnsBadRequestObligatorio()
    {
        var sut = CreateSut();
        SetHttpContext(sut);
        sut.ModelState.AddModelError("Codigo", "Este campo es obligatorio");

        var result = await sut.Verify(new VerifyVm(), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        ErrorDe(result).Should().Be("Este campo es obligatorio");
        _verification.Verify(v => v.VerifyAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Verify_CodigoExpirado_ReturnsGone()
    {
        _verification.Setup(v => v.VerifyAsync("ana@correo.com", "123456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<string>.Fail("El código expiró. Reenvía el código"));

        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.Verify(new VerifyVm { Correo = "ana@correo.com", Codigo = "123456" }, CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(410);
        ErrorDe(result).Should().Be("El código expiró. Reenvía el código");
    }

    // ---- Resend: POST /api/account/resend ----

    [Fact]
    public async Task Resend_ModeloInvalido_ReturnsBadRequest()
    {
        var sut = CreateSut();
        SetHttpContext(sut);
        sut.ModelState.AddModelError("Correo", "Este campo es obligatorio");

        var result = await sut.Resend(new ResendVm(), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        ErrorDe(result).Should().Be("Este campo es obligatorio");
        _verification.Verify(v => v.ResendAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Resend_CorreoValido_ReturnsOkConMensaje()
    {
        _verification.Setup(v => v.ResendAsync("ana@correo.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult.Ok("Código reenviado"));

        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.Resend(new ResendVm { Correo = "ana@correo.com" }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _verification.Verify(v => v.ResendAsync("ana@correo.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---- ResendRecovery: POST /api/account/forgot/resend ----

    [Fact]
    public async Task ForgotResend_ModeloInvalido_ReturnsBadRequest()
    {
        var sut = CreateSut();
        SetHttpContext(sut);
        sut.ModelState.AddModelError("Correo", "Este campo es obligatorio");

        var result = await sut.ResendRecovery(new ForgotVm(), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        ErrorDe(result).Should().Be("Este campo es obligatorio");
        _recovery.Verify(r => r.ResendAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ForgotResend_FalloDelServicio_ReturnsBadRequest()
    {
        _recovery.Setup(r => r.ResendAsync("ana@correo.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult.Fail("Espera un momento antes de reenviar el código"));

        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.ResendRecovery(new ForgotVm { Correo = "ana@correo.com" }, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        ErrorDe(result).Should().Be("Espera un momento antes de reenviar el código");
    }

    // ---- Forgot: POST /api/account/forgot ----

    [Fact]
    public async Task Forgot_ModeloInvalido_ReturnsBadRequestConPrimerError()
    {
        var sut = CreateSut();
        SetHttpContext(sut);
        sut.ModelState.AddModelError("Correo", "Ingresa un correo válido");

        var result = await sut.Forgot(new ForgotVm { Correo = "no-es-correo" }, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        ErrorDe(result).Should().Be("Ingresa un correo válido");
        _recovery.Verify(r => r.ForgotAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Forgot_FalloDelServicio_ReturnsBadRequest()
    {
        _recovery.Setup(r => r.ForgotAsync("ana@correo.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult.Fail("No fue posible enviar el código. Intente nuevamente."));

        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.Forgot(new ForgotVm { Correo = "ana@correo.com" }, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        ErrorDe(result).Should().Be("No fue posible enviar el código. Intente nuevamente.");
    }

    // ---- VerifyCode: POST /api/account/forgot/verify ----

    [Fact]
    public async Task ForgotVerify_ModeloInvalido_ReturnsBadRequest()
    {
        var sut = CreateSut();
        SetHttpContext(sut);
        sut.ModelState.AddModelError("Codigo", "Este campo es obligatorio");

        var result = await sut.VerifyCode(new VerifyCodeVm(), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        ErrorDe(result).Should().Be("Este campo es obligatorio");
        _recovery.Verify(r => r.VerifyCodeAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ForgotVerify_CodigoCorrecto_ReturnsOk()
    {
        _recovery.Setup(r => r.VerifyCodeAsync("ana@correo.com", "123456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult.Ok("Código verificado. Define tu nueva contraseña"));

        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.VerifyCode(new VerifyCodeVm { Correo = "ana@correo.com", Codigo = "123456" }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ForgotVerify_CodigoExpirado_ReturnsGone()
    {
        _recovery.Setup(r => r.VerifyCodeAsync("ana@correo.com", "123456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult.Fail("El código expiró. Reenvía el código"));

        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.VerifyCode(new VerifyCodeVm { Correo = "ana@correo.com", Codigo = "123456" }, CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(410);
        ErrorDe(result).Should().Be("El código expiró. Reenvía el código");
    }

    // ---- ResetPassword: POST /api/account/forgot/reset ----

    [Fact]
    public async Task ForgotReset_ModeloInvalido_ReturnsBadRequest()
    {
        var sut = CreateSut();
        SetHttpContext(sut);
        sut.ModelState.AddModelError("Password", "Este campo es obligatorio");

        var result = await sut.ResetPassword(new ResetPasswordVm(), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        ErrorDe(result).Should().Be("Este campo es obligatorio");
        _recovery.Verify(r => r.ResetPasswordAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ForgotReset_ConIp_PasaIpYRespondeOk()
    {
        _recovery.Setup(r => r.ResetPasswordAsync("ana@correo.com", "123456", "Nueva123", "10.0.0.7", It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult.Ok("Contraseña actualizada exitosamente"));

        var sut = CreateSut();
        SetHttpContext(sut, ip: "10.0.0.7");

        var result = await sut.ResetPassword(new ResetPasswordVm { Correo = "ana@correo.com", Codigo = "123456", Password = "Nueva123" }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _recovery.Verify(r => r.ResetPasswordAsync("ana@correo.com", "123456", "Nueva123", "10.0.0.7", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ForgotReset_CodigoExpirado_ReturnsGone()
    {
        _recovery.Setup(r => r.ResetPasswordAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult.Fail("El código expiró. Reenvía el código"));

        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.ResetPassword(new ResetPasswordVm { Correo = "ana@correo.com", Codigo = "123456", Password = "Nueva123" }, CancellationToken.None);

        ((ObjectResult)result).StatusCode.Should().Be(410);
        ErrorDe(result).Should().Be("El código expiró. Reenvía el código");
    }

    // ---- ReanudarVerificacion: POST /api/account/verificacion/reanudar ----

    [Fact]
    public async Task Reanudar_CorreoEnBlanco_ReturnsBadRequest()
    {
        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.ReanudarVerificacion(new ReanudarVerificacionVm { Correo = "   " }, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        ErrorDe(result).Should().Be("Ingresa un correo válido");
        _perfil.Verify(p => p.ReanudarAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
