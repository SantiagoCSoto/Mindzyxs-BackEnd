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

public class AdminControllerPilotTests
{
    private readonly Mock<AdminService> _admin;

    public AdminControllerPilotTests()
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

    private static void SetHttpContext(AdminController sut, string? authHeader = null)
    {
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        if (authHeader != null) httpContext.Request.Headers.Authorization = authHeader;
        sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    [Fact]
    public async Task PendientesValidacion_SinToken_ReturnsForbidden()
    {
        _admin.Setup(a => a.GetAdminIdAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);

        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.PendientesValidacion(CancellationToken.None);

        result.Should().BeOfType<ObjectResult>();
        ((ObjectResult)result).StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task PendientesValidacion_ConAdmin_ReturnsOk()
    {
        // AdminController.ReadBearerToken extrae "token" de "Bearer token"; el servicio debe reconocerlo como administrador.
        _admin.Setup(a => a.GetAdminIdAsync("token", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _admin.Setup(a => a.PendientesValidacionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<List<PsicologoPendienteDto>>.Ok(new List<PsicologoPendienteDto>(), "OK"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.PendientesValidacion(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }
}
