using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using CalendarioApp.Services;
using CalendarioApp.ViewModels;

namespace CalendarioApp.Views;

/// <summary>Página con el formulario para modificar los datos de un profesor (solo DIRECTOR).</summary>
public partial class EditarProfesorView : UserControl
{
    private ProfesorDto? _profesor;

    /// <summary>Se lanza cuando la API ha aceptado los cambios.</summary>
    public event EventHandler? Guardado;

    /// <summary>Se lanza cuando el usuario cancela la edición.</summary>
    public event EventHandler? Cancelado;

    public EditarProfesorView()
    {
        InitializeComponent();
        CmbRol.ItemsSource = new[] { Roles.Usuario, Roles.Gestor, Roles.Director };
    }

    /// <summary>Rellena el formulario con una COPIA del profesor (el listado no cambia hasta guardar).</summary>
    public void Cargar(ProfesorDto profesor)
    {
        _profesor = profesor.Copiar();

        TxtSubtitulo.Text = $"{profesor.Nombre} {profesor.Apellidos}".Trim();
        TxtNombre.Text = profesor.Nombre ?? string.Empty;
        TxtApellidos.Text = profesor.Apellidos ?? string.Empty;
        TxtDni.Text = profesor.Dni ?? string.Empty;
        TxtEmail.Text = profesor.Email ?? string.Empty;
        TxtTelefono.Text = profesor.Telefono ?? string.Empty;
        TxtDireccion.Text = profesor.Direccion ?? string.Empty;
        TxtLocalidad.Text = profesor.Localidad ?? string.Empty;
        TxtCodigoPostal.Text = profesor.CodigoPostal?.ToString() ?? string.Empty;
        TxtCuerpo.Text = profesor.Cuerpo ?? string.Empty;
        TxtDepartamento.Text = profesor.Departamento ?? string.Empty;
        TxtEspecialidad.Text = profesor.Especialidad ?? string.Empty;
        TxtPassword.Clear();

        CmbRol.SelectedItem = (CmbRol.ItemsSource as string[])?
            .FirstOrDefault(r => string.Equals(r, profesor.Rol, StringComparison.OrdinalIgnoreCase));

        // Un director no puede quitarse a sí mismo el rol: perdería el acceso a esta página
        bool esUnoMismo = profesor.Id == SesionUsuario.Actual.Id;
        CmbRol.IsEnabled = !esUnoMismo;
        TxtAvisoRol.Visibility = esUnoMismo ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Borra los datos del formulario (la contraseña no debe quedar en memoria al cerrar sesión).</summary>
    public void Limpiar()
    {
        _profesor = null;
        TxtPassword.Clear();
    }

    private void EditarProfesorView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not true) return;

        Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => TxtNombre.Focus()));
    }

    private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        if (_profesor is null || DataContext is not MainViewModel vm) return;

        int? codigoPostal = null;
        var textoCp = TxtCodigoPostal.Text.Trim();
        if (textoCp.Length > 0)
        {
            if (!int.TryParse(textoCp, out var cp) || cp < 0)
            {
                vm.NotificarError("El código postal debe ser un número.");
                return;
            }
            codigoPostal = cp;
        }

        var datos = _profesor.Copiar();
        datos.Nombre = TxtNombre.Text.Trim();
        datos.Apellidos = TxtApellidos.Text.Trim();
        datos.Dni = TxtDni.Text.Trim();
        datos.Email = TxtEmail.Text.Trim();
        datos.Telefono = NullSiVacio(TxtTelefono.Text);
        datos.Direccion = NullSiVacio(TxtDireccion.Text);
        datos.Localidad = NullSiVacio(TxtLocalidad.Text);
        datos.CodigoPostal = codigoPostal;
        datos.Cuerpo = NullSiVacio(TxtCuerpo.Text);
        datos.Departamento = NullSiVacio(TxtDepartamento.Text);
        datos.Especialidad = NullSiVacio(TxtEspecialidad.Text);
        datos.Rol = CmbRol.SelectedItem as string ?? _profesor.Rol;

        if (await vm.ActualizarProfesorAsync(datos, TxtPassword.Password))
        {
            Limpiar();
            Guardado?.Invoke(this, EventArgs.Empty);
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        Limpiar();
        Cancelado?.Invoke(this, EventArgs.Empty);
    }

    private static string? NullSiVacio(string texto)
    {
        texto = texto.Trim();
        return texto.Length == 0 ? null : texto;
    }
}
