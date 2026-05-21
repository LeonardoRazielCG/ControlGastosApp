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

        private async void OnAgregarMovimientoClicked(object sender, System.EventArgs e)
        {
            try
            {
                // 1. Detectamos qué texto tiene la pestaña actual del carrusel ("Gastos" o "Ingresos")
                // carouselViews es el x:Name que le pusimos al Carrusel en el XAML
                string pestañaActual = carouselViews.CurrentItem as string;

                // 2. Si por alguna razón está vacío, por defecto asumimos que son "Gastos"
                if (string.IsNullOrEmpty(pestañaActual))
                {
                    pestañaActual = "Gastos";
                }

                // 3. Abrimos la nueva página que creaste pasándole como dato de viaje la pestaña actual
                // Usamos PushModalAsync para que aparezca deslizándose de abajo hacia arriba de forma muy elegante
                await Navigation.PushModalAsync(new NuevoMovimientoPage(pestañaActual));
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al abrir la pantalla de nuevo movimiento: {ex.Message}");
            }
        }
    }
}