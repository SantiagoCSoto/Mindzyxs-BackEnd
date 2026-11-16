using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class PsicologoCuestionariosServiceMoreTests
{
    private readonly Mock<ICuestionarioRepository> _cuestionarios = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICatalogRepository> _catalogs = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();
    private readonly Mock<IHistorialRepository> _historial = new();

    private PsicologoCuestionariosService CreateSut() => new(
        _cuestionarios.Object, _users.Object, _catalogs.Object, _sesiones.Object, _authLogs.Object, _historial.Object);

    private void SetupSesion(int userId = 5)
    {
        var sesion = new SesionDto(1, userId, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var user = new UsuarioDto { IdUsuario = userId, IdRol = 2, IdEstadoCuenta = 1, IdEstadoAprobacion = 1 };
        _users.Setup(u => u.FindByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Psicologo");
        _users.Setup(u => u.GetEstadoCuentaNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
        _catalogs.Setup(c => c.GetEstadoNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Aprobado");
    }

    private static CuestionarioRow MakeCuest(int id = 1, string estado = "Borrador", int creador = 5) => new()
    {
        IdCuestionario = id, IdCreador = creador, Titulo = "Cuest", Tipo = "Personalizado", Estado = estado,
        FechaCreacion = DateTime.UtcNow
    };

    [Fact]
    public async Task Crear_HappyPath_RetornaIdYLoguea()
    {
        SetupSesion();
        _cuestionarios.Setup(c => c.CrearAsync(It.IsAny<CuestionarioRow>(), It.IsAny<List<(PreguntaRow, List<OpcionPreguntaRow>)>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var dto = new CuestionarioCrearDto
        {
            Titulo = "  Mi cuestionario  ",
            Descripcion = "Desc",
            Publicar = false,
            Preguntas = new List<PreguntaCrearDto>
            {
                new() { Texto = "P1", Opciones = new List<OpcionCrearDto> { new() { Texto = "O1", Valor = 1 }, new() { Texto = "O2", Valor = 2 } } }
            }
        };
        var result = await sut.CrearAsync("token", dto, "127.0.0.1", CancellationToken.None);

        result.Id.Should().Be(42);
        result.Mensaje.Should().Contain("guardado");
    }

    [Fact]
    public async Task Crear_ConUmbralesValidos_ModoSuma()
    {
        SetupSesion();
        _cuestionarios.Setup(c => c.CrearAsync(It.IsAny<CuestionarioRow>(), It.IsAny<List<(PreguntaRow, List<OpcionPreguntaRow>)>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var dto = new CuestionarioCrearDto
        {
            Titulo = "C",
            Publicar = true,
            Umbrales = new UmbralesRiesgoDto { BajoMax = 2, MedioMax = 4 },
            Preguntas = new List<PreguntaCrearDto>
            {
                new() { Texto = "P1", Opciones = new List<OpcionCrearDto> { new() { Texto = "O1", Valor = 1 }, new() { Texto = "O2", Valor = 5 } } }
            }
        };
        var result = await sut.CrearAsync("token", dto, "127.0.0.1", CancellationToken.None);

        result.Id.Should().Be(7);
    }

    [Fact]
    public async Task Crear_PreguntaSinTexto_LanzaInvalidOperation()
    {
        SetupSesion();
        var sut = CreateSut();
        var dto = new CuestionarioCrearDto
        {
            Titulo = "C",
            Preguntas = new List<PreguntaCrearDto>
            {
                new() { Texto = "   ", Opciones = new List<OpcionCrearDto> { new() { Texto = "O1", Valor = 1 }, new() { Texto = "O2", Valor = 2 } } }
            }
        };
        var act = () => sut.CrearAsync("token", dto, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*pregunta debe tener texto*");
    }

    [Fact]
    public async Task Crear_MenosDeDosOpciones_LanzaInvalidOperation()
    {
        SetupSesion();
        var sut = CreateSut();
        var dto = new CuestionarioCrearDto
        {
            Titulo = "C",
            Preguntas = new List<PreguntaCrearDto>
            {
                new() { Texto = "P1", Opciones = new List<OpcionCrearDto> { new() { Texto = "O1", Valor = 1 } } }
            }
        };
        var act = () => sut.CrearAsync("token", dto, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*al menos dos opciones*");
    }

    [Fact]
    public async Task Crear_OpcionSinTexto_LanzaInvalidOperation()
    {
        SetupSesion();
        var sut = CreateSut();
        var dto = new CuestionarioCrearDto
        {
            Titulo = "C",
            Preguntas = new List<PreguntaCrearDto>
            {
                new() { Texto = "P1", Opciones = new List<OpcionCrearDto> { new() { Texto = "O1", Valor = 1 }, new() { Texto = "  ", Valor = 2 } } }
            }
        };
        var act = () => sut.CrearAsync("token", dto, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*opci*n debe tener texto*");
    }

    [Fact]
    public async Task Crear_ValorNegativo_LanzaInvalidOperation()
    {
        SetupSesion();
        var sut = CreateSut();
        var dto = new CuestionarioCrearDto
        {
            Titulo = "C",
            Preguntas = new List<PreguntaCrearDto>
            {
                new() { Texto = "P1", Opciones = new List<OpcionCrearDto> { new() { Texto = "O1", Valor = -1 }, new() { Texto = "O2", Valor = 2 } } }
            }
        };
        var act = () => sut.CrearAsync("token", dto, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*mayor o igual a cero*");
    }

    [Fact]
    public async Task Crear_UmbralesInvalidos_LanzaInvalidOperation()
    {
        SetupSesion();
        var sut = CreateSut();
        var dto = new CuestionarioCrearDto
        {
            Titulo = "C",
            Umbrales = new UmbralesRiesgoDto { BajoMax = 5, MedioMax = 4 },
            Preguntas = new List<PreguntaCrearDto>
            {
                new() { Texto = "P1", Opciones = new List<OpcionCrearDto> { new() { Texto = "O1", Valor = 1 }, new() { Texto = "O2", Valor = 5 } } }
            }
        };
        var act = () => sut.CrearAsync("token", dto, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*intervalos de riesgo no son v*");
    }

    [Fact]
    public async Task Crear_UmbralMedioMayorOIgualMaximo_LanzaInvalidOperation()
    {
        SetupSesion();
        var sut = CreateSut();
        var dto = new CuestionarioCrearDto
        {
            Titulo = "C",
            Umbrales = new UmbralesRiesgoDto { BajoMax = 1, MedioMax = 5 },
            Preguntas = new List<PreguntaCrearDto>
            {
                new() { Texto = "P1", Opciones = new List<OpcionCrearDto> { new() { Texto = "O1", Valor = 1 }, new() { Texto = "O2", Valor = 5 } } }
            }
        };
        var act = () => sut.CrearAsync("token", dto, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Medio debe ser menor*");
    }

    [Fact]
    public async Task Listar_HappyPath_RetornaLista()
    {
        SetupSesion();
        _cuestionarios.Setup(c => c.GetDePsicologoAsync(5, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CuestionarioPsicologoListaDto> { new(1, "C1", null, "Borrador", 2, DateTime.UtcNow) });

        var sut = CreateSut();
        var result = await sut.ListarAsync("token", "  q  ", CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Titulo.Should().Be("C1");
    }

    [Fact]
    public async Task ObtenerDetalle_HappyPath_RetornaDetalle()
    {
        SetupSesion();
        var cuest = MakeCuest(1, "Borrador", 5);
        _cuestionarios.Setup(c => c.FindDePsicologoAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(cuest);
        _cuestionarios.Setup(c => c.GetCompletoAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(
            (new List<PreguntaRow> { new() { IdPregunta = 1, IdCuestionario = 1, Texto = "P1", Tipo = "escala", Orden = 1 } },
             new List<OpcionPreguntaRow> { new() { IdOpcion = 1, IdPregunta = 1, Texto = "O1", Valor = 3, Orden = 1 } }));

        var sut = CreateSut();
        var result = await sut.ObtenerDetalleAsync("token", 1, CancellationToken.None);

        result.Id.Should().Be(1);
        result.Preguntas.Should().HaveCount(1);
        result.Preguntas[0].Opciones.Should().HaveCount(1);
    }

    [Fact]
    public async Task Despublicar_HappyPath_RetornaMensajeYLoguea()
    {
        SetupSesion();
        var cuest = MakeCuest(1, "Publicado", 5);
        _cuestionarios.Setup(c => c.FindDePsicologoAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(cuest);
        _cuestionarios.Setup(c => c.DespublicarAsync(1, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.DespublicarAsync("token", 1, "127.0.0.1", CancellationToken.None);

        result.Should().Contain("borrador");
        _cuestionarios.Verify(c => c.DespublicarAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Despublicar_NoPublicado_LanzaInvalidOperation()
    {
        SetupSesion();
        var cuest = MakeCuest(1, "Borrador", 5);
        _cuestionarios.Setup(c => c.FindDePsicologoAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(cuest);

        var sut = CreateSut();
        var act = () => sut.DespublicarAsync("token", 1, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no est* publicado*");
    }

    [Fact]
    public async Task Actualizar_Publicado_LanzaInvalidOperation()
    {
        SetupSesion();
        var cuest = MakeCuest(1, "Publicado", 5);
        _cuestionarios.Setup(c => c.FindDePsicologoAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(cuest);

        var sut = CreateSut();
        var dto = new CuestionarioCrearDto { Titulo = "T", Preguntas = new List<PreguntaCrearDto> { new() { Texto = "P", Opciones = new List<OpcionCrearDto> { new() { Texto = "O1", Valor = 1 }, new() { Texto = "O2", Valor = 2 } } } } };
        var act = () => sut.ActualizarAsync("token", 1, dto, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*p*salo primero a borrador*");
    }

    [Fact]
    public async Task Actualizar_HappyPath_RetornaMensaje()
    {
        SetupSesion();
        var cuest = MakeCuest(1, "Borrador", 5);
        _cuestionarios.Setup(c => c.FindDePsicologoAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(cuest);
        _cuestionarios.Setup(c => c.ActualizarAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<List<(PreguntaRow, List<OpcionPreguntaRow>)>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var dto = new CuestionarioCrearDto { Titulo = "  Nuevo  ", Descripcion = "D", Publicar = true, Preguntas = new List<PreguntaCrearDto> { new() { Texto = "P", Opciones = new List<OpcionCrearDto> { new() { Texto = "O1", Valor = 1 }, new() { Texto = "O2", Valor = 2 } } } } };
        var result = await sut.ActualizarAsync("token", 1, dto, "127.0.0.1", CancellationToken.None);

        result.Id.Should().Be(1);
        result.Mensaje.Should().Contain("actualizado");
    }

    [Fact]
    public async Task Eliminar_HappyPath_EliminaYLoguea()
    {
        SetupSesion();
        var cuest = MakeCuest(1, "Borrador", 5);
        _cuestionarios.Setup(c => c.FindDePsicologoAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(cuest);
        _cuestionarios.Setup(c => c.EliminarObservacionesDeCuestionarioAsync(1, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _cuestionarios.Setup(c => c.EliminarAsync(1, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.EliminarAsync("token", 1, true, "127.0.0.1", CancellationToken.None);

        result.Should().Contain("eliminado");
        _cuestionarios.Verify(c => c.EliminarObservacionesDeCuestionarioAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _cuestionarios.Verify(c => c.EliminarAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ObtenerPantallaAsignar_HappyPath_RetornaPantalla()
    {
        SetupSesion();
        var cuest = MakeCuest(1, "Publicado", 5);
        _cuestionarios.Setup(c => c.FindDePsicologoAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(cuest);
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>())).ReturnsAsync(8);
        _historial.Setup(h => h.GetPacientesAutorizadosAsync(5, 8, It.IsAny<CancellationToken>())).ReturnsAsync(new List<UsuarioDto>
        {
            new() { IdUsuario = 10, Nombre = "Ana", Apellido = "Paz" }
        });
        _cuestionarios.Setup(c => c.GetPacientesConAsignacionPendienteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<int> { 10 });

        var sut = CreateSut();
        var result = await sut.ObtenerPantallaAsignarAsync("token", 1, CancellationToken.None);

        result.Id.Should().Be(1);
        result.Pacientes.Should().HaveCount(1);
        result.Pacientes[0].YaAsignado.Should().BeTrue();
    }

    [Fact]
    public async Task Asignar_SinPacientes_LanzaInvalidOperation()
    {
        SetupSesion();
        var cuest = MakeCuest(1, "Publicado", 5);
        _cuestionarios.Setup(c => c.FindDePsicologoAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(cuest);

        var sut = CreateSut();
        var req = new AsignarCuestionarioRequest { PacientesIds = new List<int>() };
        var act = () => sut.AsignarAsync("token", 1, req, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*al menos un paciente*");
    }

    [Fact]
    public async Task Asignar_NoPublicado_LanzaInvalidOperation()
    {
        SetupSesion();
        var cuest = MakeCuest(1, "Borrador", 5);
        _cuestionarios.Setup(c => c.FindDePsicologoAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(cuest);

        var sut = CreateSut();
        var req = new AsignarCuestionarioRequest { PacientesIds = new List<int> { 10 } };
        var act = () => sut.AsignarAsync("token", 1, req, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*publicados*");
    }

    [Fact]
    public async Task Asignar_PacienteNoAutorizado_LanzaInvalidOperation()
    {
        SetupSesion();
        var cuest = MakeCuest(1, "Publicado", 5);
        _cuestionarios.Setup(c => c.FindDePsicologoAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(cuest);
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>())).ReturnsAsync(8);
        _historial.Setup(h => h.GetPacientesAutorizadosAsync(5, 8, It.IsAny<CancellationToken>())).ReturnsAsync(new List<UsuarioDto>());

        var sut = CreateSut();
        var req = new AsignarCuestionarioRequest { PacientesIds = new List<int> { 10 } };
        var act = () => sut.AsignarAsync("token", 1, req, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*autorizado*");
    }

    [Fact]
    public async Task Asignar_HappyPath_RetornaCantidadYLoguea()
    {
        SetupSesion();
        var cuest = MakeCuest(1, "Publicado", 5);
        _cuestionarios.Setup(c => c.FindDePsicologoAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(cuest);
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>())).ReturnsAsync(8);
        _historial.Setup(h => h.GetPacientesAutorizadosAsync(5, 8, It.IsAny<CancellationToken>())).ReturnsAsync(new List<UsuarioDto>
        {
            new() { IdUsuario = 10, Nombre = "Ana", Apellido = "Paz" }
        });
        _cuestionarios.Setup(c => c.AsignarAsync(1, 5, It.IsAny<List<int>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<int> { 100 });
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var req = new AsignarCuestionarioRequest { PacientesIds = new List<int> { 10 } };
        var result = await sut.AsignarAsync("token", 1, req, "127.0.0.1", CancellationToken.None);

        result.Should().Be(1);
    }
}
