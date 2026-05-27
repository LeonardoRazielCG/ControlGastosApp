using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ControlGastosApp.Models;
using ControlGastosApp.Services;

namespace ControlGastosApp.ViewModels
{
    // Clase auxiliar para que el Carrusel no se confunda
    public class SeccionMovimientos
    {
        public string Titulo { get; set; }
        public ObservableCollection<Movimiento> Items { get; set; }
        public bool EsIngreso { get; set; }
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _dbService;
        private double _capitalTotal;

        // Esta es la colección maestra que leerá el Carrusel
        public ObservableCollection<SeccionMovimientos> Secciones { get; set; }

        public ObservableCollection<Movimiento> Gastos { get; set; }
        public ObservableCollection<Movimiento> Ingresos { get; set; }

        public double CapitalTotal
        {
            get => _capitalTotal;
            set { _capitalTotal = value; OnPropertyChanged(); }
        }

        public MainViewModel()
        {
            _dbService = new DatabaseService();
            Gastos = new ObservableCollection<Movimiento>();
            Ingresos = new ObservableCollection<Movimiento>();
            Secciones = new ObservableCollection<SeccionMovimientos>();

            // Creamos las dos pestañas fijas
            Secciones.Add(new SeccionMovimientos { Titulo = "Gastos", Items = Gastos, EsIngreso = false });
            Secciones.Add(new SeccionMovimientos { Titulo = "Ingresos", Items = Ingresos, EsIngreso = true });

            _ = CargarDatosAsync();
        }

        public async Task GuardarNuevoCapitalEnBD(double nuevoCapital)
        {
            var config = await _dbService.GetConfigAsync() ?? new ConfigUsuario();
            config.CapitalTotal = nuevoCapital;
            await _dbService.UpdateConfigAsync(config);
            CapitalTotal = nuevoCapital;
        }

        public async Task CargarDatosAsync()
        {
            try
            {
                var config = await _dbService.GetConfigAsync();
                if (config != null) CapitalTotal = config.CapitalTotal;

                // Leemos de SQLite
                var listaGastosBD = await _dbService.GetMovimientosAsync(false);
                var listaIngresosBD = await _dbService.GetMovimientosAsync(true);

                // Actualizamos las listas en el hilo principal
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Gastos.Clear();
                    foreach (var m in listaGastosBD) Gastos.Add(m);

                    Ingresos.Clear();
                    foreach (var m in listaIngresosBD) Ingresos.Add(m);
                });
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}