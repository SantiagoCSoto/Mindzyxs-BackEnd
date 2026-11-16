using FluentAssertions;
using Mindzyxs_API.Services;
using Mindzyxs_API.Utilities;
using Xunit;

namespace Mindzyxs_API.Tests.Utilities;

public class TokenHelperTests
{
    [Fact]
    public void Generate_ReturnsUrlSafeString()
    {
        var token = TokenHelper.Generate();
        token.Should().NotBeNullOrEmpty();
        token.Should().NotContain("+");
        token.Should().NotContain("/");
        token.Should().NotContain("=");
        token.Length.Should().Be(43);
    }

    [Fact]
    public void Generate_ProducesDifferentTokens()
    {
        var t1 = TokenHelper.Generate();
        var t2 = TokenHelper.Generate();
        t1.Should().NotBe(t2);
    }

    [Fact]
    public void Hash_ProducesConsistentHash()
    {
        var h1 = TokenHelper.Hash("test-token");
        var h2 = TokenHelper.Hash("test-token");
        h1.Should().Be(h2);
        h1.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Hash_DifferentInput_ProducesDifferentHash()
    {
        TokenHelper.Hash("token-a").Should().NotBe(TokenHelper.Hash("token-b"));
    }
}

public class NotificationServiceTests
{
    [Fact]
    public void Sustituir_ReplacesVariables()
    {
        var plantilla = "Hola {nombre}, tu código es {codigo}";
        var vars = new Dictionary<string, string> { ["nombre"] = "Camila", ["codigo"] = "123456" };
        var result = NotificationService.Sustituir(plantilla, vars);
        result.Should().Be("Hola Camila, tu código es 123456");
    }

    [Fact]
    public void Sustituir_NullValue_ReplacesWithEmpty()
    {
        var plantilla = "Hola {nombre}";
        var vars = new Dictionary<string, string> { ["nombre"] = null! };
        var result = NotificationService.Sustituir(plantilla, vars);
        result.Should().Be("Hola ");
    }

    [Fact]
    public void Sustituir_NoVariables_ReturnsOriginal()
    {
        var plantilla = "Sin variables";
        var result = NotificationService.Sustituir(plantilla, new Dictionary<string, string>());
        result.Should().Be("Sin variables");
    }

    [Fact]
    public void Sustituir_MultipleOccurrences()
    {
        var plantilla = "{x} y {x}";
        var vars = new Dictionary<string, string> { ["x"] = "val" };
        var result = NotificationService.Sustituir(plantilla, vars);
        result.Should().Be("val y val");
    }
}

public class EmailTemplateTests
{
    [Fact]
    public void VerificationCode_ContainsNombreAndCodigo()
    {
        var html = EmailTemplates.VerificationCode("Camila", "123456");
        html.Should().Contain("Camila");
        html.Should().Contain("123456");
        html.Should().Contain("Mindzyxs");
    }

    [Fact]
    public void RecoveryCode_ContainsNombreAndCodigo()
    {
        var html = EmailTemplates.RecoveryCode("Juan", "654321");
        html.Should().Contain("Juan");
        html.Should().Contain("654321");
        html.Should().Contain("recuperar");
    }
}

