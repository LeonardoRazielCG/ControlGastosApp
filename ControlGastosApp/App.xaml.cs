namespace ControlGastosApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Establecemos LoginPage como la primera pantalla
        MainPage = new Views.LoginPage();
    }
}