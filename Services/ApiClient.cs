using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CalendarioApp.Models;

namespace CalendarioApp.Services;

// ---- DTOs (lo que devuelve la API) ----
public class LoginRespuesta
{
    public string Token { get; set; } = string.Empty;
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
}

public class PerfilDto
{
    public int Id { get; set; }
    public string? Nombre { get; set; }
    public string? Email { get; set; }
    public string Rol { get; set; } = string.Empty;
}

public class BloqueoDto
{
    public int Id { get; set; }
    public string Fecha { get; set; } = string.Empty;   // "yyyy-MM-dd"
    public string? Tipo { get; set; }                   // nombre del enum, p. ej. "VACACIONES"
    public string? Comentario { get; set; }
}

public class ProfesorDto
{
    public int Id { get; set; }
    public string? Nombre { get; set; }
    public string? Apellidos { get; set; }
    public string? Dni { get; set; }
    public string? Email { get; set; }
    public string? Rol { get; set; }
    public string? Departamento { get; set; }
    public string? Especialidad { get; set; }
    public string? Cuerpo { get; set; }
    public string? Telefono { get; set; }
    public string? Localidad { get; set; }
}

/// <summary>
/// Cliente HTTP de la API. Por defecto apunta a http://localhost:8080; se puede cambiar con
/// la variable de entorno CALENDARIO_API_URL. Cuando se publique la API, usar https.
/// </summary>
public sealed class ApiClient
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private const string FormatoFecha = "yyyy-MM-dd";

    public static ApiClient Instancia { get; } = new();

    private readonly HttpClient _http;

    private ApiClient()
    {
        var url = Environment.GetEnvironmentVariable("CALENDARIO_API_URL");
        if (string.IsNullOrWhiteSpace(url)) url = "http://localhost:8080";

        _http = new HttpClient
        {
            BaseAddress = new Uri(url.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    // ================= AUTENTICACIÓN =================

    /// <summary>POST /auth/login</summary>
    public async Task<LoginRespuesta> LoginAsync(string email, string contrasena)
    {
        using var resp = await EnviarAsync(HttpMethod.Post, "auth/login", new { email, contrasena });

        if (resp.StatusCode == HttpStatusCode.Unauthorized)
            throw new ApiException("Email o contraseña incorrectos.", resp.StatusCode);
        if (resp.StatusCode == HttpStatusCode.BadRequest)
            throw new ApiException("Introduce el email y la contraseña.", resp.StatusCode);

        ComprobarRespuesta(resp);
        return await LeerAsync<LoginRespuesta>(resp);
    }

    /// <summary>GET /auth/me: lanza SesionExpiradaException si el servidor no acepta el token.</summary>
    public async Task<PerfilDto> PerfilAsync(string token)
    {
        using var resp = await EnviarAsync(HttpMethod.Get, "auth/me", token: token);
        ComprobarRespuesta(resp);
        return await LeerAsync<PerfilDto>(resp);
    }

    // ================= PROFESORES =================

    /// <summary>GET /profesores (la API solo lo permite al rol DIRECTOR).</summary>
    public async Task<List<ProfesorDto>> ObtenerProfesoresAsync(string token)
    {
        using var resp = await EnviarAsync(HttpMethod.Get, "profesores", token: token);
        ComprobarRespuesta(resp);
        return await LeerAsync<List<ProfesorDto>>(resp);
    }

    // ================= CALENDARIO =================

    /// <summary>GET /bloqueados</summary>
    public async Task<List<BloqueoDto>> ObtenerBloqueosAsync(string token)
    {
        using var resp = await EnviarAsync(HttpMethod.Get, "bloqueados", token: token);
        ComprobarRespuesta(resp);
        return await LeerAsync<List<BloqueoDto>>(resp);
    }

    /// <summary>POST /bloqueados. Devuelve solo los días que se han creado (los ya bloqueados se respetan).</summary>
    public async Task<List<BloqueoDto>> BloquearAsync(
        string token, DateTime inicio, DateTime fin, TipoDia tipo, string? comentario)
    {
        var cuerpo = new
        {
            fechaInicio = Formatear(inicio),
            fechaFin = Formatear(fin),
            tipo = tipo.ToString(),
            comentario = string.IsNullOrWhiteSpace(comentario) ? null : comentario.Trim()
        };

        using var resp = await EnviarAsync(HttpMethod.Post, "bloqueados", cuerpo, token);
        ComprobarRespuesta(resp);
        return await LeerAsync<List<BloqueoDto>>(resp);
    }

    /// <summary>DELETE /bloqueados/desbloquear?desde=...&amp;hasta=...</summary>
    public async Task DesbloquearAsync(string token, DateTime desde, DateTime hasta)
    {
        var ruta = $"bloqueados/desbloquear?desde={Formatear(desde)}&hasta={Formatear(hasta)}";
        using var resp = await EnviarAsync(HttpMethod.Delete, ruta, token: token);
        ComprobarRespuesta(resp);
    }

    // ================= AUXILIARES =================

    private static string Formatear(DateTime fecha) => fecha.ToString(FormatoFecha, CultureInfo.InvariantCulture);

    private async Task<HttpResponseMessage> EnviarAsync(
        HttpMethod metodo, string ruta, object? cuerpo = null, string? token = null)
    {
        using var peticion = new HttpRequestMessage(metodo, ruta);

        if (token != null)
            peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (cuerpo != null)
            peticion.Content = JsonContent.Create(cuerpo, options: JsonOpts);

        try
        {
            return await _http.SendAsync(peticion);
        }
        catch (TaskCanceledException ex)
        {
            throw new ApiException("El servidor no responde (tiempo de espera agotado).", null, ex);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException("No se pudo conectar con el servidor. Comprueba que la API está en marcha.", null, ex);
        }
    }

    private static void ComprobarRespuesta(HttpResponseMessage resp)
    {
        if (resp.IsSuccessStatusCode) return;

        ApiException error = resp.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new SesionExpiradaException(),
            HttpStatusCode.Forbidden => new SinPermisosException(),
            HttpStatusCode.BadRequest => new ApiException(
                "La API rechazó los datos enviados. Revisa las fechas (el rango máximo es de 366 días).", resp.StatusCode),
            _ => new ApiException($"Error del servidor ({(int)resp.StatusCode}).", resp.StatusCode)
        };
        throw error;
    }

    private static async Task<T> LeerAsync<T>(HttpResponseMessage resp) where T : new()
    {
        try
        {
            return await resp.Content.ReadFromJsonAsync<T>(JsonOpts) ?? new T();
        }
        catch (JsonException ex)
        {
            throw new ApiException("La API devolvió una respuesta inesperada.", resp.StatusCode, ex);
        }
    }
}
