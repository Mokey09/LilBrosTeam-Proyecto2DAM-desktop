using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using CalendarioApp.Services;

namespace CalendarioApp.Views;

public partial class LoginView : UserControl
{
    /// <summary>Se lanza cuando el login ha sido correcto y la sesión ya está guardada.</summary>
    public event EventHandler? LoginCorrecto;

    public LoginView()
    {
        InitializeComponent();
    }

    /// <summary>Deja el formulario listo para un nuevo intento, con un aviso opcional.</summary>
    public void Preparar(string? aviso = null)
    {
        TxtPassword.Clear();
        TxtError.Visibility = Visibility.Collapsed;

        PanelAviso.Visibility = string.IsNullOrWhiteSpace(aviso) ? Visibility.Collapsed : Visibility.Visible;
        TxtAviso.Text = aviso ?? string.Empty;
    }

    private void LoginView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not true) return;

        // Foco en el primer campo vacío cuando aparece la página
        Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
        {
            if (string.IsNullOrEmpty(TxtEmail.Text)) TxtEmail.Focus();
            else TxtPassword.Focus();
        }));
    }

    private async void BtnEntrar_Click(object sender, RoutedEventArgs e) => await IntentarLoginAsync();

    private async void Campo_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && BtnEntrar.IsEnabled)
            await IntentarLoginAsync();
    }

    private async Task IntentarLoginAsync()
    {
        var email = TxtEmail.Text.Trim();
        var contrasena = TxtPassword.Password;

        if (email.Length == 0 || contrasena.Length == 0)
        {
            MostrarError("Introduce el email y la contraseña.");
            return;
        }

        Ocupado(true);
        try
        {
            await AuthService.Instancia.LoginAsync(email, contrasena);

            TxtPassword.Clear();
            TxtError.Visibility = Visibility.Collapsed;
            LoginCorrecto?.Invoke(this, EventArgs.Empty);
        }
        catch (ApiException ex)
        {
            TxtPassword.Clear();
            MostrarError(ex.Message);
        }
        catch (Exception ex)
        {
            TxtPassword.Clear();
            MostrarError("Error inesperado al iniciar sesión: " + ex.Message);
        }
        finally
        {
            Ocupado(false);
            if (IsVisible) TxtPassword.Focus(); // tras un error, listo para reintentar
        }
    }

    private void MostrarError(string mensaje)
    {
        PanelAviso.Visibility = Visibility.Collapsed;
        TxtError.Text = mensaje;
        TxtError.Visibility = Visibility.Visible;
    }

    private void Ocupado(bool ocupado)
    {
        TxtEmail.IsEnabled = !ocupado;
        TxtPassword.IsEnabled = !ocupado;
        BtnEntrar.IsEnabled = !ocupado;
        BtnEntrar.Content = ocupado ? "Entrando…" : "Entrar";
    }
}
