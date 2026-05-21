using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace ControlGastosApp
{
    public partial class MainPage : ContentPage
    {
        // Referencia local a nuestro ViewModel
        private readonly ViewModels.MainViewModel _viewModel;

        public MainPage()
        {
            InitializeComponent();

            // Instanciamos el ViewModel y lo asignamos al contexto de la página
            _viewModel = new ViewModels.MainViewModel();
            this.BindingContext = _viewModel;
        }

        // Se ejecuta automáticamente cada vez que la pantalla vuelve a estar al frente
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_viewModel != null)
            {
                try
                {
                    // Forzamos una recarga limpia desde la base de datos de SQLite
                    await _viewModel.CargarDatosAsync();
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error en OnAppearing: {ex.Message}");
                }
            }
        }

        // El detector físico del toque que activamos en el XAML
        private async void OnCapitalTarjetaTapped(object sender, System.EventArgs e)
        {
            try
            {
                // 1. Desplegamos el cuadro de texto pidiendo el monto directamente sobre esta página
                string resultado = await DisplayPromptAsync(
                    title: "Actualizar Capital",
                    message: "Ingresa el nuevo monto disponible:",
                    accept: "Confirmar",
                    cancel: "Cancelar",
                    placeholder: "Ejemplo: 1500.00",
                    keyboard: Keyboard.Numeric); // Despliega el teclado numérico en tu OPPO

                // 2. Si el usuario ingresó un valor numérico válido...
                if (!string.IsNullOrEmpty(resultado) && double.TryParse(resultado, out double nuevoMonto))
                {
                    if (_viewModel != null)
                    {
                        // Regresamos al hilo principal para actualizar la pantalla de golpe
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            // Actualizamos la propiedad observable para cambiar el $0.00 en vivo
                            _viewModel.CapitalTotal = nuevoMonto;

                            // 3. Mandamos a guardar el dato en la base de datos en un hilo de fondo
                            await Task.Run(async () =>
                            {
                                // Invocamos el método que acabas de revisar en tu MainViewModel
                                await _viewModel.GuardarNuevoCapitalEnBD(nuevoMonto);
                            });
                        });
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en el prompt de capital: {ex.Message}");
            }
        }
    }
}