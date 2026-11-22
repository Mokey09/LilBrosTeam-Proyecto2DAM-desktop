using System.Net;

namespace CalendarioApp.Services;

/// <summary>Error al hablar con la API. El mensaje está pensado para enseñárselo al usuario.</summary>
public class ApiException : Exception
{
    public HttpStatusCode? CodigoHttp { get; }

    public ApiException(string mensaje, HttpStatusCode? codigoHttp = null, Exception? inner = null)
        : base(mensaje, inner)
    {
        CodigoHttp = codigoHttp;
    }
}

/// <summary>La API respondió 401: el token falta, no es válido o ha caducado.</summary>
public class SesionExpiradaException : ApiException
{
    public SesionExpiradaException()
        : base("Tu sesión ha caducado. Vuelve a iniciar sesión.", HttpStatusCode.Unauthorized) { }
}

/// <summary>El usuario está identificado pero su rol no permite la operación (403 o rol no permitido).</summary>
public class SinPermisosException : ApiException
{
    public SinPermisosException(string? mensaje = null)
        : base(mensaje ?? "No tienes permisos para realizar esta acción.", HttpStatusCode.Forbidden) { }
}
