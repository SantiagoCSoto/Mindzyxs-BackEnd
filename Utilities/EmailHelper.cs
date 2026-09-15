using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Mindzyxs_API.Utilities;

public class SmtpOptions
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string User { get; set; } = string.Empty;
    public string Pass { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
}

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}

public class SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var o = options.Value;
        if (string.IsNullOrWhiteSpace(o.User) || string.IsNullOrWhiteSpace(o.From))
        {
            // Dev: sin credenciales configuradas, no se envía pero se registra (HU4 permite reenviar).
            logger.LogWarning("SMTP sin configurar. Código para {To} no enviado por correo.", to);
            return;
        }

        using var client = new SmtpClient(o.Host, o.Port) { EnableSsl = o.EnableSsl };
        client.Credentials = new NetworkCredential(o.User, o.Pass);
        using var mail = new MailMessage(o.From, to, subject, htmlBody) { IsBodyHtml = true };
        await client.SendMailAsync(mail, ct);
    }
}
