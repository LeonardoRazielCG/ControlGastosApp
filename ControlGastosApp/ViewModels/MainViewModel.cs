using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlGastosApp.Models;
using ControlGastosApp.Services;
using System.Collections.ObjectModel;

namespace ControlGastosApp.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService = new DatabaseService();

        [ObservableProperty]
        private double capitalTotal;

        // Listas que se actualizan solas en la pantalla
        public ObservableCollection<Movimiento> Gastos { get; set; } = new();
        public ObservableCollection<Movimiento> Ingresos { get; set; } = new();

        public MainViewModel()
        {
            CargarDatos();
        }

        public async void CargarDatos()
        {
            var config = await _dbService.GetConfigAsync();
            CapitalTotal = config.CapitalTotal;

            // Cargamos listas desde SQLite
            var gastosDb = await _dbService.GetMovimientosAsync(false);
            var ingresosDb = await _dbService.GetMovimientosAsync(true);

            Gastos.Clear();
            foreach (var g in gastosDb) Gastos.Add(g);

            Ingresos.Clear();
            foreach (var i in ingresosDb) Ingresos.Add(i);
        }

        // Comando para actualizar el capital (se activará desde el Tap)
        [RelayCommand]
        private async Task ActualizarCapital()
        {
            string resultado = await Shell.Current.DisplayPromptAsync("Actualizar Capital", "Ingresa el nuevo monto:", "Confirmar", "Cancelar", keyboard: Keyboard.Numeric);

            if (!string.IsNullOrEmpty(resultado) && double.TryParse(resultado, out double nuevoMonto))
            {
                CapitalTotal = nuevoMonto;
                var config = await _dbService.GetConfigAsync();
                config.CapitalTotal = nuevoMonto;
                await _dbService.UpdateConfigAsync(config);
            }
        }
    }
}
