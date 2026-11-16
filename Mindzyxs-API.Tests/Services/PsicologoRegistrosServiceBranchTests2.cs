using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class PsicologoRegistrosServiceBranchTests2
{
    private readonly Mock<IHistorialRepository> _historial = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICatalogRepository> _catalogs = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();

    private PsicologoRegistrosService CreateSut() => new(_historial.Object, _users.Object, _catalogs.Object, _sesiones.Object, _authLogs.Object);

    private void ConfigurarPsicologoAutenticado(int idUsuario = 7)
    {
        var sesion = new SesionDto(1, idUsuario, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);
        _users.Setup(u => u.FindByIdAsync(idUsuario, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioDto { IdUsuario = idUsuario, IdRol = 2, IdEstadoCuenta = 1 });
        _users.Setup(u => u.GetRolNombreAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Psicologo");
        _users.Setup(u => u.GetEstadoCuentaNombreAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Activa");
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Psicologo");
    }

    private void ConfigurarHistorialVacio()
    {
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        _historial.Setup(h => h.EstaAutorizadoAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _historial.Setup(h => h.GetEventosAsync(It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EventoRow>());
    }

    // ---- RegistrosAsync: fecha invalida ----

    [Fact]
    public async Task Registros_FechaDesdePosteriorAHasta_LanzaInvalidOperationException()
    {
        ConfigurarPsicologoAutenticado();
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        _historial.Setup(h => h.EstaAutorizadoAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();
        var act = () => sut.RegistrosAsync("token", 1, DateTime.UtcNow, DateTime.UtcNow.AddDays(-1), "ip", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("La fecha \"Desde\" no puede ser posterior a \"Hasta\".");
    }

    // ---- RegistrosAsync: no autorizado ----

    [Fact]
    public async Task Registros_PacienteNoAutorizado_LanzaInvalidOperationException()
    {
        ConfigurarPsicologoAutenticado();
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        _historial.Setup(h => h.EstaAutorizadoAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateSut();
        var act = () => sut.RegistrosAsync("token", 1, null, null, "ip", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No tienes autorización para ver los registros de este paciente.");
    }

    // ---- RegistrosAsync: sin eventos ----

    [Fact]
    public async Task Registros_SinEventos_DevuelveListaVacia()
    {
        ConfigurarPsicologoAutenticado();
        ConfigurarHistorialVacio();

        var sut = CreateSut();
        var result = await sut.RegistrosAsync("token", 1, null, null, "ip", CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ---- RegistrosAsync: evento de cuestionario con nivel y cuestionario ----

    [Fact]
    public async Task Registros_EventoCuestionarioConNivelYCuestionario_ArmaResumenCompleto()
    {
        ConfigurarPsicologoAutenticado();
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        _historial.Setup(h => h.EstaAutorizadoAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _historial.Setup(h => h.GetEventosAsync(It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EventoRow>
            {
                new() { IdEvento = 1, IdPaciente = 1, IdTipoEvento = 2, IdCuestionario = 10, IdNivelRiesgo = 3, PuntajeTotal = 42, FechaEvento = DateTime.UtcNow }
            });
        _historial.Setup(h => h.GetNivelesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, string> { [3] = "Medio" });
        _historial.Setup(h => h.GetCuestionariosTituloAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, string> { [10] = "Ansiedad" });

        var sut = CreateSut();
        var result = await sut.RegistrosAsync("token", 1, null, null, "ip", CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Tipo.Should().Be("Cuestionario");
        result[0].Resumen.Should().Contain("Ansiedad");
        result[0].Resumen.Should().Contain("puntaje 42");
        result[0].Resumen.Should().Contain("riesgo Medio");
    }

    // ---- RegistrosAsync: evento de cuestionario sin nivel ni cuestionario ni puntaje ----

    [Fact]
    public async Task Registros_EventoCuestionarioSinDatos_ArmaResumenMinimo()
    {
        ConfigurarPsicologoAutenticado();
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        _historial.Setup(h => h.EstaAutorizadoAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _historial.Setup(h => h.GetEventosAsync(It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EventoRow>
            {
                new() { IdEvento = 2, IdPaciente = 1, IdTipoEvento = 2, IdCuestionario = null, IdNivelRiesgo = null, PuntajeTotal = null, FechaEvento = DateTime.UtcNow }
            });
        _historial.Setup(h => h.GetNivelesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, string>());
        _historial.Setup(h => h.GetCuestionariosTituloAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, string>());

        var sut = CreateSut();
        var result = await sut.RegistrosAsync("token", 1, null, null, "ip", CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Tipo.Should().Be("Cuestionario");
        result[0].Resumen.Should().Be("Cuestionario");
    }

    // ---- RegistrosAsync: evento de registro emocional con puntaje ----

    [Fact]
    public async Task Registros_EventoEmocionalConPuntaje_ArmaResumenConPuntaje()
    {
        ConfigurarPsicologoAutenticado();
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        _historial.Setup(h => h.EstaAutorizadoAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _historial.Setup(h => h.GetEventosAsync(It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EventoRow>
            {
                new() { IdEvento = 3, IdPaciente = 1, IdTipoEvento = 1, PuntajeTotal = 4, FechaEvento = DateTime.UtcNow }
            });
        _historial.Setup(h => h.GetNivelesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, string>());
        _historial.Setup(h => h.GetCuestionariosTituloAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, string>());

        var sut = CreateSut();
        var result = await sut.RegistrosAsync("token", 1, null, null, "ip", CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Tipo.Should().Be("RegistroEmocional");
        result[0].Resumen.Should().Be("Registro emocional: 4/5");
    }

    // ---- RegistrosAsync: evento de registro emocional sin puntaje ----

    [Fact]
    public async Task Registros_EventoEmocionalSinPuntaje_ArmaResumenMinimo()
    {
        ConfigurarPsicologoAutenticado();
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        _historial.Setup(h => h.EstaAutorizadoAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _historial.Setup(h => h.GetEventosAsync(It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EventoRow>
            {
                new() { IdEvento = 4, IdPaciente = 1, IdTipoEvento = 1, PuntajeTotal = null, FechaEvento = DateTime.UtcNow }
            });
        _historial.Setup(h => h.GetNivelesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, string>());
        _historial.Setup(h => h.GetCuestionariosTituloAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, string>());

        var sut = CreateSut();
        var result = await sut.RegistrosAsync("token", 1, null, null, "ip", CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Tipo.Should().Be("RegistroEmocional");
        result[0].Resumen.Should().Be("Registro emocional");
    }

    // ---- DetalleAsync: registro no encontrado ----

    [Fact]
    public async Task Detalle_RegistroNoEncontrado_LanzaInvalidOperationException()
    {
        ConfigurarPsicologoAutenticado();
        _historial.Setup(h => h.FindEventoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventoRow?)null);

        var sut = CreateSut();
        var act = () => sut.DetalleAsync("token", 99, "ip", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Registro no encontrado.");
    }

    // ---- DetalleAsync: no autorizado ----

    [Fact]
    public async Task Detalle_PacienteNoAutorizado_LanzaInvalidOperationException()
    {
        ConfigurarPsicologoAutenticado();
        _historial.Setup(h => h.FindEventoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventoRow { IdEvento = 5, IdPaciente = 1, IdTipoEvento = 1 });
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        _historial.Setup(h => h.EstaAutorizadoAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateSut();
        var act = () => sut.DetalleAsync("token", 5, "ip", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No tienes autorización para ver este registro.");
    }

    // ---- DetalleAsync: con respuestas y observaciones ----

    [Fact]
    public async Task Detalle_ConRespuestasYObservaciones_ArmaDetalleCompleto()
    {
        ConfigurarPsicologoAutenticado();
        var evento = new EventoRow { IdEvento = 5, IdPaciente = 1, IdTipoEvento = 2, IdCuestionario = 10, IdNivelRiesgo = 3, PuntajeTotal = 42, FechaEvento = DateTime.UtcNow, Nota = "nota" };
        _historial.Setup(h => h.FindEventoAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evento);
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        _historial.Setup(h => h.EstaAutorizadoAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _historial.Setup(h => h.GetNivelesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, string> { [3] = "Medio" });
        _historial.Setup(h => h.GetCuestionariosTituloAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, string> { [10] = "Ansiedad" });
        _historial.Setup(h => h.GetRespuestasAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RespuestaRow> { new() { IdEvento = 5, IdPregunta = 1, Valor = 3, Texto = null } });
        _historial.Setup(h => h.GetPreguntasTextoAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, string> { [1] = "Pregunta 1" });
        _historial.Setup(h => h.GetTextosOpcionAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<(int, int), string> { [(1, 3)] = "Opción 3" });
        _historial.Setup(h => h.GetObservacionesAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ObservacionRow> { new() { Contenido = "obs", Firma = "firma", FechaObservacion = DateTime.UtcNow } });

        var sut = CreateSut();
        var result = await sut.DetalleAsync("token", 5, "ip", CancellationToken.None);

        result.Should().NotBeNull();
        result.Respuestas.Should().HaveCount(1);
        result.Respuestas![0].Texto.Should().Be("Opción 3");
        result.Observaciones.Should().HaveCount(1);
    }

    // ---- RequirePsicologo: sesion inexistente ----

    [Fact]
    public async Task Pacientes_SesionInexistente_LanzaUnauthorizedAccessException()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SesionDto?)null);

        var sut = CreateSut();
        var act = () => sut.PacientesAsync("token", null, "ip", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Sesión requerida");
    }
    // ---- RequirePsicologo: sesion expirada ----

    [Fact]
    public async Task Pacientes_SesionExpirada_LanzaUnauthorizedAccessException()
    {
        var sesion = new SesionDto(1, 7, "hash", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);

        var sut = CreateSut();
        var act = () => sut.PacientesAsync("token", null, "ip", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Sesión requerida");
    }

    // ---- RequirePsicologo: usuario no encontrado ----

    [Fact]
    public async Task Pacientes_UsuarioNoEncontrado_LanzaUnauthorizedAccessException()
    {
        var sesion = new SesionDto(1, 7, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);
        _users.Setup(u => u.FindByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var act = () => sut.PacientesAsync("token", null, "ip", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Sesión requerida");
    }

    // ---- RequirePsicologo: rol incorrecto ----

    [Fact]
    public async Task Pacientes_RolIncorrecto_LanzaInvalidOperationException()
    {
        var sesion = new SesionDto(1, 7, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);
        _users.Setup(u => u.FindByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioDto { IdUsuario = 7, IdRol = 2, IdEstadoCuenta = 1 });
        _users.Setup(u => u.GetRolNombreAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Paciente");

        var sut = CreateSut();
        var act = () => sut.PacientesAsync("token", null, "ip", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No tienes permisos para acceder a esta función");
    }

    // ---- RequirePsicologo: cuenta inactiva ----

    [Fact]
    public async Task Pacientes_CuentaInactiva_LanzaInvalidOperationException()
    {
        var sesion = new SesionDto(1, 7, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);
        _users.Setup(u => u.FindByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioDto { IdUsuario = 7, IdRol = 2, IdEstadoCuenta = 1 });
        _users.Setup(u => u.GetRolNombreAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Psicologo");
        _users.Setup(u => u.GetEstadoCuentaNombreAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Suspendida");

        var sut = CreateSut();
        var act = () => sut.PacientesAsync("token", null, "ip", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("La cuenta no está activa.");
    }

    // ---- RequirePsicologo: no aprobado ----

    [Fact]
    public async Task Pacientes_NoAprobado_LanzaInvalidOperationException()
    {
        var sesion = new SesionDto(1, 7, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);
        _users.Setup(u => u.FindByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioDto { IdUsuario = 7, IdRol = 2, IdEstadoCuenta = 1, IdEstadoAprobacion = 9 });
        _users.Setup(u => u.GetRolNombreAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Psicologo");
        _users.Setup(u => u.GetEstadoCuentaNombreAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Activa");
        _catalogs.Setup(c => c.GetEstadoNombreAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Pendiente");

        var sut = CreateSut();
        var act = () => sut.PacientesAsync("token", null, "ip", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("El psicólogo aún no está aprobado.");
    }
    // ---- PacientesAsync: con busqueda ----

    [Fact]
    public async Task Pacientes_ConBusqueda_FiltraPorNombre()
    {
        ConfigurarPsicologoAutenticado();
        var usuarios = new List<UsuarioDto>
        {
            new() { IdUsuario = 1, Nombre = "Ana", Apellido = "Perez", Correo = "ana@correo.com" },
            new() { IdUsuario = 2, Nombre = "Luis", Apellido = "Gomez", Correo = "luis@correo.com" }
        };
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        _historial.Setup(h => h.GetPacientesAutorizadosAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuarios);

        var sut = CreateSut();
        var result = await sut.PacientesAsync("token", "ana", "ip", CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Nombre.Should().Be("Ana Perez");
    }
}
