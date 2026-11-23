using System.Windows;
using System.Windows.Controls;
using CalendarioApp.Services;
using CalendarioApp.ViewModels;

namespace CalendarioApp.Views;

public partial class ProfesoresView : UserControl
{
    /// <summary>Se lanza al pulsar "Modificar" con un profesor seleccionado.</summary>
    public event EventHandler<ProfesorDto>? ModificarSolicitado;

    public ProfesoresView()
    {
        InitializeComponent();
    }

    private async void BtnActualizar_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            await vm.CargarProfesoresAsync();
    }

    private void BtnModificar_Click(object sender, RoutedEventArgs e) => SolicitarModificacion();

    private void GridProfesores_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Doble clic sobre una fila (no sobre la cabecera ni sobre una zona vacía)
        if (e.OriginalSource is DependencyObject origen &&
            ItemsControl.ContainerFromElement(GridProfesores, origen) is DataGridRow)
            SolicitarModificacion();
    }

    private void SolicitarModificacion()
    {
        if (GridProfesores.SelectedItem is ProfesorDto profesor)
            ModificarSolicitado?.Invoke(this, profesor);
    }
}
