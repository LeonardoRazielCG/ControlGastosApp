using System;
using Microsoft.Maui.Controls;
using ControlGastosApp.Models;
using ControlGastosApp.Services;
using System.Collections.Generic;

namespace ControlGastosApp.Views
{
    public partial class ServiciosPage : ContentPage
    {
        private readonly DatabaseService _databaseService;

        public ServiciosPage()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            // Cargamos los servicios desde SQLite
            await CargarServiciosAsync();
        }

        private async Task CargarServiciosAsync()
        {
            var lista = await _databaseService.GetServiciosFijosAsync();
            listaServicios.ItemsSource = lista;
        }

        // Evento para dar de alta un servicio predeterminado nuevo (Luz, Agua, etc)
        private async void OnAgregarServicioClicked(object sender, EventArgs e)
        {
            string nombre = await DisplayPromptAsync("Nuevo Servicio", "Nombre del servicio (ej. Luz CFE, Neto):");
            if (string.IsNullOrEmpty(nombre)) return;

            string montoStr = await DisplayPromptAsync("Monto Estimado", "Monto promedio aproximado:", keyboard: Keyboard.Numeric);
            if (!double.TryParse(montoStr, out double monto)) return;

            string diaStr = await DisplayPromptAsync("Día de Vencimiento", "Día del mes en que vence (1-31):", keyboard: Keyboard.Numeric);
            if (!int.TryParse(diaStr, out int dia) || dia < 1 || dia > 31) return;

            var nuevoServicio = new ServicioFijo
            {
                Nombre = nombre,
                MontoEstimado = monto,
                DiaVencimiento = dia,
                Periodicidad = "Mensual"
            };

            await _databaseService.SaveServicioFijoAsync(nuevoServicio);
            await CargarServiciosAsync();
        }

        // Evento cuando marcan el Checkbox (Pagar Recibo)
        private async void OnPagarServicioCheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            // Solo actuar si el usuario marca la casilla como verdadera
            if (!e.Value) return;

            var cb = sender as CheckBox;
            var servicio = cb?.BindingContext as ServicioFijo;
            if (servicio == null) return;

            // Pedir la cantidad REAL cobrada en el recibo
            string montoRealStr = await DisplayPromptAsync("Registrar Pago",
                $"¿Cuánto llegó de {servicio.Nombre}? (Estimado: ${servicio.MontoEstimado:F2})",
                "Registrar", "Cancelar", placeholder: servicio.MontoEstimado.ToString(), keyboard: Keyboard.Numeric);

            if (double.TryParse(montoRealStr, out double montoReal))
            {
                // 1. Guardar en el historial para las gráficas
                var historial = new HistorialServicio
                {
                    ServicioFijoId = servicio.Id,
                    FechaPago = DateTime.Now,
                    MontoReal = montoReal
                };
                await _databaseService.SaveHistorialServicioAsync(historial);

                // 2. Descontarlo en automático del Capital Total como un Gasto recurrente
                var config = await _databaseService.GetConfigAsync();
                if (config != null)
                {
                    config.CapitalTotal -= montoReal;
                    await _databaseService.UpdateConfigAsync(config);
                }

                // 3. Registrarlo también en la lista de movimientos diaria para que no se desfase
                var movimientoGasto = new Movimiento
                {
                    Concepto = $"Pago {servicio.Nombre}",
                    Monto = montoReal,
                    EsIngreso = false,
                    Fecha = DateTime.Now
                };
                await _databaseService.SaveMovimientoAsync(movimientoGasto);

                MessagingCenter.Send(this, "ServicioPagado");

                await DisplayAlert("Éxito", "Pago registrado y descontado de tu capital.", "OK");
            }

            // Desmarcamos el checkbox para que quede listo para el siguiente mes
            cb.IsChecked = false;
        }

        // Evento para ver los consumos anteriores (Próximamente con gráficas)
        private async void OnVerHistorialClicked(object sender, EventArgs e)
        {
            var btn = sender as ImageButton;
            var servicio = btn?.CommandParameter as ServicioFijo;
            if (servicio == null) return;

            var historial = await _databaseService.GetHistorialPorServicioAsync(servicio.Id);

            // Construimos un resumen rápido en texto en lo que metemos las gráficas
            string mensaje = historial.Count == 0 ? "No hay pagos registrados aún." : "";
            foreach (var h in historial)
            {
                mensaje += $"{h.FechaPago:dd/MM/yyyy}: ${h.MontoReal:F2}\n";
            }

            await DisplayAlert($"Historial de {servicio.Nombre}", mensaje, "Cerrar");
        }

        private async void OnEliminarServicioClicked(object sender, EventArgs e)
        {
            try
            {
                var mi = sender as MenuItem;

                // Corregido: Usamos tu clase real 'ServicioFijo'
                var servicioAEliminar = mi?.CommandParameter as ServicioFijo;

                if (servicioAEliminar != null)
                {
                    bool confirmar = await DisplayAlert("¿Eliminar servicio?",
                        $"¿Estás seguro de que quieres quitar \"{servicioAEliminar.Nombre}\" de tus servicios fijos?",
                        "Sí, eliminar", "Cancelar");

                    if (confirmar)
                    {
                        // 1. Borramos permanentemente de la base de datos SQLite
                        await _databaseService.DeleteServicioAsync(servicioAEliminar);

                        // 2. Quitamos el elemento de la lista visual en caliente
                        if (listaServicios.ItemsSource is System.Collections.IList listaLocal)
                        {
                            listaLocal.Remove(servicioAEliminar);

                            // TRUCO DE ORO: Forzamos al ListView a redibujarse en pantalla de inmediato
                            listaServicios.ItemsSource = null;
                            listaServicios.ItemsSource = listaLocal;
                        }

                        await DisplayAlert("Eliminado", "El servicio ha sido eliminado correctamente.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo eliminar el servicio: {ex.Message}", "OK");
            }
        }
    }
}