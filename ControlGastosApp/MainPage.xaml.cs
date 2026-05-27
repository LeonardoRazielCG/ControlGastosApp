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

        // 2. Refresco forzado al volver a la pantalla principal
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_viewModel != null)
            {
                try
                {
                    // 1. Esperamos a que SQLite termine de cargar los datos en memoria
                    await _viewModel.CargarDatosAsync();

                    // 2. Forzamos el refresco visual en el hilo de la interfaz de usuario
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        var fuenteOriginal = carouselViews.ItemsSource;
                        carouselViews.ItemsSource = null;
                        carouselViews.ItemsSource = fuenteOriginal;
                    });
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

        // 1. Detección infalible de pestaña al pulsar el botón flotante
        private async void OnAgregarMovimientoClicked(object sender, System.EventArgs e)
        {
            try
            {
                // Obtenemos la sección actual del carrusel
                var seccionActual = carouselViews.CurrentItem as ControlGastosApp.ViewModels.SeccionMovimientos;
                string nombrePestaña = seccionActual?.Titulo ?? "Gastos";

                await Navigation.PushModalAsync(new NuevoMovimientoPage(nombrePestaña));
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }
    }
}