using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CalendarioApp.Models;

public class DiaCalendario : INotifyPropertyChanged
{
    private bool _bloqueado;
    private TipoDia? _tipo;
    private string _descripcion = string.Empty;

    public DiaCalendario(DateTime fecha) => Fecha = fecha.Date;

    public DateTime Fecha { get; }

    public bool Bloqueado
    {
        get => _bloqueado;
        set => Set(ref _bloqueado, value);
    }

    public TipoDia? Tipo
    {
        get => _tipo;
        set => Set(ref _tipo, value);
    }

    public string Descripcion
    {
        get => _descripcion;
        set => Set(ref _descripcion, value ?? string.Empty);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T campo, T valor, [CallerMemberName] string? nombre = null)
    {
        if (EqualityComparer<T>.Default.Equals(campo, valor)) return;
        campo = valor;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));
    }
}
