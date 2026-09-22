namespace Mindzyxs_API.Utilities;

// Portado de proyectoMindzyxs/Mindzyxs/Utilities/EmailTemplate.cs (único código reutilizado).
public static class EmailTemplates
{
    public static string VerificationCode(string nombre, string codigo)
    {
        return $@"
        <div style='font-family:Arial;background:#f7f8fb;padding:30px;'>
          <div style='max-width:520px;margin:auto;background:#fff;border-radius:12px;padding:24px;border:1px solid #e5e7eb;'>
            <h2 style='margin:0;color:#111827;'>Mindzyxs</h2>
            <p style='color:#667085;'>Hola {nombre},</p>
            <p style='color:#111827;'>Tu código de verificación es:</p>
            <div style='font-size:28px;font-weight:bold;color:#111827;margin:12px 0;'>{codigo}</div>
            <p style='color:#667085;'>Este código expira en 15 minutos.</p>
          </div>
        </div>";
    }

    // PB-05 HU1-PB5 / HU2-PB5: código para recuperar contraseña.
    public static string RecoveryCode(string nombre, string codigo)
    {
        return $@"
        <div style='font-family:Arial;background:#f7f8fb;padding:30px;'>
          <div style='max-width:520px;margin:auto;background:#fff;border-radius:12px;padding:24px;border:1px solid #e5e7eb;'>
            <h2 style='margin:0;color:#111827;'>Mindzyxs</h2>
            <p style='color:#667085;'>Hola {nombre},</p>
            <p style='color:#111827;'>Recibimos una solicitud para recuperar tu contraseña. Tu código de recuperación es:</p>
            <div style='font-size:28px;font-weight:bold;color:#111827;margin:12px 0;'>{codigo}</div>
            <p style='color:#667085;'>Este código expira en 15 minutos. Si no lo solicitaste, ignora este mensaje.</p>
          </div>
        </div>";
    }
}
