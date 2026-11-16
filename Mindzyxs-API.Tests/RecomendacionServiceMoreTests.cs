using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class RecomendacionServiceMoreTests
{
    private readonly Mock<IRecomendacionRepository> _recomendaciones = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();

    private RecomendacionService CreateSut() => new(
        _recomendaciones.Object, _sesiones.Object, _authLogs.Object);

    [Fact]
    public async Task ObtenerDetalle_NoEncontrado_LanzaInvalidOperation()
    {
        var sesion = new SesionDto(1, 1, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        _recomendaciones.Setup(r => r.FindByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((RecomendacionRow?)null);

        var sut = CreateSut();
        var act = () => sut.ObtenerDetalleAsync("token", 99, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no disponible*");
    }

    [Fact]
    public async Task ObtenerDetalle_Valido_RetornaDetalle()
    {
        var sesion = new SesionDto(1, 1, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var rec = new RecomendacionRow { IdRecomendacion = 1, Titulo = "Respiración", Contenido = "Contenido", Intro = "Intro", Pasos = "Paso 1\nPaso 2", Duracion = "5 min", Instrucciones = "Instrucciones", Icono = "icon", NivelRiesgo = "Bajo" };
        _recomendaciones.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(rec);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.ObtenerDetalleAsync("token", 1, "127.0.0.1", CancellationToken.None);

        result.Should().NotBeNull();
        result.Titulo.Should().Be("Respiración");
        result.Pasos.Should().HaveCount(2);
    }
}
