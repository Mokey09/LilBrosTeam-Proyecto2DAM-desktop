using System.Text;
using System.Text.Json;

namespace CalendarioApp.Services;

public static class Roles
{
    public const string Usuario = "USUARIO";
    public const string Gestor = "GESTOR";
    public const string Director = "DIRECTOR";

    public static bool EsDirector(string? rol) =>
        string.Equals(rol, Director, StringComparison.OrdinalIgnoreCase);

    /// <summary>Solo los gestores y los directores pueden usar esta aplicación.</summary>
    public static bool PuedeGestionarCalendario(string? rol) =>
        string.Equals(rol, Gestor, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(rol, Director, StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Sesión del usuario que ha iniciado sesión. Vive SOLO en memoria: el token no se escribe
/// en disco, así que al cerrar la aplicación hay que volver a iniciar sesión.
/// </summary>
public sealed class SesionUsuario
{
    public static SesionUsuario Actual { get; } = new();

    private SesionUsuario() { }

    public string? Token { get; private set; }
    public int Id { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Rol { get; private set; } = string.Empty;

    /// <summary>Instante de caducidad leído del propio token (claim "exp"), si lo trae.</summary>
    public DateTimeOffset? CaducaEn { get; private set; }

    public bool HayToken => !string.IsNullOrEmpty(Token);

    /// <summary>Margen de 10 s para no enviar un token que caduca justo en ese momento.</summary>
    public bool TokenCaducado =>
        CaducaEn is { } caduca && DateTimeOffset.UtcNow >= caduca.AddSeconds(-10);

    public void Iniciar(LoginRespuesta respuesta, string email)
    {
        Token = respuesta.Token;
        Id = respuesta.Id;
        Nombre = respuesta.Nombre;
        Rol = respuesta.Rol;
        Email = email;
        CaducaEn = LeerCaducidad(respuesta.Token);
    }

    public void ActualizarRol(string rol) => Rol = rol;

    public void Cerrar()
    {
        Token = null;
        Id = 0;
        Nombre = Email = Rol = string.Empty;
        CaducaEn = null;
    }

    /// <summary>Lee el claim "exp" del JWT (sin validar la firma: eso solo puede hacerlo el servidor).</summary>
    private static DateTimeOffset? LeerCaducidad(string token)
    {
        try
        {
            var partes = token.Split('.');
            if (partes.Length != 3) return null;

            var base64 = partes[1].Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');

            using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(base64)));
            if (doc.RootElement.TryGetProperty("exp", out var exp) && exp.TryGetInt64(out var segundos))
                return DateTimeOffset.FromUnixTimeSeconds(segundos);
        }
        catch
        {
            // Si no se puede leer, se confía en la comprobación con el servidor
        }
        return null;
    }
}
