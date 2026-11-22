using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using CalendarioApp.Models;
using CalendarioApp.Services;

namespace CalendarioApp.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly Dictionary<DateTime, DiaCalendario> _dias = new();
    private readonly ApiClient _api = ApiClient.Instancia;
    private DateTime? _fechaSeleccionada;
    private DiaCalendario _diaActual;
    private int _version;
    private string _estado = string.Empty;
    private bool _estadoEsError;
    private bool _ocupado;

    public MainViewModel()
    {
        _fechaSeleccionada = SiguienteDiaLaborable(DateTime.Today);
        _diaActual = ObtenerDia(_fechaSeleccionada.Value);
        _diaActual.PropertyChanged += DiaActual_PropertyChanged;
    }

    /// <summary>
    /// Se lanza cuando la sesión deja de ser válida (caducada, rechazada por el servidor o sin permisos).
    /// La ventana reacciona mostrando la página de login con el mensaje recibido.
    /// </summary>
    public event EventHandler<string>? SesionInvalida;

    public IReadOnlyList<TipoDia> TiposDia { get; } = Enum.GetValues<TipoDia>();

    /// <summary>Profesores mostrados en la página de profesores.</summary>
    public ObservableCollection<ProfesorDto> Profesores { get; } = new();

    /// <summary>Se incrementa cada vez que cambia un bloqueo, para refrescar el calendario.</summary>
    public int Version
    {
        get => _version;
        private set => Set(ref _version, value);
    }

    public DateTime? FechaSeleccionada
    {
        get => _fechaSeleccionada;
        set
        {
            if (value == null || EsFinDeSemana(value.Value))
            {
                // No se permite quedarse sin día ni seleccionar sábados/domingos
                OnPropertyChanged();
                return;
            }

            if (_fechaSeleccionada == value.Value.Date) return;
            _fechaSeleccionada = value.Value.Date;

            _diaActual.PropertyChanged -= DiaActual_PropertyChanged;
            DiaActual = ObtenerDia(_fechaSeleccionada.Value);
            _diaActual.PropertyChanged += DiaActual_PropertyChanged;

            MostrarOk(string.Empty);
            OnPropertyChanged();
            OnPropertyChanged(nameof(NombreDia));
            OnPropertyChanged(nameof(FechaTexto));
        }
    }

    public DiaCalendario DiaActual
    {
        get => _diaActual;
        private set => Set(ref _diaActual, value);
    }

    public string NombreDia
    {
        get
        {
            var nombre = CultureInfo.CurrentCulture.DateTimeFormat.GetDayName((_fechaSeleccionada ?? DateTime.Today).DayOfWeek);
            return char.ToUpper(nombre[0]) + nombre[1..];
        }
    }

    public string FechaTexto => (_fechaSeleccionada ?? DateTime.Today).ToString("d 'de' MMMM 'de' yyyy");

    public string Estado
    {
        get => _estado;
        private set => Set(ref _estado, value);
    }

    /// <summary>true si el mensaje de Estado es un error (se pinta en rojo).</summary>
    public bool EstadoEsError
    {
        get => _estadoEsError;
        private set => Set(ref _estadoEsError, value);
    }

    /// <summary>Mientras hay una operación contra la API en curso, los botones se desactivan.</summary>
    public bool NoOcupado => !_ocupado;

    public static bool EsFinDeSemana(DateTime fecha) =>
        fecha.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    private static DateTime SiguienteDiaLaborable(DateTime fecha)
    {
        fecha = fecha.Date;
        while (EsFinDeSemana(fecha)) fecha = fecha.AddDays(1);
        return fecha;
    }

    public bool EstaBloqueado(DateTime fecha) =>
        _dias.TryGetValue(fecha.Date, out var dia) && dia.Bloqueado;

    // =====================================================================
    //  ACCIONES (todas comprueban el token ANTES de tocar nada)
    // =====================================================================

    /// <summary>Comprueba la sesión (sin hacer nada más). Útil para acciones aún sin implementar.</summary>
    public Task<bool> VerificarSesionAsync() => EjecutarAsync(() => Task.FromResult(true));

    /// <summary>Descarga los bloqueos de la API. Se usa justo después del login (el token es recién emitido).</summary>
    public Task<bool> CargarBloqueosAsync() => EjecutarAsync(async () =>
    {
        await RecargarBloqueosAsync();
        MostrarOk("Calendario cargado");
        return true;
    }, verificarSesion: false);

    /// <summary>Descarga el listado de profesores (solo DIRECTOR).</summary>
    public Task<bool> CargarProfesoresAsync() => EjecutarAsync(async () =>
    {
        // La API solo permite /profesores al rol DIRECTOR: se evita la llamada si no lo es
        if (!Roles.EsDirector(SesionUsuario.Actual.Rol))
        {
            Profesores.Clear();
            MostrarError("Solo los directores pueden ver el listado de profesores.");
            return false;
        }

        var lista = await _api.ObtenerProfesoresAsync(Token);

        Profesores.Clear();
        foreach (var p in lista
                     .OrderBy(p => p.Apellidos, StringComparer.CurrentCultureIgnoreCase)
                     .ThenBy(p => p.Nombre, StringComparer.CurrentCultureIgnoreCase))
            Profesores.Add(p);

        MostrarOk(lista.Count == 0 ? "No hay profesores registrados" : $"{lista.Count} profesores");
        return true;
    });

    /// <summary>Vacía el listado de profesores (al cerrar sesión no deben quedar datos en pantalla).</summary>
    public void LimpiarProfesores() => Profesores.Clear();

    /// <summary>Bloquea todos los días entre dos fechas (ambas incluidas) en la API.</summary>
    public Task<bool> BloquearRangoAsync(DateTime inicio, DateTime fin, TipoDia? tipo, string descripcion) =>
        EjecutarAsync(async () =>
        {
            if (tipo is null)
            {
                MostrarError("Selecciona el tipo de día.");
                return false;
            }

            var creados = await _api.BloquearAsync(Token, inicio, fin, tipo.Value, descripcion);
            await RecargarBloqueosAsync();

            int nuevos = creados.Count(b => TryFecha(b.Fecha, out var f) && !EsFinDeSemana(f));
            if (nuevos > 0)
                MostrarOk($"{nuevos} días bloqueados");
            else
                MostrarOk("Esos días ya estaban bloqueados (o el rango solo contiene fines de semana)");
            return true;
        });

    /// <summary>Desbloquea los días entre dos fechas (ambas incluidas) en la API.</summary>
    public Task<bool> DesbloquearRangoAsync(DateTime inicio, DateTime fin) =>
        EjecutarAsync(async () =>
        {
            int antes = _dias.Values.Count(d =>
                d.Bloqueado && d.Fecha >= inicio.Date && d.Fecha <= fin.Date && !EsFinDeSemana(d.Fecha));

            await _api.DesbloquearAsync(Token, inicio, fin);
            await RecargarBloqueosAsync();

            MostrarOk(antes == 0 ? "No había días bloqueados en el rango" : $"{antes} días desbloqueados");
            return true;
        });

    /// <summary>Marca o desmarca el bloqueo del día seleccionado (casilla "Bloquear día").</summary>
    public Task<bool> CambiarBloqueoDiaAsync(bool bloquear)
    {
        var dia = DiaActual; // el usuario podría cambiar de día mientras se espera a la API

        return EjecutarAsync(async () =>
        {
            if (bloquear)
            {
                if (dia.Tipo is null)
                {
                    MostrarError("Selecciona un tipo de día antes de bloquear.");
                    return false;
                }
                await _api.BloquearAsync(Token, dia.Fecha, dia.Fecha, dia.Tipo.Value, dia.Descripcion);
            }
            else
            {
                await _api.DesbloquearAsync(Token, dia.Fecha, dia.Fecha);
            }

            dia.Bloqueado = bloquear;
            if (!bloquear) LimpiarCampos(dia); // el servidor ha borrado el bloqueo con su tipo y comentario
            Version++;
            MostrarOk(bloquear ? "Día bloqueado" : "Día desbloqueado");
            return true;
        });
    }

    /// <summary>
    /// Guarda tipo y descripción del día seleccionado. La API no tiene un endpoint de edición,
    /// así que el día se vuelve a crear (borrar + crear).
    /// </summary>
    public Task<bool> GuardarAsync()
    {
        var dia = DiaActual;

        return EjecutarAsync(async () =>
        {
            var texto = dia.Fecha.ToString("d 'de' MMMM 'de' yyyy");

            if (!dia.Bloqueado)
            {
                MostrarOk($"El {texto} no está bloqueado: solo se guardan los días bloqueados");
                return false;
            }
            if (dia.Tipo is null)
            {
                MostrarError("Selecciona un tipo de día antes de guardar.");
                return false;
            }

            await _api.DesbloquearAsync(Token, dia.Fecha, dia.Fecha);
            try
            {
                await _api.BloquearAsync(Token, dia.Fecha, dia.Fecha, dia.Tipo.Value, dia.Descripcion);
            }
            catch
            {
                // El día se borró pero no se pudo recrear: se vuelve a leer del servidor para que
                // la pantalla refleje lo que realmente hay guardado
                try { await RecargarBloqueosAsync(); } catch { /* se informa del error original */ }
                throw;
            }

            MostrarOk($"Datos del {texto} guardados");
            return true;
        });
    }

    // =====================================================================
    //  INFRAESTRUCTURA
    // =====================================================================

    /// <summary>Token de la sesión actual; si no hay, se trata como sesión caducada.</summary>
    private static string Token => SesionUsuario.Actual.Token ?? throw new SesionExpiradaException();

    /// <summary>
    /// Ejecuta una acción de forma segura: evita dos operaciones a la vez, comprueba el token
    /// ANTES de empezar y traduce los errores de la API a mensajes (o a volver al login).
    /// </summary>
    private async Task<bool> EjecutarAsync(Func<Task<bool>> accion, bool verificarSesion = true)
    {
        if (_ocupado) return false;
        MostrarOk(string.Empty); // un mensaje de una acción o página anterior no debe quedarse en pantalla
        SetOcupado(true);
        try
        {
            if (verificarSesion && !await ComprobarSesionAsync())
                return false;

            return await accion();
        }
        catch (Exception ex)
        {
            ManejarError(ex);
            return false;
        }
        finally
        {
            SetOcupado(false);
        }
    }

    private async Task<bool> ComprobarSesionAsync()
    {
        var resultado = await AuthService.Instancia.VerificarSesionAsync();
        if (resultado.EsValida) return true;

        if (resultado.RequiereLogin)
            SesionInvalida?.Invoke(this, resultado.Mensaje);
        else
            MostrarError(resultado.Mensaje);
        return false;
    }

    private void ManejarError(Exception ex)
    {
        switch (ex)
        {
            case SesionExpiradaException:
                // El servidor rechazó el token en mitad de la operación
                AuthService.Instancia.CerrarSesion();
                SesionInvalida?.Invoke(this, ex.Message);
                break;
            case ApiException api:
                MostrarError(api.Message);
                break;
            default:
                MostrarError("Error inesperado: " + ex.Message);
                break;
        }
    }

    /// <summary>Sustituye el estado de los días por el que hay en la API.</summary>
    private async Task RecargarBloqueosAsync()
    {
        var bloqueos = await _api.ObtenerBloqueosAsync(Token);

        // Se limpian todos los días bloqueados (estado, tipo y descripción) y después se vuelven a rellenar
        // con lo que hay en el servidor: así los días desbloqueados quedan con el formulario en blanco
        foreach (var dia in _dias.Values.Where(d => d.Bloqueado).ToList())
        {
            dia.Bloqueado = false;
            LimpiarCampos(dia);
        }

        foreach (var b in bloqueos)
        {
            if (!TryFecha(b.Fecha, out var fecha) || EsFinDeSemana(fecha)) continue;

            var dia = ObtenerDia(fecha);
            dia.Bloqueado = true;
            dia.Tipo = Enum.TryParse<TipoDia>(b.Tipo, true, out var tipo) ? tipo : null;
            dia.Descripcion = b.Comentario ?? string.Empty;
        }

        Version++; // refresca el calendario
    }

    private static void LimpiarCampos(DiaCalendario dia)
    {
        dia.Tipo = null;
        dia.Descripcion = string.Empty;
    }

    private static bool TryFecha(string texto, out DateTime fecha) =>
        DateTime.TryParseExact(texto, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha);

    private void MostrarOk(string mensaje)
    {
        EstadoEsError = false;
        Estado = mensaje;
    }

    private void MostrarError(string mensaje)
    {
        EstadoEsError = true;
        Estado = mensaje;
    }

    private void SetOcupado(bool valor)
    {
        _ocupado = valor;
        OnPropertyChanged(nameof(NoOcupado));
    }

    private DiaCalendario ObtenerDia(DateTime fecha)
    {
        fecha = fecha.Date;
        if (!_dias.TryGetValue(fecha, out var dia))
            _dias[fecha] = dia = new DiaCalendario(fecha);
        return dia;
    }

    private void DiaActual_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DiaCalendario.Bloqueado))
            Version++;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? nombre = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));

    private void Set<T>(ref T campo, T valor, [CallerMemberName] string? nombre = null)
    {
        if (EqualityComparer<T>.Default.Equals(campo, valor)) return;
        campo = valor;
        OnPropertyChanged(nombre);
    }
}
