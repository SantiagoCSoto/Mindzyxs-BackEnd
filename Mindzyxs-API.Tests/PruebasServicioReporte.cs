using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class PruebasServicioReporte
{
    private readonly Mock<IHistorialRepository> _historial = new();
    private readonly Mock<IUserRepository> _usuarios = new();
    private readonly Mock<ISesionRepository> _sesiones = new();

    private ReporteService CreateSut() => new(_historial.Object, _usuarios.Object, _sesiones.Object);

    [Fact]
    public async Task GenerarReporte_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var act = () => sut.GenerarReportePacienteAsync(null, 1, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GenerarReporte_ConDatos_GeneraPdfNoVacio()
    {
        var sesion = new SesionDto(1, 5, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var user = new UsuarioDto { IdUsuario = 5, IdRol = 2, IdEstadoCuenta = 1 };
        _usuarios.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _usuarios.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Psicologo");
        _usuarios.Setup(u => u.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(
            new UsuarioDto { IdUsuario = 1, Nombre = "Camila", Apellido = "Rojas", Correo = "camila@test.com", FechaNacimiento = new DateTime(2002, 5, 10) });
        _historial.Setup(h => h.GetEventosAsync(1, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(new List<EventoRow>
        {
            new() { IdEvento = 1, IdPaciente = 1, IdTipoEvento = 1, PuntajeTotal = 4, FechaEvento = DateTime.UtcNow, IdNivelRiesgo = 1, Nota = "Me sentí bien" },
            new() { IdEvento = 2, IdPaciente = 1, IdTipoEvento = 1, PuntajeTotal = 2, FechaEvento = DateTime.UtcNow.AddDays(-1), IdNivelRiesgo = 2, Nota = null },
        });
        _historial.Setup(h => h.GetNivelesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(
            new Dictionary<int, string> { [1] = "Bajo", [2] = "Medio" });

        var sut = CreateSut();
        var pdf = await sut.GenerarReportePacienteAsync("token", 1, CancellationToken.None);

        pdf.Should().NotBeNull();
        pdf.Length.Should().BeGreaterThan(100);
        // Verificar que es un PDF válido (empieza con %PDF)
        System.Text.Encoding.ASCII.GetString(pdf, 0, 4).Should().Be("%PDF");
    }

    [Fact]
    public async Task GenerarReporte_SinEventos_GeneraPdfValido()
    {
        var sesion = new SesionDto(1, 5, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var user = new UsuarioDto { IdUsuario = 5, IdRol = 2, IdEstadoCuenta = 1 };
        _usuarios.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _usuarios.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Psicologo");
        _usuarios.Setup(u => u.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(
            new UsuarioDto { IdUsuario = 1, Nombre = "Juan", Apellido = "Perez", Correo = "juan@test.com" });
        _historial.Setup(h => h.GetEventosAsync(1, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(new List<EventoRow>());
        _historial.Setup(h => h.GetNivelesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<int, string>());

        var sut = CreateSut();
        var pdf = await sut.GenerarReportePacienteAsync("token", 1, CancellationToken.None);

        pdf.Should().NotBeNull();
        pdf.Length.Should().BeGreaterThan(100);
        System.Text.Encoding.ASCII.GetString(pdf, 0, 4).Should().Be("%PDF");
    }
}

