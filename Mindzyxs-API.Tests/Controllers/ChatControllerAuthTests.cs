using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Mindzyxs_API.Controllers;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Services;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Controllers;

public class ChatControllerAuthTests
{
    private readonly Mock<ChatService> _chats;
    private readonly Mock<FirebaseService> _firebase;
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IUserRepository> _usuarios = new();

    public ChatControllerAuthTests()
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

    private static ConversacionDto MakeConv(int id = 1, string estado = "Pendiente") => new()
    {
        Id = id, Estado = estado, FechaCreacion = DateTime.UtcNow,
        Contraparte = new ChatContraparteDto { Id = 2, Nombre = "X", Rol = "Paciente" }
    };

    [Fact]
    public async Task Solicitar_ConSesion_ReturnsOk()
    {
        _chats.Setup(c => c.CrearSolicitudAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeConv());

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Solicitar(new SolicitudChatVm { IdPaciente = 1 }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Solicitar_Invalido_ReturnsBadRequest()
    {
        _chats.Setup(c => c.CrearSolicitudAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Ya existe una solicitud"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Solicitar(new SolicitudChatVm { IdPaciente = 1 }, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SolicitarPaciente_ConSesion_ReturnsOk()
    {
        _chats.Setup(c => c.SolicitarPacienteAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeConv());

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.SolicitarPaciente(new SolicitudPacienteVm { IdPsicologo = 1 }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SolicitarPaciente_Invalido_ReturnsBadRequest()
    {
        _chats.Setup(c => c.SolicitarPacienteAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Psicólogo no encontrado."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.SolicitarPaciente(new SolicitudPacienteVm { IdPsicologo = 1 }, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Aceptar_ConSesion_ReturnsOk()
    {
        _chats.Setup(c => c.ResponderAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeConv(estado: "Aceptada"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Aceptar(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Aceptar_NotFound_ReturnsNotFound()
    {
        _chats.Setup(c => c.ResponderAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Conversación no encontrada."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Aceptar(1, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Aceptar_Invalido_ReturnsBadRequest()
    {
        _chats.Setup(c => c.ResponderAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("La solicitud ya fue procesada."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Aceptar(1, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Rechazar_ConSesion_ReturnsOk()
    {
        _chats.Setup(c => c.ResponderAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeConv(estado: "Rechazada"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Rechazar(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AceptarPsicologo_ConSesion_ReturnsOk()
    {
        _chats.Setup(c => c.ResponderPsicologoAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeConv(estado: "Aceptada"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.AceptarPsicologo(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AceptarPsicologo_NotFound_ReturnsNotFound()
    {
        _chats.Setup(c => c.ResponderPsicologoAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Conversación no encontrada."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.AceptarPsicologo(1, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RechazarPsicologo_ConSesion_ReturnsOk()
    {
        _chats.Setup(c => c.ResponderPsicologoAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeConv(estado: "Rechazada"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.RechazarPsicologo(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SolicitarBloqueo_ConSesion_ReturnsOk()
    {
        _chats.Setup(c => c.SolicitarBloqueoAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeConv(estado: "BloqueoPendiente"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.SolicitarBloqueo(1, new SolicitudBloqueoVm { Motivo = "x" }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SolicitarBloqueo_NotFound_ReturnsNotFound()
    {
        _chats.Setup(c => c.SolicitarBloqueoAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Conversación no encontrada."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.SolicitarBloqueo(1, new SolicitudBloqueoVm { Motivo = "x" }, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SolicitarBloqueo_Invalido_ReturnsBadRequest()
    {
        _chats.Setup(c => c.SolicitarBloqueoAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Solo se puede solicitar bloqueo en un chat aceptado."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.SolicitarBloqueo(1, new SolicitudBloqueoVm { Motivo = "x" }, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ResolverBloqueo_ConSesion_ReturnsOk()
    {
        _chats.Setup(c => c.ResolverBloqueoAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeConv(estado: "Bloqueada"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.ResolverBloqueo(1, new ResolverBloqueoVm { Aceptar = true }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ResolverBloqueo_NotFound_ReturnsNotFound()
    {
        _chats.Setup(c => c.ResolverBloqueoAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Conversación no encontrada."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.ResolverBloqueo(1, new ResolverBloqueoVm { Aceptar = true }, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Desbloquear_ConSesion_ReturnsOk()
    {
        _chats.Setup(c => c.DesbloquearAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeConv(estado: "Aceptada"));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Desbloquear(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Desbloquear_NotFound_ReturnsNotFound()
    {
        _chats.Setup(c => c.DesbloquearAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Conversación no encontrada."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Desbloquear(1, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Desbloquear_Invalido_ReturnsBadRequest()
    {
        _chats.Setup(c => c.DesbloquearAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("No puedes desbloquear esta conversación."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.Desbloquear(1, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task AvisarMensaje_NotFound_ReturnsNotFound()
    {
        _chats.Setup(c => c.AvisarMensajeAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Conversación no encontrada."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.AvisarMensaje(1, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AvisarMensaje_Bloqueado_ReturnsForbidden()
    {
        _chats.Setup(c => c.AvisarMensajeAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Has sido bloqueado por este usuario."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.AvisarMensaje(1, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>();
        ((ObjectResult)result).StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task AvisarMensaje_Invalido_ReturnsBadRequest()
    {
        _chats.Setup(c => c.AvisarMensajeAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("El chat no está aceptado."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.AvisarMensaje(1, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task BuscarPacientes_Invalido_ReturnsBadRequest()
    {
        _chats.Setup(c => c.BuscarPacientesAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Seleccione al menos un filtro antes de buscar."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.BuscarPacientes(null, null, null, null, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task BuscarPacientes_Forbidden_ReturnsForbidden()
    {
        _chats.Setup(c => c.BuscarPacientesAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Se requiere rol Psicólogo."));

        var sut = CreateSut();
        SetHttpContext(sut, "Bearer token");

        var result = await sut.BuscarPacientes(null, null, null, null, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>();
        ((ObjectResult)result).StatusCode.Should().Be(403);
    }
}
