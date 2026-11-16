using FluentAssertions;
using Mindzyxs_API.Services;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class FirebaseServiceTests
{
    [Fact]
    public async Task WriteMetaAsync_SinDatabaseUrl_LanzaInvalidOperation()
    {
        var opts = Options.Create(new FirebaseOptions { ProjectId = "test", DatabaseUrl = "" });
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<FirebaseService>>();
        var httpFactory = new Mock<IHttpClientFactory>();
        var sut = new FirebaseService(opts, logger.Object, httpFactory.Object);

        var act = () => sut.WriteMetaAsync(1, 1, 2, "Aceptada", null, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*DatabaseUrl no configurada*");
    }

    [Fact]
    public async Task WriteMetaAsync_SinCredenciales_LanzaInvalidOperation()
    {
        var opts = Options.Create(new FirebaseOptions { ProjectId = "test", DatabaseUrl = "https://test.firebaseio.com", CredentialsPath = "" });
        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", null);
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<FirebaseService>>();
        var httpFactory = new Mock<IHttpClientFactory>();
        var sut = new FirebaseService(opts, logger.Object, httpFactory.Object);

        var act = () => sut.WriteMetaAsync(1, 1, 2, "Aceptada", null, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*CredentialsPath o GOOGLE_APPLICATION_CREDENTIALS no definida*");
    }

    [Fact]
    public async Task CreateCustomTokenAsync_SinDatabaseUrl_LanzaInvalidOperation()
    {
        var opts = Options.Create(new FirebaseOptions { ProjectId = "test", DatabaseUrl = "", CredentialsPath = "" });
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<FirebaseService>>();
        var httpFactory = new Mock<IHttpClientFactory>();
        var sut = new FirebaseService(opts, logger.Object, httpFactory.Object);

        var act = () => sut.CreateCustomTokenAsync("uid", "Paciente", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateCustomTokenAsync_SinCredenciales_LanzaInvalidOperation()
    {
        var opts = Options.Create(new FirebaseOptions { ProjectId = "test", DatabaseUrl = "https://test.firebaseio.com", CredentialsPath = "" });
        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", null);
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<FirebaseService>>();
        var httpFactory = new Mock<IHttpClientFactory>();
        var sut = new FirebaseService(opts, logger.Object, httpFactory.Object);

        var act = () => sut.CreateCustomTokenAsync("uid", "Paciente", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*CredentialsPath o GOOGLE_APPLICATION_CREDENTIALS no definida*");
    }
}
