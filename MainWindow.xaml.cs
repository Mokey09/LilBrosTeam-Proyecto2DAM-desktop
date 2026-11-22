using System.Windows;
using CalendarioApp.ViewModels;

namespace CalendarioApp;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;
    }

    private void BtnIniciarSesion_Click(object sender, RoutedEventArgs e)
    {
        // TODO: navegar a la página de iniciar sesión
    }

    private void BtnModificarCalendario_Click(object sender, RoutedEventArgs e)
    {
        // TODO: navegar a la página de modificar calendario
    }

    private void BtnImportar_Click(object sender, RoutedEventArgs e)
    {
        // TODO: importar calendario desde un XML (pendiente de implementar)
    }

    private void BtnBloquearRango_Click(object sender, RoutedEventArgs e)
    {
        var dialogo = new RangoDiasWindow(_vm.FechaSeleccionada ?? DateTime.Today, desbloquear: false, _vm.TiposDia)
        {
            Owner = this
        };

        if (dialogo.ShowDialog() == true)
            _vm.BloquearRango(dialogo.FechaInicio, dialogo.FechaFin, dialogo.Tipo, dialogo.Descripcion);
    }

    private void BtnDesbloquearRango_Click(object sender, RoutedEventArgs e)
    {
        var dialogo = new RangoDiasWindow(_vm.FechaSeleccionada ?? DateTime.Today, desbloquear: true, _vm.TiposDia)
        {
            Owner = this
        };

        if (dialogo.ShowDialog() == true)
            _vm.DesbloquearRango(dialogo.FechaInicio, dialogo.FechaFin);
    }

    private void BtnGuardar_Click(object sender, RoutedEventArgs e) => _vm.Guardar();
}
