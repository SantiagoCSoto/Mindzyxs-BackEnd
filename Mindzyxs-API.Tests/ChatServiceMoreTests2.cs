using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Mindzyxs_API.Utilities;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class ChatServiceMoreTests2
{
    private readonly Mock<IChatRepository> _chats = new();
    private readonly Mock<IUserRepository> _usuarios = new();
    private readonly Mock<ICatalogRepository> _catalogs = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();
    private readonly Mock<IHistorialRepository> _historial = new();
    private readonly FirebaseService _firebase;

    public ChatServiceMoreTests2()
    {
        var opts = Microsoft.Extensions.Options.Options.Create(new FirebaseOptions { ProjectId = "test", DatabaseUrl = "https://test.firebaseio.com" });
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<FirebaseService>>();
        var httpFactory = new Mock<IHttpClientFactory>();
        _firebase = new FirebaseService(opts, logger.Object, httpFactory.Object);
    }

    private ChatService CreateSut() => new(
        _chats.Object, _usuarios.Object, _catalogs.Object, _sesiones.Object, _authLogs.Object, _historial.Object, _firebase);

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
    public async Task CrearSolicitud_HappyPath_CreaYRetornaDto()
    {
        SetupSesion(5, "Psicologo");
        var pac = new UsuarioDto { IdUsuario = 10, IdRol = 1, IdEstadoCuenta = 1 };
        _usuarios.Setup(u => u.FindByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(pac);
        _catalogs.Setup(c => c.GetRolNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");
        _usuarios.Setup(u => u.GetEstadoCuentaNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
        _chats.Setup(c => c.FindParejaAsync(10, 5, It.IsAny<CancellationToken>())).ReturnsAsync((ConversacionRow?)null);
        _chats.Setup(c => c.CrearAsync(10, 5, 5, It.IsAny<CancellationToken>())).ReturnsAsync(new ConversacionRow { IdConversacion = 99, IdPaciente = 10, IdPsicologo = 5, Estado = "Pendiente", FechaCreacion = DateTime.UtcNow, IdSolicitante = 5 });
        _chats.Setup(c => c.FindAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync(new ConversacionRow { IdConversacion = 99, IdPaciente = 10, IdPsicologo = 5, Estado = "Pendiente", FechaCreacion = DateTime.UtcNow, IdSolicitante = 5 });
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _usuarios.Setup(u => u.FindByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new UsuarioDto { IdUsuario = 10, Nombre = "Ana", Apellido = "Paz", IdRol = 1 });
        _catalogs.Setup(c => c.GetRolNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");

        var sut = CreateSut();
        var result = await sut.CrearSolicitudAsync("token", 10, "127.0.0.1", CancellationToken.None);

        result.Id.Should().Be(99);
        result.Estado.Should().Be("Pendiente");
    }

    [Fact]
    public async Task CrearSolicitud_ConversacionExistenteRechazada_Reactiva()
    {
        SetupSesion(5, "Psicologo");
        var pac = new UsuarioDto { IdUsuario = 10, IdRol = 1, IdEstadoCuenta = 1 };
        _usuarios.Setup(u => u.FindByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(pac);
        _catalogs.Setup(c => c.GetRolNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");
        _usuarios.Setup(u => u.GetEstadoCuentaNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
        var existente = new ConversacionRow { IdConversacion = 7, IdPaciente = 10, IdPsicologo = 5, Estado = "Rechazada" };
        _chats.Setup(c => c.FindParejaAsync(10, 5, It.IsAny<CancellationToken>())).ReturnsAsync(existente);
        _chats.Setup(c => c.SetEstadoAsync(7, "Pendiente", null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _chats.Setup(c => c.FindAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(new ConversacionRow { IdConversacion = 7, IdPaciente = 10, IdPsicologo = 5, Estado = "Pendiente", FechaCreacion = DateTime.UtcNow, IdSolicitante = 5 });
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _usuarios.Setup(u => u.FindByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new UsuarioDto { IdUsuario = 10, Nombre = "Ana", Apellido = "Paz", IdRol = 1 });
        _catalogs.Setup(c => c.GetRolNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");

        var sut = CreateSut();
        var result = await sut.CrearSolicitudAsync("token", 10, "127.0.0.1", CancellationToken.None);

        result.Id.Should().Be(7);
    }

    [Fact]
    public async Task SolicitarPaciente_HappyPath_CreaYRetornaDto()
    {
        SetupSesion(1, "Paciente");
        var psi = new UsuarioDto { IdUsuario = 5, IdRol = 2, IdEstadoCuenta = 1, IdEstadoAprobacion = 1 };
        _usuarios.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(psi);
        _catalogs.Setup(c => c.GetRolNombreAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync("Psicologo");
        _usuarios.Setup(u => u.GetEstadoCuentaNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
        _catalogs.Setup(c => c.GetEstadoNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Aprobado");
        _chats.Setup(c => c.FindParejaAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync((ConversacionRow?)null);
        _chats.Setup(c => c.CrearAsync(1, 5, 1, It.IsAny<CancellationToken>())).ReturnsAsync(new ConversacionRow { IdConversacion = 88, IdPaciente = 1, IdPsicologo = 5, Estado = "Pendiente", FechaCreacion = DateTime.UtcNow, IdSolicitante = 1 });
        _chats.Setup(c => c.FindAsync(88, It.IsAny<CancellationToken>())).ReturnsAsync(new ConversacionRow { IdConversacion = 88, IdPaciente = 1, IdPsicologo = 5, Estado = "Pendiente", FechaCreacion = DateTime.UtcNow, IdSolicitante = 1 });
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _usuarios.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new UsuarioDto { IdUsuario = 5, Nombre = "Laura", Apellido = "Gomez", IdRol = 2 });
        _catalogs.Setup(c => c.GetRolNombreAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync("Psicologo");

        var sut = CreateSut();
        var result = await sut.SolicitarPacienteAsync("token", 5, "127.0.0.1", CancellationToken.None);

        result.Id.Should().Be(88);
    }

    [Fact]
    public async Task Responder_Rechazar_NoLlamaFirebaseYRetornaDto()
    {
        SetupSesion(1, "Paciente");
        var conv = new ConversacionRow { IdConversacion = 1, IdPaciente = 1, IdPsicologo = 5, Estado = "Pendiente", IdSolicitante = 5 };
        _chats.Setup(c => c.SetEstadoAsync(1, "Rechazada", null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _chats.SetupSequence(c => c.FindAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conv)
            .ReturnsAsync(new ConversacionRow { IdConversacion = 1, IdPaciente = 1, IdPsicologo = 5, Estado = "Rechazada", FechaCreacion = DateTime.UtcNow });

        var sut = CreateSut();
        var result = await sut.ResponderAsync("token", 1, false, "127.0.0.1", CancellationToken.None);

        result.Estado.Should().Be("Rechazada");
    }

    [Fact]
    public async Task ResponderPsicologo_Rechazar_NoLlamaFirebaseYRetornaDto()
    {
        SetupSesion(5, "Psicologo");
        var conv = new ConversacionRow { IdConversacion = 1, IdPaciente = 1, IdPsicologo = 5, Estado = "Pendiente", IdSolicitante = 1 };
        _chats.Setup(c => c.SetEstadoAsync(1, "Rechazada", null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _chats.SetupSequence(c => c.FindAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conv)
            .ReturnsAsync(new ConversacionRow { IdConversacion = 1, IdPaciente = 1, IdPsicologo = 5, Estado = "Rechazada", FechaCreacion = DateTime.UtcNow });

        var sut = CreateSut();
        var result = await sut.ResponderPsicologoAsync("token", 1, false, "127.0.0.1", CancellationToken.None);

        result.Estado.Should().Be("Rechazada");
    }

    [Fact]
    public async Task AvisarMensaje_HappyPath_RegistraLog()
    {
        SetupSesion(1, "Paciente");
        var conv = new ConversacionRow { IdConversacion = 1, IdPaciente = 1, IdPsicologo = 5, Estado = "Aceptada" };
        _chats.Setup(c => c.FindAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(conv);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = CreateSut();
        await sut.AvisarMensajeAsync("token", 1, "127.0.0.1", CancellationToken.None);

        _authLogs.Verify(a => a.LogAsync(1, 5, LogAcciones.ChatMensaje, It.IsAny<string>(), "127.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BuscarPacientes_HappyPath_RetornaLista()
    {
        SetupSesion(5, "Psicologo");
        _catalogs.Setup(c => c.GetRolIdAsync("Paciente", It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _catalogs.Setup(c => c.GetEstadoIdAsync("Activa", It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _chats.Setup(c => c.BuscarPacientesAsync(1, 2, "ana", "Bajo", 1, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PacienteBusquedaDto> { new(10, "Ana Paz", "ana@test.com", 5, "Bajo", DateTime.UtcNow, 2, 1) });

        var sut = CreateSut();
        var result = await sut.BuscarPacientesAsync("token", "ana", "bajo", 1, true, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Nombre.Should().Be("Ana Paz");
        result[0].Correo.Should().Contain("***");
    }
}
