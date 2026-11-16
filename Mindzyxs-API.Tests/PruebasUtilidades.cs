using FluentAssertions;
using Mindzyxs_API.Utilities;
using Xunit;

namespace Mindzyxs_API.Tests.Utilities;

public class PasswordHelperTests
{
    [Fact]
    public void IsStrongEnough_NullOrEmpty_ReturnsFalse()
    {
        PasswordHelper.IsStrongEnough(null).Should().BeFalse();
        PasswordHelper.IsStrongEnough("").Should().BeFalse();
        PasswordHelper.IsStrongEnough("   ").Should().BeFalse();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123456")]
    [InlineData("abcdef")]
    [InlineData("abcde")]
    public void IsStrongEnough_WeakPasswords_ReturnsFalse(string password)
    {
        PasswordHelper.IsStrongEnough(password).Should().BeFalse();
    }

    [Theory]
    [InlineData("Abc12345")]
    [InlineData("Password1")]
    [InlineData("MyPass99")]
    public void IsStrongEnough_StrongPasswords_ReturnsTrue(string password)
    {
        PasswordHelper.IsStrongEnough(password).Should().BeTrue();
    }

    [Fact]
    public void Hash_ProducesVerifiableHash()
    {
        var hash = PasswordHelper.Hash("Abc12345");
        hash.Should().NotBeNullOrEmpty();
        PasswordHelper.Verify("Abc12345", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = PasswordHelper.Hash("Abc12345");
        PasswordHelper.Verify("WrongPass1", hash).Should().BeFalse();
    }

    [Fact]
    public void Verify_NullStored_ReturnsFalse()
    {
        PasswordHelper.Verify("Abc12345", null).Should().BeFalse();
    }
}

public class CodeHelperTests
{
    [Fact]
    public void NewCode6_ReturnsSixDigits()
    {
        var code = CodeHelper.NewCode6();
        code.Should().HaveLength(6);
        code.Should().MatchRegex(@"^\d{6}$");
    }

    [Fact]
    public void NewCode6_GeneratesDifferentCodes()
    {
        var codes = Enumerable.Range(0, 5).Select(_ => CodeHelper.NewCode6()).ToList();
        codes.Distinct().Count().Should().BeGreaterThan(1);
    }
}

public class ContactHelperTests
{
    [Theory]
    [InlineData("  test@test.com  ", "test@test.com")]
    [InlineData(null, "")]
    [InlineData("", "")]
    public void Normalize_TrimsAndHandlesNull(string? input, string expected)
    {
        ContactHelper.Normalize(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("test@test.com", true)]
    [InlineData("invalid", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("a@b.c", true)]
    public void IsEmail_ValidatesCorrectly(string? input, bool expected)
    {
        ContactHelper.IsEmail(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("Paciente", true)]
    [InlineData("Psicologo", true)]
    [InlineData("paciente", true)]
    [InlineData("admin", false)]
    [InlineData(null, false)]
    public void IsValidTipoUsuario_ValidatesCorrectly(string? input, bool expected)
    {
        ContactHelper.IsValidTipoUsuario(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("paciente", "Paciente")]
    [InlineData("PSICOLOGO", "Psicologo")]
    [InlineData("  Paciente  ", "Paciente")]
    public void CanonicalTipoUsuario_NormalizesCorrectly(string input, string expected)
    {
        ContactHelper.CanonicalTipoUsuario(input).Should().Be(expected);
    }
}

