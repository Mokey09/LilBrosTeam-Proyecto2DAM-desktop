using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace CalendarioApp;

public partial class App : Application
{
    public App()
    {
        // Hace que el calendario y los textos de fecha salgan en español
        var cultura = new CultureInfo("es-ES");
        CultureInfo.DefaultThreadCurrentCulture = cultura;
        CultureInfo.DefaultThreadCurrentUICulture = cultura;
        Thread.CurrentThread.CurrentCulture = cultura;
        Thread.CurrentThread.CurrentUICulture = cultura;

        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(cultura.IetfLanguageTag)));
    }
}
