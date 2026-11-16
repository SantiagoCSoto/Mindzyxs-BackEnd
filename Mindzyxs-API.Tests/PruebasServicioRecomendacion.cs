using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class PruebasServicioRecomendacion
{
    private readonly Mock<IRecomendacionRepository> _recomendaciones = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();

    private RecomendacionService CreateSut() => new(
        _recomendaciones.Object, _sesiones.Object, _authLogs.Object);

    [Fact]
    public async Task ObtenerLista_Anonimo_SinRecomendaciones_RetornaNoDisponible()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));
        _recomendaciones.Setup(r => r.GetPriorizadasAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecomendacionRow>());
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.ObtenerListaAsync(null, null, "127.0.0.1", CancellationToken.None);

        result.Disponible.Should().BeFalse();
    }

    [Fact]
    public async Task ObtenerLista_SinRecomendaciones_RetornaNoDisponible()
    {
        var sesion = new SesionDto(1, 1, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        _recomendaciones.Setup(r => r.GetPriorizadasAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecomendacionRow>());
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.ObtenerListaAsync("token", null, "127.0.0.1", CancellationToken.None);

        result.Disponible.Should().BeFalse();
        result.Recomendaciones.Should().BeEmpty();
    }

    [Fact]
    public async Task ObtenerLista_ConRecomendaciones_RetornaLista()
    {
        var sesion = new SesionDto(1, 1, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var recs = new List<RecomendacionRow>
        {
            new() { IdRecomendacion = 1, Titulo = "Respiración", Contenido = "Ejercicio de respiración", NivelRiesgo = "Bajo" }
        };
        _recomendaciones.Setup(r => r.GetPriorizadasAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(recs);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.ObtenerListaAsync("token", "Bajo", "127.0.0.1", CancellationToken.None);

        result.Disponible.Should().BeTrue();
        result.Recomendaciones.Should().HaveCount(1);
        result.Recomendaciones[0].Titulo.Should().Be("Respiración");
    }

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
}

