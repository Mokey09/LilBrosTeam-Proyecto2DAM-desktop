using System.Windows;
using CalendarioApp.Models;

namespace CalendarioApp;

public partial class RangoDiasWindow : Window
{
    private readonly bool _desbloquear;

    public RangoDiasWindow(DateTime fechaInicial, bool desbloquear, IReadOnlyList<TipoDia> tipos)
    {
        InitializeComponent();
        _desbloquear = desbloquear;

        DpInicio.SelectedDate = fechaInicial;
        DpInicio.DisplayDate = fechaInicial;
        DpFin.DisplayDate = fechaInicial;

        CbTipo.ItemsSource = tipos;
        CbTipo.SelectedItem = TipoDia.VACACIONES;

        if (desbloquear)
        {
            Title = "Desbloquear rango de días";
            TxtTitulo.Text = "Desbloquear rango de días";
            TxtSubtitulo.Text = "Se desbloquearán todos los días del rango. Los sábados y domingos se ignoran.";
            BtnAceptar.Content = "Desbloquear";
            PanelDatos.Visibility = Visibility.Collapsed;
        }
        else
        {
            Title = "Bloquear rango de días";
            TxtTitulo.Text = "Bloquear rango de días";
            TxtSubtitulo.Text = "Por ejemplo, las vacaciones de Navidad. Los sábados y domingos se ignoran.";
            BtnAceptar.Content = "Bloquear";
        }
    }

    public DateTime FechaInicio { get; private set; }
    public DateTime FechaFin { get; private set; }
    public TipoDia? Tipo { get; private set; }
    public string Descripcion { get; private set; } = string.Empty;

    private void BtnAceptar_Click(object sender, RoutedEventArgs e)
    {
        if (DpInicio.SelectedDate is not DateTime inicio || DpFin.SelectedDate is not DateTime fin)
        {
            MostrarError("Debes indicar el día de inicio y el día final.");
            return;
        }

        if (fin < inicio)
        {
            MostrarError("El día final no puede ser anterior al día de inicio.");
            return;
        }

        if (!_desbloquear)
        {
            if (CbTipo.SelectedItem is not TipoDia tipo)
            {
                MostrarError("Selecciona el tipo de día.");
                return;
            }

            Tipo = tipo;
            Descripcion = TxtDescripcion.Text.Trim();
        }

        FechaInicio = inicio.Date;
        FechaFin = fin.Date;
        DialogResult = true;
    }

    private void MostrarError(string mensaje)
    {
        TxtError.Text = mensaje;
        TxtError.Visibility = Visibility.Visible;
    }
}
