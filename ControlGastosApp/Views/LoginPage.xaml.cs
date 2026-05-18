using ControlGastosApp.Services;
using Microsoft.Maui;

namespace ControlGastosApp.Views;

public partial class LoginPage : ContentPage
{
    // Instancia del servicio (esto es simplificado, luego usaremos inyección)
    DatabaseService _dbService = new DatabaseService();

    public LoginPage()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        string pin = PinEntry.Text;

        if (string.IsNullOrEmpty(pin))
        {
            await DisplayAlert("Error", "Por favor ingresa un PIN", "OK");
            return;
        }

        // Validamos contra la base de datos
        bool esValido = await _dbService.ValidarPinAsync(pin);

        if (esValido)
        {
            // Si es correcto, navegamos al Dashboard (MainPage)
            // Por ahora usaremos App.Current.MainPage para cambiar la raíz
            Application.Current.MainPage = new NavigationPage(new MainPage());
        }
        else
        {
            await DisplayAlert("Denegado", "PIN Incorrecto", "OK");
            PinEntry.Text = string.Empty;
        }
    }
}