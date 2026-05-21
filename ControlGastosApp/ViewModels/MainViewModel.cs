using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlGastosApp.Models;
using ControlGastosApp.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace ControlGastosApp.ViewModels
{
    // Definimos la clase como parcial e indicamos que hereda de ObservableObject para activar las herramientas de MVVM Toolkit
    public partial class MainViewModel : ObservableObject
    {
        // Instancia privada para manejar la comunicación directa con la base de datos SQLite
        private readonly DatabaseService _dbService = new DatabaseService();

        // Propiedad reactiva para el Capital Total. Al usar [ObservableProperty], 
        // el toolkit genera automáticamente tras bambalinas la propiedad pública "CapitalTotal" en mayúscula
        [ObservableProperty]
        private double capitalTotal;

        // Colecciones especializadas de MAUI. Al añadir o limpiar elementos aquí,
        // los CollectionView de la interfaz (XAML) se refrescarán de inmediato en vivo
        public ObservableCollection<Movimiento> Gastos { get; set; } = new();
        public ObservableCollection<Movimiento> Ingresos { get; set; } = new();

        // Constructor del ViewModel. Se ejecuta al crear la pantalla principal
        public MainViewModel()
        {
            // Iniciamos la carga de datos de manera segura en un hilo de fondo sin congelar la interfaz de usuario
            _ = InicializarDatosAsync();
        }

        // Método seguro que envuelve la carga inicial de datos y maneja posibles excepciones de SQLite
        private async Task InicializarDatosAsync()
        {
            try
            {
                // Llamamos a la carga asíncrona principal
                await CargarDatosAsync();
            }
            catch (System.Exception ex)
            {
                // Si la base de datos llega a fallar al inicio, mostramos una alerta nativa controlada en el dispositivo
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Shell.Current.DisplayAlert("Error de Carga", $"No se pudieron recuperar los datos iniciales: {ex.Message}", "Aceptar");
                });
            }
        }

        // Método principal de lectura de datos de SQLite optimizado para correr en segundo plano
        public async Task CargarDatosAsync()
        {
            // Task.Run empuja las consultas pesadas de la base de datos a un hilo secundario, 
            // liberando por completo el hilo de la interfaz gráfica para evitar congelamientos
            await Task.Run(async () =>
            {
                // 1. Solicitamos la configuración global guardada de forma asíncrona (donde reside el capital actual)
                var config = await _dbService.GetConfigAsync();

                // 2. Cargamos las listas de movimientos desde SQLite (false = Gastos, true = Ingresos)
                var gastosDb = await _dbService.GetMovimientosAsync(false);
                var ingresosDb = await _dbService.GetMovimientosAsync(true);

                // 3. Los cambios visuales (modificar propiedades observables y limpiar colecciones) 
                // DEBEN ejecutarse obligatoriamente de vuelta en el hilo principal de la interfaz de usuario
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    // Asignamos el valor a la propiedad observable, actualizando el Label superior del XAML
                    CapitalTotal = config.CapitalTotal;

                    // Limpiamos la colección de Gastos actual y vaciamos los nuevos registros traídos de la BD
                    Gastos.Clear();
                    foreach (var g in gastosDb)
                    {
                        Gastos.Add(g);
                    }

                    // Limpiamos la colección de Ingresos actual y volcamos los datos actualizados
                    Ingresos.Clear();
                    foreach (var i in ingresosDb)
                    {
                        Ingresos.Add(i);
                    }
                });
            });
        }

        // Método público para permitir al Code-Behind inyectar y salvar el capital en la base de datos
        public async Task GuardarNuevoCapitalEnBD(double nuevoMonto)
        {
            try
            {
                // Solicitamos el registro de configuración actual desde SQLite
                var config = await _dbService.GetConfigAsync();

                // Modificamos el valor interno con el nuevo saldo
                config.CapitalTotal = nuevoMonto;

                // Guardamos los cambios permanentemente en el teléfono
                await _dbService.UpdateConfigAsync(config);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al escribir en SQLite: {ex.Message}");
            }
        }

        // Comando especial vinculado al comportamiento Tap del Frame de Capital en el XAML
        // Cambiado a 'private void' para desatar la acción visual inmediatamente sin bloquear el hilo ejecutor
        [RelayCommand]
        private void ActualizarCapital()
        {
            // Verificación de seguridad básica: Si el Shell de la app no está listo o es nulo,
            // salimos del método de inmediato para proteger la aplicación de un cierre inesperado
            if (Shell.Current == null)
            {
                return;
            }

            // Usamos BeginInvokeOnMainThread (Fuego y Olvido). Envía la orden a la pantalla 
            // de abrir el cuadro de diálogo sin colisionar con los ciclos de carga de la página (OnAppearing)
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    // Desplegamos el prompt nativo en pantalla solicitando el nuevo monto
                    string resultado = await Shell.Current.DisplayPromptAsync(
                        title: "Actualizar Capital",
                        message: "Ingresa el nuevo monto disponible:",
                        accept: "Confirmar",
                        cancel: "Cancelar",
                        placeholder: "Ejemplo: 1500.00",
                        keyboard: Keyboard.Numeric); // Obliga a Android a abrir exclusivamente el teclado numérico

                    // Validamos que el usuario no haya cancelado (resultado no nulo) y que el texto ingresado sea un número real válido
                    if (!string.IsNullOrEmpty(resultado) && double.TryParse(resultado, out double nuevoMonto))
                    {
                        // Primero: Actualizamos la propiedad local en el hilo visual para que el cambio se refleje al instante
                        CapitalTotal = nuevoMonto;

                        // Segundo: Guardamos el cambio de forma permanente en la base de datos ejecutando el proceso en segundo plano
                        // para que la animación de cierre del teclado y del diálogo permanezca totalmente fluida
                        await Task.Run(async () =>
                        {
                            // Solicitamos el objeto de configuración actual desde SQLite
                            var config = await _dbService.GetConfigAsync();

                            // Modificamos el valor interno del objeto con el nuevo saldo ingresado
                            config.CapitalTotal = nuevoMonto;

                            // Guardamos los cambios permanentemente en el archivo de base de datos del teléfono
                            await _dbService.UpdateConfigAsync(config);
                        });
                    }
                }
                catch (System.Exception ex)
                {
                    // En caso de un error inesperado al renderizar el diálogo, lo capturamos en la consola de depuración
                    System.Diagnostics.Debug.WriteLine($"Error al mostrar u operar el prompt de capital: {ex.Message}");
                }
            });
        }
    }
}
