using FluentAssertions;
using Mindzyxs_API.Services;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class PruebasServicioCuestionario
{
    [Fact]
    public void CalcularPuntaje_Empty_ReturnsZeroBajo()
    {
        var (puntaje, nivel) = CuestionarioService.CalcularPuntaje(Array.Empty<int>());
        puntaje.Should().Be(0);
        nivel.Should().Be("Bajo");
    }

    [Theory]
    [InlineData(new[] { 1, 1, 1 }, 1, "Bajo")]
    [InlineData(new[] { 2, 2, 2 }, 2, "Bajo")]
    [InlineData(new[] { 3, 3, 3 }, 3, "Medio")]
    [InlineData(new[] { 4, 4, 4 }, 4, "Alto")]
    [InlineData(new[] { 5, 5, 5 }, 5, "Alto")]
    public void CalcularPuntaje_Promedio_CalculaNivel(int[] valores, int esperado, string nivelEsperado)
    {
        var (puntaje, nivel) = CuestionarioService.CalcularPuntaje(valores);
        puntaje.Should().Be(esperado);
        nivel.Should().Be(nivelEsperado);
    }

    [Fact]
    public void CalcularPuntaje_Suma_ConUmbrales()
    {
        var (puntaje, nivel) = CuestionarioService.CalcularPuntaje(
            new[] { 2, 3, 4 }, "suma", 5, 9);
        puntaje.Should().Be(9);
        nivel.Should().Be("Medio");
    }

    [Fact]
    public void CalcularPuntaje_Suma_Alto()
    {
        var (puntaje, nivel) = CuestionarioService.CalcularPuntaje(
            new[] { 4, 4, 4 }, "suma", 5, 9);
        puntaje.Should().Be(12);
        nivel.Should().Be("Alto");
    }

    [Fact]
    public void CalcularPuntaje_Suma_Bajo()
    {
        var (puntaje, nivel) = CuestionarioService.CalcularPuntaje(
            new[] { 1, 1, 1 }, "suma", 5, 9);
        puntaje.Should().Be(3);
        nivel.Should().Be("Bajo");
    }

    [Fact]
    public void CalcularPuntaje_SinUmbrales_UsaPromedio()
    {
        var (puntaje, nivel) = CuestionarioService.CalcularPuntaje(
            new[] { 2, 3, 4 }, "suma", null, 9);
        // Sin umbralBajoMax, usa promedio
        puntaje.Should().Be(3);
        nivel.Should().Be("Medio");
    }

    [Fact]
    public void CalcularPuntaje_ModoNoSuma_UsaPromedio()
    {
        var (puntaje, nivel) = CuestionarioService.CalcularPuntaje(
            new[] { 2, 3, 4 }, "promedio", 5, 9);
        puntaje.Should().Be(3);
        nivel.Should().Be("Medio");
    }

    [Fact]
    public void CalcularPuntaje_SumaVacio_RetornaCeroBajo()
    {
        var (puntaje, nivel) = CuestionarioService.CalcularPuntaje(
            Array.Empty<int>(), "suma", 5, 9);
        puntaje.Should().Be(0);
        nivel.Should().Be("Bajo");
    }
}

