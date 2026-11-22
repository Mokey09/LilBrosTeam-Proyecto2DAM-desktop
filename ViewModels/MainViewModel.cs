using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using CalendarioApp.Models;

namespace CalendarioApp.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly Dictionary<DateTime, DiaCalendario> _dias = new();
    private DateTime? _fechaSeleccionada;
    private DiaCalendario _diaActual;
    private int _version;
    private string _estado = string.Empty;

    public MainViewModel()
    {
        _fechaSeleccionada = SiguienteDiaLaborable(DateTime.Today);
        _diaActual = ObtenerDia(_fechaSeleccionada.Value);
        _diaActual.PropertyChanged += DiaActual_PropertyChanged;
    }

    public IReadOnlyList<TipoDia> TiposDia { get; } = Enum.GetValues<TipoDia>();

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

            Estado = string.Empty;
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
        set => Set(ref _estado, value);
    }

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

    /// <summary>
    /// Bloquea todos los días laborables entre dos fechas (ambas incluidas) y les asigna tipo y descripción.
    /// Devuelve cuántos días bloqueó.
    /// </summary>
    public int BloquearRango(DateTime inicio, DateTime fin, TipoDia? tipo, string descripcion)
    {
        int bloqueados = 0;
        for (var fecha = inicio.Date; fecha <= fin.Date; fecha = fecha.AddDays(1))
        {
            if (EsFinDeSemana(fecha)) continue;

            var dia = ObtenerDia(fecha);
            dia.Bloqueado = true;
            dia.Tipo = tipo;
            if (!string.IsNullOrWhiteSpace(descripcion))
                dia.Descripcion = descripcion;
            bloqueados++;
        }

        Version++; // refresca el calendario
        Estado = bloqueados == 0
            ? "El rango solo contiene fines de semana"
            : $"{bloqueados} días bloqueados";
        return bloqueados;
    }

    /// <summary>Desbloquea los días entre dos fechas (ambas incluidas). Devuelve cuántos desbloqueó.</summary>
    public int DesbloquearRango(DateTime inicio, DateTime fin)
    {
        int desbloqueados = 0;
        for (var fecha = inicio.Date; fecha <= fin.Date; fecha = fecha.AddDays(1))
        {
            if (!_dias.TryGetValue(fecha, out var dia) || !dia.Bloqueado) continue;
            dia.Bloqueado = false;
            desbloqueados++;
        }

        Version++; // refresca el calendario
        Estado = desbloqueados == 0
            ? "No había días bloqueados en el rango"
            : $"{desbloqueados} días desbloqueados";
        return desbloqueados;
    }

    public void Guardar()
    {
        // TODO: persistir los datos (base de datos / fichero) en el futuro
        Estado = $"Datos del {FechaTexto} guardados";
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
