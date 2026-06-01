using System;
using Microsoft.Maui.Controls;
using ControlGastosApp.ViewModels;
using ControlGastosApp.Models;

namespace ControlGastosApp.Views
{
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel _viewModel;

        public MainPage()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            BindingContext = _viewModel;

            //Nos suscribimos al mensaje que viene desde la pantallaa de Servicios
            MessagingCenter.Subscribe<ServiciosPage>(this, "ServicioPagado", async (sender) =>
            {
                if (_viewModel != null)
                {
                    //Forzamos a la pantalla principal al volver a leer la BD en hilos seguros 
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await _viewModel.CargarDatosAsync();
                    });
                }
            });
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (_viewModel != null)
            {
                // Sincroniza el capital total y las listas desde la BD al abrir la app
                await _viewModel.CargarDatosAsync();
            }
        }

        /// <summary>
        /// Abre el formulario flotante para añadir un registro
        /// </summary>
        private async void OnAgregarMovimientoClicked(object sender, EventArgs e)
        {
            try
            {
                var seccionActual = carouselViews.CurrentItem as SeccionMovimientos;
                string nombrePestaña = seccionActual?.Titulo ?? "Gastos";
                await Navigation.PushModalAsync(new NuevoMovimientoPage(nombrePestaña));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al abrir agregar: {ex.Message}");
            }
        }

        /// <summary>
        /// Ejecuta la eliminación nativa mediante el menú contextual (Al dejar presionado el renglón)
        /// </summary>
        private async void OnEliminarContextClicked(object sender, EventArgs e)
        {
            try
            {
                var mi = sender as MenuItem;
                var movimientoAEliminar = mi?.CommandParameter as Movimiento;

                if (movimientoAEliminar != null)
                {
                    bool confirmar = await DisplayAlert("¿Eliminar registro?",
                        $"¿Estás seguro de que quieres borrar \"{movimientoAEliminar.Concepto}\" por ${movimientoAEliminar.Monto:F2}?",
                        "Sí, eliminar", "Cancelar");

                    if (confirmar)
                    {
                        await _viewModel.EliminarMovimientoAsync(movimientoAEliminar);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al eliminar desde contexto: {ex.Message}");
            }
        }

        /// <summary>
        /// Modifica el capital base desde la tarjeta superior
        /// </summary>
        private async void OnCapitalTarjetaTapped(object sender, EventArgs e)
        {
            try
            {
                string resultado = await DisplayPromptAsync("Modificar Capital",
                    "Ingresa tu nuevo capital total:",
                    "Guardar",
                    "Cancelar",
                    placeholder: _viewModel.CapitalTotal.ToString(),
                    keyboard: Keyboard.Numeric);

                if (!string.IsNullOrEmpty(resultado) && double.TryParse(resultado, out double nuevoCapital))
                {
                    await _viewModel.GuardarNuevoCapitalEnBD(nuevoCapital);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al modificar capital: {ex.Message}");
            }
        }
    }
}