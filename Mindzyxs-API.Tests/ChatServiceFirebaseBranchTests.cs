using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class ChatServiceFirebaseBranchTests
{
    private readonly Mock<IChatRepository> _chats = new();
    private readonly Mock<IUserRepository> _usuarios = new();
    private readonly Mock<ICatalogRepository> _catalogs = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();
    private readonly Mock<IHistorialRepository> _historial = new();
    private readonly Mock<FirebaseService> _firebase;

    public ChatServiceFirebaseBranchTests()
    {
        var opts = Options.Create(new FirebaseOptions { ProjectId = "test", DatabaseUrl = "https://test.firebaseio.com" });
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<FirebaseService>>();
        var httpFactory = new Mock<IHttpClientFactory>();
        _firebase = new Mock<FirebaseService>(opts, logger.Object, httpFactory.Object);
        _firebase.Setup(f => f.WriteMetaAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private ChatService CreateSut() => new(
        _chats.Object, _usuarios.Object, _catalogs.Object, _sesiones.Object, _authLogs.Object, _historial.Object, _firebase.Object);

    private void SetupSesion(int userId, string rol = "Psicologo")
    {
        var sesion = new SesionDto(1, userId, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var user = new UsuarioDto { IdUsuario = userId, IdRol = rol == "Psicologo" ? 2 : 1, IdEstadoCuenta = 1, IdEstadoAprobacion = rol == "Psicologo" ? 1 : null, Nombre = "Nombre", Apellido = "Apellido" };
        _usuarios.Setup(u => u.FindByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _catalogs.Setup(c => c.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(rol);
        _usuarios.Setup(u => u.GetEstadoCuentaNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
        if (rol == "Psicologo")
            _catalogs.Setup(c => c.GetEstadoNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Aprobado");
    }

    [Fact]
    public async Task Responder_Aceptar_LlamaFirebaseYOtorgaAutorizacion()
    {
        SetupSesion(1, "Paciente");
        var conv = new ConversacionRow { IdConversacion = 1, IdPaciente = 1, IdPsicologo = 5, Estado = "Pendiente", IdSolicitante = 5 };
        _chats.Setup(c => c.FindAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(conv);
        _chats.Setup(c => c.SetEstadoAsync(1, "Aceptada", null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>())).ReturnsAsync(8);
        _historial.Setup(h => h.AsegurarAutorizacionAsync(1, 5, 8, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _chats.SetupSequence(c => c.FindAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conv)
            .ReturnsAsync(new ConversacionRow { IdConversacion = 1, IdPaciente = 1, IdPsicologo = 5, Estado = "Aceptada", FechaCreacion = DateTime.UtcNow });

        var sut = CreateSut();
        var result = await sut.ResponderAsync("token", 1, true, "127.0.0.1", CancellationToken.None);

        result.Estado.Should().Be("Aceptada");
        _firebase.Verify(f => f.WriteMetaAsync(1, 1, 5, "Aceptada", null, It.IsAny<CancellationToken>()), Times.Once);
        _historial.Verify(h => h.AsegurarAutorizacionAsync(1, 5, 8, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResponderPsicologo_Aceptar_LlamaFirebaseYOtorgaAutorizacion()
    {
        SetupSesion(5, "Psicologo");
        var conv = new ConversacionRow { IdConversacion = 1, IdPaciente = 1, IdPsicologo = 5, Estado = "Pendiente", IdSolicitante = 1 };
        _chats.Setup(c => c.FindAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(conv);
        _chats.Setup(c => c.SetEstadoAsync(1, "Aceptada", null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>())).ReturnsAsync(8);
        _historial.Setup(h => h.AsegurarAutorizacionAsync(1, 5, 8, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _chats.SetupSequence(c => c.FindAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conv)
            .ReturnsAsync(new ConversacionRow { IdConversacion = 1, IdPaciente = 1, IdPsicologo = 5, Estado = "Aceptada", FechaCreacion = DateTime.UtcNow });

        var sut = CreateSut();
        var result = await sut.ResponderPsicologoAsync("token", 1, true, "127.0.0.1", CancellationToken.None);

        result.Estado.Should().Be("Aceptada");
        _firebase.Verify(f => f.WriteMetaAsync(1, 1, 5, "Aceptada", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SolicitarBloqueo_HappyPath_LlamaFirebase()
    {
        SetupSesion(1, "Paciente");
        var conv = new ConversacionRow { IdConversacion = 1, IdPaciente = 1, IdPsicologo = 5, Estado = "Aceptada" };
        _chats.Setup(c => c.FindAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(conv);
        _chats.Setup(c => c.SolicitarBloqueoAsync(1, "motivo", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _firebase.Setup(f => f.WriteMetaAsync(1, 1, 5, "BloqueoPendiente", 1, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _chats.SetupSequence(c => c.FindAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conv)
            .ReturnsAsync(new ConversacionRow { IdConversacion = 1, IdPaciente = 1, IdPsicologo = 5, Estado = "BloqueoPendiente", FechaCreacion = DateTime.UtcNow });

        var sut = CreateSut();
        var result = await sut.SolicitarBloqueoAsync("token", 1, "  motivo  ", "127.0.0.1", CancellationToken.None);

        result.Estado.Should().Be("BloqueoPendiente");
        _firebase.Verify(f => f.WriteMetaAsync(1, 1, 5, "BloqueoPendiente", 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolverBloqueo_Aceptar_LlamaFirebaseYBloquea()
    {
        SetupSesion(5, "Psicologo");
        var conv = new ConversacionRow { IdConversacion = 1, IdPaciente = 1, IdPsicologo = 5, Estado = "BloqueoPendiente" };
        _chats.Setup(c => c.FindAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(conv);
        _chats.Setup(c => c.SetEstadoAsync(1, "Bloqueada", 1, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _firebase.Setup(f => f.WriteMetaAsync(1, 1, 5, "Bloqueada", 1, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _chats.SetupSequence(c => c.FindAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conv)
            .ReturnsAsync(new ConversacionRow { IdConversacion = 1, IdPaciente = 1, IdPsicologo = 5, Estado = "Bloqueada", FechaCreacion = DateTime.UtcNow });

        var sut = CreateSut();
        var result = await sut.ResolverBloqueoAsync("token", 1, true, "127.0.0.1", CancellationToken.None);

        result.Estado.Should().Be("Bloqueada");
        _firebase.Verify(f => f.WriteMetaAsync(1, 1, 5, "Bloqueada", 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolverBloqueo_Rechazar_LlamaFirebaseYAcepta()
    {
        SetupSesion(5, "Psicologo");
        var conv = new ConversacionRow { IdConversacion = 1, IdPaciente = 1, IdPsicologo = 5, Estado = "BloqueoPendiente" };
        _chats.Setup(c => c.FindAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(conv);
        _chats.Setup(c => c.SetEstadoAsync(1, "Aceptada", null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _firebase.Setup(f => f.WriteMetaAsync(1, 1, 5, "Aceptada", null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _chats.SetupSequence(c => c.FindAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conv)
            .ReturnsAsync(new ConversacionRow { IdConversacion = 1, IdPaciente = 1, IdPsicologo = 5, Estado = "Aceptada", FechaCreacion = DateTime.UtcNow });

        var sut = CreateSut();
        var result = await sut.ResolverBloqueoAsync("token", 1, false, "127.0.0.1", CancellationToken.None);

        result.Estado.Should().Be("Aceptada");
        _firebase.Verify(f => f.WriteMetaAsync(1, 1, 5, "Aceptada", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Desbloquear_HappyPath_LlamaFirebase()
    {
        SetupSesion(1, "Paciente");
        var conv = new ConversacionRow { IdConversacion = 1, IdPaciente = 1, IdPsicologo = 5, Estado = "Bloqueada", IdBloqueador = 1 };
        _chats.Setup(c => c.FindAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(conv);
        _chats.Setup(c => c.SetEstadoAsync(1, "Aceptada", null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _firebase.Setup(f => f.WriteMetaAsync(1, 1, 5, "Aceptada", null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _chats.SetupSequence(c => c.FindAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conv)
            .ReturnsAsync(new ConversacionRow { IdConversacion = 1, IdPaciente = 1, IdPsicologo = 5, Estado = "Aceptada", FechaCreacion = DateTime.UtcNow });

        var sut = CreateSut();
        var result = await sut.DesbloquearAsync("token", 1, "127.0.0.1", CancellationToken.None);

        result.Estado.Should().Be("Aceptada");
        _firebase.Verify(f => f.WriteMetaAsync(1, 1, 5, "Aceptada", null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
