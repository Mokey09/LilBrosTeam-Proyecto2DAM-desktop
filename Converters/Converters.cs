using System.Globalization;
using System.Windows;
using System.Windows.Data;
using CalendarioApp.ViewModels;

namespace CalendarioApp.Converters;

/// <summary>Devuelve la opacidad de un día del calendario: apagado si está bloqueado.</summary>
public class DiaBloqueadoOpacidadConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 3 && values[0] is DateTime fecha)
        {
            if (MainViewModel.EsFinDeSemana(fecha)) return 0.3;
            if (values[2] is MainViewModel vm && vm.EstaBloqueado(fecha)) return 0.3;
        }
        return 1.0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Muestra los valores del enum en minúsculas.</summary>
public class MinusculasConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value?.ToString()?.ToLowerInvariant() ?? string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Los sábados y domingos no se pueden seleccionar (IsEnabled = false).</summary>
public class DiaLectivoHabilitadoConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => !(value is DateTime fecha && MainViewModel.EsFinDeSemana(fecha));

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>true si el valor no es null (p. ej. hay un elemento seleccionado en una lista).</summary>
public class NoNuloConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is not null;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
