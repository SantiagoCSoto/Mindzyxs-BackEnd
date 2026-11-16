using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Mindzyxs_API.Controllers;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Services;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Controllers;

public class ChatControllerUnitTests
{
    private readonly Mock<ChatService> _chats;
    private readonly Mock<FirebaseService> _firebase;
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IUserRepository> _usuarios = new();

    public ChatControllerUnitTests()
    {
        var opts = Microsoft.Extensions.Options.Options.Create(new FirebaseOptions { ProjectId = "test", DatabaseUrl = "https://test.firebaseio.com" });
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<FirebaseService>>();
        var httpFactory = new Mock<IHttpClientFactory>();
        _firebase = new Mock<FirebaseService>(opts, logger.Object, httpFactory.Object);

        var chatDeps = new object[]
        {
            new Mock<IChatRepository>().Object, new Mock<IUserRepository>().Object,
            new Mock<ICatalogRepository>().Object, new Mock<ISesionRepository>().Object,
            new Mock<IAuthLogRepository>().Object, new Mock<IHistorialRepository>().Object,
            _firebase.Object
        };
        _chats = new Mock<ChatService>(chatDeps);
    }

    private ChatController CreateSut() => new(_firebase.Object, _chats.Object, _sesiones.Object, _usuarios.Object);

    private static void SetHttpContext(ChatController sut, string? authHeader = null)
    {
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        if (authHeader != null) httpContext.Request.Headers.Authorization = authHeader;
        sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    [Fact]
    public async Task MisConversaciones_SinSesion_ReturnsUnauthorized()
    {
        _chats.Setup(c => c.MisConversacionesAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Sesión requerida"));

        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.MisConversaciones(CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task MisConversaciones_ConSesion_ReturnsOk()
    {
        _chats.Setup(c => c.MisConversacionesAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConversacionDto>());

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.MisConversaciones(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Solicitudes_SinSesion_ReturnsUnauthorized()
    {
        _chats.Setup(c => c.CrearSolicitudAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Sesión requerida"));

        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.Solicitar(new SolicitudChatVm { IdPaciente = 1 }, CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Solicitudes_ConSesion_ReturnsOk()
    {
        _chats.Setup(c => c.CrearSolicitudAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConversacionDto { Id = 1, Estado = "Pendiente" });

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Solicitar(new SolicitudChatVm { IdPaciente = 1 }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SolicitarPaciente_SinSesion_ReturnsUnauthorized()
    {
        _chats.Setup(c => c.SolicitarPacienteAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Sesión requerida"));

        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.SolicitarPaciente(new SolicitudPacienteVm { IdPsicologo = 1 }, CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task AvisarMensaje_SinSesion_ReturnsUnauthorized()
    {
        _chats.Setup(c => c.AvisarMensajeAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Sesión requerida"));

        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.AvisarMensaje(1, CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task AvisarMensaje_ConSesion_ReturnsOk()
    {
        _chats.Setup(c => c.AvisarMensajeAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.AvisarMensaje(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task BuscarPacientes_SinSesion_ReturnsUnauthorized()
    {
        _chats.Setup(c => c.BuscarPacientesAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Sesión requerida"));

        var sut = CreateSut();
        SetHttpContext(sut);

        var result = await sut.BuscarPacientes(null, null, null, null, CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task BuscarPacientes_ConSesion_ReturnsOk()
    {
        _chats.Setup(c => c.BuscarPacientesAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PacienteChatDto>());

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.BuscarPacientes(null, null, null, null, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task BuscarPacientes_Invalido_ReturnsBadRequest()
    {
        _chats.Setup(c => c.BuscarPacientesAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Seleccione al menos un filtro"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.BuscarPacientes(null, null, null, null, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
