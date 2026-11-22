using System.Windows;
using System.Windows.Controls;
using CalendarioApp.ViewModels;

namespace CalendarioApp.Views;

public partial class ProfesoresView : UserControl
{
    public ProfesoresView()
    {
        InitializeComponent();
    }

    private async void BtnActualizar_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            await vm.CargarProfesoresAsync();
    }
}
