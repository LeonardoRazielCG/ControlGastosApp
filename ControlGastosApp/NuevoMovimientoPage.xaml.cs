using Microsoft.Maui.Controls;
using ControlGastosApp.Models;
using ControlGastosApp.Services;
using System;

namespace ControlGastosApp
{
    public partial class NuevoMovimientoPage : ContentPage
    {
        // Instancia local del servicio de la base de datos
        private readonly DatabaseService _dbService = new DatabaseService();

        // Variable para recordar si este registro es un Ingreso o un Gasto
        private readonly bool _esIngreso;

        // Modificamos el constructor para recibir el texto de la pestaña
        public NuevoMovimientoPage(string tipoMovimiento)
        {
            InitializeComponent();

            // Si el texto que viene de la pantalla principal es "Ingresos", la variable será true, de lo contrario false (Gastos)
            _esIngreso = (tipoMovimiento == "Ingresos");
        }

        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            // Forzamos a ocultar el teclado si el usuario cancela la operación
            OcultarTeclado();

            await Navigation.PopModalAsync();
        }

        // Lógica definitiva para el botón Guardar
        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            try
            {
                // 1. Validamos que el monto ingresado sea un número válido y mayor a cero
                if (string.IsNullOrWhiteSpace(txtMonto.Text) || !double.TryParse(txtMonto.Text, out double monto) || monto <= 0)
                {
                    await DisplayAlert("Dato Incorrecto", "Por favor, ingresa un monto válido mayor a 0.", "Aceptar");
                    return;
                }

                // 2. Validamos que el concepto no esté vacío
                if (string.IsNullOrWhiteSpace(txtConcepto.Text))
                {
                    await DisplayAlert("Dato Incompleto", "Por favor, escribe un concepto o descripción.", "Aceptar");
                    return;
                }

                // Forzamos a ocultar el teclado aquí para que Android lo guarde antes de cerrar la animación modal
                OcultarTeclado();

                // 3. Creamos el nuevo objeto Movimiento con los datos capturados en la interfaz
                var nuevoMovimiento = new Movimiento
                {
                    Monto = double.Parse(txtMonto.Text),
                    Concepto = txtConcepto.Text,
                    EsIngreso = _esIngreso,
                    Fecha = DateTime.Now // Registra la fecha y hora exacta actual de San Luis Potosí
                };

                // 4. Guardamos permanentemente en la base de datos SQLite en un hilo de fondo
                await System.Threading.Tasks.Task.Run(async () =>
                {
                    await _dbService.SaveMovimientoAsync(nuevoMovimiento);

                    // Ajustamos automáticamente el Capital Total sumando o restando el impacto del nuevo registro
                    var config = await _dbService.GetConfigAsync();
                    if (_esIngreso)
                    {
                        config.CapitalTotal += monto; // Si es ingreso, suma al dinero disponible
                    }
                    else
                    {
                        config.CapitalTotal -= monto; // Si es gasto, resta del dinero disponible
                    }
                    await _dbService.UpdateConfigAsync(config);
                });

                // 5. Cerramos la pantalla regresando a la vista principal
                await Navigation.PopModalAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo guardar el registro: {ex.Message}", "Aceptar");
            }
        }

        /// <summary>
        /// Quita el foco de los cuadros de texto, obligando al sistema operativo Android a ocultar el teclado virtual.
        /// </summary>
        private void OcultarTeclado()
        {
            if (txtMonto != null && txtMonto.IsFocused)
                txtMonto.Unfocus();

            if (txtConcepto != null && txtConcepto.IsFocused)
                txtConcepto.Unfocus();
        }
    }
}