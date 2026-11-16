using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class ChatServiceBranchTargetedTests
{
    private readonly Mock<IChatRepository> _chats = new();
    private readonly Mock<IUserRepository> _usuarios = new();
    private readonly Mock<ICatalogRepository> _catalogs = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();
    private readonly Mock<IHistorialRepository> _historial = new();
    private readonly Mock<FirebaseService> _firebase;

    public ChatServiceBranchTargetedTests()
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
    public async Task MisConversaciones_ConConversaciones_RetornaLista()
    {
        SetupSesion(5, "Psicologo");
        _chats.Setup(c => c.GetDeUsuarioAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new List<ConversacionRow>
        {
            new() { IdConversacion = 1, IdPaciente = 1, IdPsicologo = 5, Estado = "Aceptada", FechaCreacion = DateTime.UtcNow }
        });
        _usuarios.Setup(u => u.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new UsuarioDto { IdUsuario = 1, Nombre = "Ana", Apellido = "Paz", IdRol = 1 });
        _catalogs.Setup(c => c.GetRolNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.MisConversacionesAsync("token", "127.0.0.1", CancellationToken.None);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task SolicitarPaciente_PsicologoNoEncontrado_Lanza()
    {
        SetupSesion(1, "Paciente");
        _usuarios.Setup(u => u.FindByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var act = () => sut.SolicitarPacienteAsync("token", 99, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Psicólogo no encontrado*");
    }

    [Fact]
    public async Task SolicitarPaciente_ConversacionActiva_Lanza()
    {
        SetupSesion(1, "Paciente");
        var psi = new UsuarioDto { IdUsuario = 5, IdRol = 2, IdEstadoCuenta = 1, IdEstadoAprobacion = 1 };
        _usuarios.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(psi);
        _catalogs.Setup(c => c.GetRolNombreAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync("Psicologo");
        _usuarios.Setup(u => u.GetEstadoCuentaNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
        _catalogs.Setup(c => c.GetEstadoNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Aprobado");
        var existente = new ConversacionRow { IdConversacion = 7, IdPaciente = 1, IdPsicologo = 5, Estado = "Aceptada" };
        _chats.Setup(c => c.FindParejaAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(existente);

        var sut = CreateSut();
        var act = () => sut.SolicitarPacienteAsync("token", 5, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Ya existe*");
    }

    [Fact]
    public async Task AvisarMensaje_BloqueadoPorOtro_Lanza()
    {
        SetupSesion(1, "Paciente");
        var conv = new ConversacionRow { IdConversacion = 1, IdPaciente = 1, IdPsicologo = 5, Estado = "Bloqueada", IdBloqueador = 5 };
        _chats.Setup(c => c.FindAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(conv);

        var sut = CreateSut();
        var act = () => sut.AvisarMensajeAsync("token", 1, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*bloqueado*");
    }

    [Fact]
    public async Task AvisarMensaje_BloqueoPendiente_Lanza()
    {
        SetupSesion(1, "Paciente");
        var conv = new ConversacionRow { IdConversacion = 1, IdPaciente = 1, IdPsicologo = 5, Estado = "BloqueoPendiente" };
        _chats.Setup(c => c.FindAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(conv);

        var sut = CreateSut();
        var act = () => sut.AvisarMensajeAsync("token", 1, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*pausado*");
    }

    [Fact]
    public async Task AvisarMensaje_NoAceptado_Lanza()
    {
        SetupSesion(1, "Paciente");
        var conv = new ConversacionRow { IdConversacion = 1, IdPaciente = 1, IdPsicologo = 5, Estado = "Pendiente" };
        _chats.Setup(c => c.FindAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(conv);

        var sut = CreateSut();
        var act = () => sut.AvisarMensajeAsync("token", 1, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no está aceptado*");
    }

    [Fact]
    public async Task ResolverBloqueo_PacienteCancela_Lanza()
    {
        SetupSesion(1, "Paciente");
        var conv = new ConversacionRow { IdConversacion = 1, IdPaciente = 1, IdPsicologo = 5, Estado = "BloqueoPendiente" };
        _chats.Setup(c => c.FindAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(conv);

        var sut = CreateSut();
        var act = () => sut.ResolverBloqueoAsync("token", 1, true, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*No puedes confirmar*");
    }

    [Fact]
    public async Task ResolverBloqueo_SinPermiso_Lanza()
    {
        SetupSesion(99, "Paciente");
        var conv = new ConversacionRow { IdConversacion = 1, IdPaciente = 1, IdPsicologo = 5, Estado = "BloqueoPendiente" };
        _chats.Setup(c => c.FindAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(conv);
        _usuarios.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");

        var sut = CreateSut();
        var act = () => sut.ResolverBloqueoAsync("token", 1, false, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*No tienes permiso*");
    }

    [Fact]
    public async Task Desbloquear_NoEsBloqueador_Lanza()
    {
        SetupSesion(1, "Paciente");
        var conv = new ConversacionRow { IdConversacion = 1, IdPaciente = 1, IdPsicologo = 5, Estado = "Bloqueada", IdBloqueador = 5 };
        _chats.Setup(c => c.FindAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(conv);

        var sut = CreateSut();
        var act = () => sut.DesbloquearAsync("token", 1, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*No puedes desbloquear*");
    }
}
