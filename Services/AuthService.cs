namespace CalendarioApp.Services;

public record ResultadoVerificacion(bool EsValida, bool RequiereLogin, string Mensaje)
{
    public static ResultadoVerificacion Ok { get; } = new(true, false, string.Empty);
}

/// <summary>Inicio de sesión y comprobación del token antes de cada acción.</summary>
public sealed class AuthService
{
    public static AuthService Instancia { get; } = new();

    private readonly ApiClient _api = ApiClient.Instancia;

    private AuthService() { }

    /// <summary>
    /// Inicia sesión. Solo se admiten GESTOR y DIRECTOR: si el usuario es de otro rol,
    /// el token recibido se descarta y no se guarda nada.
    /// </summary>
    public async Task LoginAsync(string email, string contrasena)
    {
        var respuesta = await _api.LoginAsync(email, contrasena);

        if (string.IsNullOrWhiteSpace(respuesta.Token))
            throw new ApiException("La API no devolvió un token válido.");

        if (!Roles.PuedeGestionarCalendario(respuesta.Rol))
            throw new SinPermisosException(
                "Tu usuario no tiene permisos para usar esta aplicación. Solo pueden acceder gestores y directores.");

        SesionUsuario.Actual.Iniciar(respuesta, email);
    }

    public void CerrarSesion() => SesionUsuario.Actual.Cerrar();

    /// <summary>
    /// Se llama ANTES de cada acción. Comprueba, por este orden:
    ///   1. que hay token,
    ///   2. que no ha caducado (según la fecha que lleva dentro),
    ///   3. que el servidor lo sigue aceptando (GET /auth/me), y
    ///   4. que el usuario sigue siendo GESTOR o DIRECTOR (el rol pudo cambiar tras el login).
    /// Si la sesión no es válida, la cierra y pide volver a iniciar sesión.
    /// Si el servidor no responde, la acción se cancela pero la sesión se conserva.
    /// </summary>
    public async Task<ResultadoVerificacion> VerificarSesionAsync()
    {
        var sesion = SesionUsuario.Actual;

        if (!sesion.HayToken)
            return new(false, true, "Inicia sesión para continuar.");

        if (sesion.TokenCaducado)
        {
            sesion.Cerrar();
            return new(false, true, "Tu sesión ha caducado. Vuelve a iniciar sesión.");
        }

        try
        {
            var perfil = await _api.PerfilAsync(sesion.Token!);

            if (!Roles.PuedeGestionarCalendario(perfil.Rol))
            {
                sesion.Cerrar();
                return new(false, true, "Tu usuario ya no tiene permisos de gestor o director.");
            }

            sesion.ActualizarRol(perfil.Rol);
            return ResultadoVerificacion.Ok;
        }
        catch (SesionExpiradaException)
        {
            sesion.Cerrar();
            return new(false, true, "Tu sesión ya no es válida. Vuelve a iniciar sesión.");
        }
        catch (ApiException ex)
        {
            return new(false, false, ex.Message);
        }
    }
}
