using System;
using System.Linq; 
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
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (_viewModel != null)
            {
                await _viewModel.CargarDatosAsync();
            }
        }

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
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        // Método óptimo y rápido para activar la eliminación al tocar el renglón
        private async void OnMovimientoTapped(object sender, EventArgs e)
        {
            try
            {
                var elementoVisual = sender as View;
                var gesto = elementoVisual?.GestureRecognizers.OfType<TapGestureRecognizer>().FirstOrDefault();
                var movimientoAEliminar = gesto?.CommandParameter as Movimiento;

                if (movimientoAEliminar != null)
                {
                    bool confirmar = await DisplayAlert("¿Eliminar registro?",
                        $"¿Estás seguro de querer borrar \"{movimientoAEliminar.Concepto}\" por ${movimientoAEliminar.Monto:F2}?",
                        "Sí, eliminar", "Cancelar");

                    if (confirmar)
                    {
                        await _viewModel.EliminarMovimientoAsync(movimientoAEliminar);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al eliminar: {ex.Message}");
            }
        }

        private async void OnCapitalTarjetaTapped(object sender, EventArgs e)
        {
            string resultado = await DisplayPromptAsync("Modificar Capital", "Ingresa tu nuevo capital total:", "Guardar", "Cancelar", placeholder: _viewModel.CapitalTotal.ToString(), keyboard: Keyboard.Numeric);

            if (!string.IsNullOrEmpty(resultado) && double.TryParse(resultado, out double nuevoCapital))
            {
                await _viewModel.GuardarNuevoCapitalEnBD(nuevoCapital);
            }
        }
    }
}