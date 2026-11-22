using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using CalendarioApp.Services;
using CalendarioApp.ViewModels;

namespace CalendarioApp;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;

        // Si en cualquier acción la sesión resulta no ser válida, se vuelve a la página de login
        _vm.SesionInvalida += (_, mensaje) => MostrarLogin(mensaje);

        // La aplicación arranca en la página de login: la API exige token para todo
        MostrarLogin();
    }

    // ================= NAVEGACIÓN ENTRE PÁGINAS =================

    private void MostrarLogin(string? aviso = null)
    {
        ZonaCalendario.Visibility = Visibility.Collapsed;
        PanelDia.Visibility = Visibility.Collapsed;
        VistaProfesores.Visibility = Visibility.Collapsed;
        _vm.LimpiarProfesores(); // sin sesión no debe quedar ningún dato en pantalla

        VistaLogin.Preparar(aviso);
        VistaLogin.Visibility = Visibility.Visible;

        ActualizarBarraLateral();
    }

    private void MostrarCalendario()
    {
        VistaLogin.Visibility = Visibility.Collapsed;
        VistaProfesores.Visibility = Visibility.Collapsed;
        ZonaCalendario.Visibility = Visibility.Visible;
        PanelDia.Visibility = Visibility.Visible;

        ActualizarBarraLateral();
    }

    private void MostrarProfesores()
    {
        VistaLogin.Visibility = Visibility.Collapsed;
        ZonaCalendario.Visibility = Visibility.Collapsed;
        PanelDia.Visibility = Visibility.Collapsed;
        VistaProfesores.Visibility = Visibility.Visible;

        ActualizarBarraLateral();
    }

    /// <summary>El botón de la barra lateral sirve para iniciar sesión o, si ya hay sesión, para cerrarla.</summary>
    private void ActualizarBarraLateral()
    {
        var sesion = SesionUsuario.Actual;
        BtnIniciarSesion.Content = sesion.HayToken ? "\uE7E8" : "\uE77B"; // apagar / persona
        BtnIniciarSesion.ToolTip = sesion.HayToken
            ? $"Cerrar sesión ({sesion.Nombre} · {sesion.Rol})"
            : "Iniciar sesión";

        // Calendario: solo con sesión. Profesores: solo DIRECTOR (la API no deja entrar a los gestores)
        BtnCalendario.Visibility = sesion.HayToken ? Visibility.Visible : Visibility.Collapsed;
        BtnProfesores.Visibility = sesion.HayToken && Roles.EsDirector(sesion.Rol)
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void VistaLogin_LoginCorrecto(object? sender, EventArgs e)
    {
        MostrarCalendario();
        await _vm.CargarBloqueosAsync();
    }

    // ================= BARRA LATERAL =================

    private void BtnIniciarSesion_Click(object sender, RoutedEventArgs e)
    {
        var sesion = SesionUsuario.Actual;

        if (!sesion.HayToken)
        {
            MostrarLogin();
            return;
        }

        var respuesta = MessageBox.Show(this,
            $"Has iniciado sesión como {sesion.Nombre} ({sesion.Rol}).\n\n¿Quieres cerrar la sesión?",
            "Cerrar sesión", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (respuesta == MessageBoxResult.Yes)
        {
            AuthService.Instancia.CerrarSesion();
            MostrarLogin();
        }
    }

    private async void BtnCalendario_Click(object sender, RoutedEventArgs e)
    {
        if (!await _vm.VerificarSesionAsync()) return;

        MostrarCalendario();
    }

    private async void BtnProfesores_Click(object sender, RoutedEventArgs e)
    {
        // Se muestra la página y se cargan los datos; CargarProfesoresAsync comprueba el token antes.
        // Si la sesión no es válida, el ViewModel avisa y se vuelve a la página de login.
        MostrarProfesores();
        await _vm.CargarProfesoresAsync();
    }

    private async void BtnModificarCalendario_Click(object sender, RoutedEventArgs e)
    {
        if (!await _vm.VerificarSesionAsync()) return;

        // TODO: navegar a la página de modificar calendario
    }

    // ================= ACCIONES (todas comprueban el token antes) =================

    private async void BtnImportar_Click(object sender, RoutedEventArgs e)
    {
        if (!await _vm.VerificarSesionAsync()) return;

        // TODO: importar calendario desde un XML (pendiente de implementar)
    }

    private async void BtnBloquearRango_Click(object sender, RoutedEventArgs e)
    {
        // Se comprueba antes de abrir el diálogo para no hacer rellenar un formulario con la sesión caducada
        // (el ViewModel vuelve a comprobarla al enviar, por si caduca mientras el diálogo está abierto)
        if (!await _vm.VerificarSesionAsync()) return;

        var dialogo = new RangoDiasWindow(_vm.FechaSeleccionada ?? DateTime.Today, desbloquear: false, _vm.TiposDia)
        {
            Owner = this
        };

        if (dialogo.ShowDialog() == true)
            await _vm.BloquearRangoAsync(dialogo.FechaInicio, dialogo.FechaFin, dialogo.Tipo, dialogo.Descripcion);
    }

    private async void BtnDesbloquearRango_Click(object sender, RoutedEventArgs e)
    {
        if (!await _vm.VerificarSesionAsync()) return;

        var dialogo = new RangoDiasWindow(_vm.FechaSeleccionada ?? DateTime.Today, desbloquear: true, _vm.TiposDia)
        {
            Owner = this
        };

        if (dialogo.ShowDialog() == true)
            await _vm.DesbloquearRangoAsync(dialogo.FechaInicio, dialogo.FechaFin);
    }

    private async void ChkBloquearDia_Click(object sender, RoutedEventArgs e)
    {
        var casilla = (CheckBox)sender;
        bool deseado = casilla.IsChecked == true;

        try
        {
            await _vm.CambiarBloqueoDiaAsync(deseado);
        }
        finally
        {
            // Si la API rechazó el cambio (o la sesión no era válida), la casilla vuelve al estado real del día
            casilla.SetCurrentValue(ToggleButton.IsCheckedProperty, _vm.DiaActual.Bloqueado);
        }
    }

    private async void BtnGuardar_Click(object sender, RoutedEventArgs e) => await _vm.GuardarAsync();
}
