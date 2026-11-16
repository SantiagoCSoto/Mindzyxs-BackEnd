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

public class ChatServiceBranchTargeted3Tests
{
    private readonly Mock<IChatRepository> _chats = new();
    private readonly Mock<IUserRepository> _usuarios = new();
    private readonly Mock<ICatalogRepository> _catalogs = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();
    private readonly Mock<IHistorialRepository> _historial = new();
    private readonly Mock<FirebaseService> _firebase;

    public ChatServiceBranchTargeted3Tests()
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
    public async Task AvisarMensaje_PsicologoEnvia_RegistraLogConPaciente()
    {
        SetupSesion(5, "Psicologo");
        var conv = new ConversacionRow { IdConversacion = 1, IdPaciente = 1, IdPsicologo = 5, Estado = "Aceptada" };
        _chats.Setup(c => c.FindAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(conv);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = CreateSut();
        await sut.AvisarMensajeAsync("token", 1, "127.0.0.1", CancellationToken.None);

        _authLogs.Verify(a => a.LogAsync(5, 1, LogAcciones.ChatMensaje, It.IsAny<string>(), "127.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BuscarPacientes_NivelMedio_RetornaLista()
    {
        SetupSesion(5, "Psicologo");
        _catalogs.Setup(c => c.GetRolIdAsync("Paciente", It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _catalogs.Setup(c => c.GetEstadoIdAsync("Activa", It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _chats.Setup(c => c.BuscarPacientesAsync(1, 2, null, "Medio", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PacienteBusquedaDto> { new(10, "Ana Paz", "ana@test.com", 5, "Medio", DateTime.UtcNow, 2, 1) });

        var sut = CreateSut();
        var result = await sut.BuscarPacientesAsync("token", null, "medio", null, null, CancellationToken.None);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task BuscarPacientes_NivelAlto_RetornaLista()
    {
        SetupSesion(5, "Psicologo");
        _catalogs.Setup(c => c.GetRolIdAsync("Paciente", It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _catalogs.Setup(c => c.GetEstadoIdAsync("Activa", It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _chats.Setup(c => c.BuscarPacientesAsync(1, 2, null, "Alto", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PacienteBusquedaDto> { new(10, "Ana Paz", "ana@test.com", 5, "Alto", DateTime.UtcNow, 2, 1) });

        var sut = CreateSut();
        var result = await sut.BuscarPacientesAsync("token", null, "alto", null, null, CancellationToken.None);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task BuscarPacientes_NivelInvalido_NormalizaNull()
    {
        SetupSesion(5, "Psicologo");
        _catalogs.Setup(c => c.GetRolIdAsync("Paciente", It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _catalogs.Setup(c => c.GetEstadoIdAsync("Activa", It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _chats.Setup(c => c.BuscarPacientesAsync(1, 2, "ana", null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PacienteBusquedaDto>());

        var sut = CreateSut();
        var result = await sut.BuscarPacientesAsync("token", "ana", "invalido", null, null, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task BuscarPacientes_CorreoEnmascarado()
    {
        SetupSesion(5, "Psicologo");
        _catalogs.Setup(c => c.GetRolIdAsync("Paciente", It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _catalogs.Setup(c => c.GetEstadoIdAsync("Activa", It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _chats.Setup(c => c.BuscarPacientesAsync(1, 2, "ana", null, 2, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PacienteBusquedaDto> { new(10, "Ana Paz", "ana@test.com", 5, "Bajo", DateTime.UtcNow, 2, 1) });

        var sut = CreateSut();
        var result = await sut.BuscarPacientesAsync("token", "ana", null, 2, true, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Correo.Should().Be("a***@test.com");
    }

    [Fact]
    public async Task Desbloquear_BloqueadoPorMi_RetornaOk()
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
    }

    [Fact]
    public async Task ResolverBloqueo_AdminResuelve_LlamaFirebase()
    {
        SetupSesion(99, "Administrador");
        _usuarios.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Administrador");
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
    }

    [Fact]
    public async Task MisConversaciones_ConContraparteNull_UsaFallback()
    {
        SetupSesion(5, "Psicologo");
        _chats.Setup(c => c.GetDeUsuarioAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new List<ConversacionRow>
        {
            new() { IdConversacion = 1, IdPaciente = 1, IdPsicologo = 5, Estado = "Aceptada", FechaCreacion = DateTime.UtcNow }
        });
        _usuarios.Setup(u => u.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.MisConversacionesAsync("token", "127.0.0.1", CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Contraparte.Nombre.Should().Contain("Usuario:");
    }
}
