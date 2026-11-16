using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class ChatServiceMoreTests
{
    private readonly Mock<IChatRepository> _chats = new();
    private readonly Mock<IUserRepository> _usuarios = new();
    private readonly Mock<ICatalogRepository> _catalogs = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();
    private readonly Mock<IHistorialRepository> _historial = new();
    private readonly FirebaseService _firebase;

    public ChatServiceMoreTests()
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
        var user = new UsuarioDto { IdUsuario = userId, IdRol = rol == "Psicologo" ? 2 : 1, IdEstadoCuenta = 1, IdEstadoAprobacion = rol == "Psicologo" ? 1 : null };
        _usuarios.Setup(u => u.FindByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _catalogs.Setup(c => c.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(rol);
        _usuarios.Setup(u => u.GetEstadoCuentaNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
        if (rol == "Psicologo")
            _catalogs.Setup(c => c.GetEstadoNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Aprobado");
    }

    [Fact]
    public async Task MisConversaciones_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var act = () => sut.MisConversacionesAsync(null, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task MisConversaciones_ConConversaciones_RetornaLista()
    {
        SetupSesion(5, "Psicologo");
        _chats.Setup(c => c.GetDeUsuarioAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new List<ConversacionRow>
        {
            new() { IdConversacion = 1, IdPaciente = 1, IdPsicologo = 5, Estado = "Aceptada" }
        });

        var sut = CreateSut();
        var result = await sut.MisConversacionesAsync("token", "127.0.0.1", CancellationToken.None);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task SolicitarPaciente_PsicologoNoAprobado_LanzaInvalidOperation()
    {
        var sesion = new SesionDto(1, 1, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var user = new UsuarioDto { IdUsuario = 1, IdRol = 1, IdEstadoCuenta = 1 };
        _usuarios.Setup(u => u.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _catalogs.Setup(c => c.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");
        _usuarios.Setup(u => u.GetEstadoCuentaNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
        _usuarios.Setup(u => u.FindByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync(new UsuarioDto { IdUsuario = 99, IdRol = 2, IdEstadoCuenta = 1, IdEstadoAprobacion = 1 });

        var sut = CreateSut();
        var act = () => sut.SolicitarPacienteAsync("token", 99, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ResolverBloqueo_SinSolicitudPendiente_LanzaInvalidOperation()
    {
        SetupSesion(5, "Psicologo");
        var conv = new ConversacionRow { IdConversacion = 1, IdPaciente = 1, IdPsicologo = 5, Estado = "Aceptada" };
        _chats.Setup(c => c.FindAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(conv);

        var sut = CreateSut();
        var act = () => sut.ResolverBloqueoAsync("token", 1, true, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*No hay solicitud de bloqueo pendiente*");
    }

    [Fact]
    public async Task Desbloquear_NoEsBloqueador_LanzaInvalidOperation()
    {
        SetupSesion(1, "Paciente");
        var conv = new ConversacionRow { IdConversacion = 1, IdPaciente = 1, IdPsicologo = 5, Estado = "Bloqueada", IdBloqueador = 5 };
        _chats.Setup(c => c.FindAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(conv);

        var sut = CreateSut();
        var act = () => sut.DesbloquearAsync("token", 1, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*No puedes desbloquear*");
    }

    [Fact]
    public async Task AvisarMensaje_ChatNoAceptado_LanzaInvalidOperation()
    {
        SetupSesion(1, "Paciente");
        var conv = new ConversacionRow { IdConversacion = 1, IdPaciente = 1, IdPsicologo = 5, Estado = "Pendiente" };
        _chats.Setup(c => c.FindAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(conv);

        var sut = CreateSut();
        var act = () => sut.AvisarMensajeAsync("token", 1, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task BuscarPacientes_SinFiltros_LanzaInvalidOperation()
    {
        SetupSesion(5, "Psicologo");
        var sut = CreateSut();
        var act = () => sut.BuscarPacientesAsync("token", null, null, null, null, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Seleccione al menos un filtro*");
    }
}
