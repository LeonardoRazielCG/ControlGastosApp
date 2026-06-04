using ControlGastosApp.Models;
using ControlGastosApp.Services;
using Microsoft.Maui.Controls;
using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        // Evento para dar de alta un servicio predeterminado nuevo (Luz, Pago cada 3 días, etc)
        private async void OnAgregarServicioClicked(object sender, EventArgs e)
        {
            // 1. Nombre del servicio
            string nombre = await DisplayPromptAsync("Nuevo Servicio", "Nombre del servicio o pago fijo:");
            if (string.IsNullOrEmpty(nombre)) return;

            // 2. Monto estimado
            string montoStr = await DisplayPromptAsync("Monto Estimado", "Monto promedio aproximado:", keyboard: Keyboard.Numeric);
            if (!double.TryParse(montoStr, out double monto)) return;

            // 3. FLEXIBILIDAD: Selección de periodicidad/frecuencia
            string periodo = await DisplayActionSheet("Frecuencia de Pago", "Cancelar", null,
                "Cada 3 días", "Semanal", "Quincenal", "Mensual");

            if (string.IsNullOrEmpty(periodo) || periodo == "Cancelar") return;

            // 4. MODO PRUEBA / CONFIGURACIÓN DE FECHA: Fecha del primer pago
            // Le pedimos al usuario que escriba una fecha (propone HOY por defecto para facilitar pruebas)
            string fechaSugerida = DateTime.Today.ToString("dd/MM/yyyy");
            string fechaStr = await DisplayPromptAsync("Próxima Fecha de Pago",
                "Ingresa la fecha en formato DD/MM/YYYY:\n(Para probar notificaciones puedes ingresar el día de mañana o pasado mañana)",
                "Aceptar", "Cancelar", placeholder: fechaSugerida);

            if (!DateTime.TryParseExact(fechaStr, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime fechaSeleccionada))
            {
                // Si el formato es inválido o cancela, tomamos HOY por defecto para no romper el flujo
                fechaSeleccionada = DateTime.Today;
            }

            // Creamos el nuevo objeto con las nuevas propiedades dinámicas
            var nuevoServicio = new ServicioFijo
            {
                Nombre = nombre,
                MontoEstimado = monto,
                FechaProximoPago = fechaSeleccionada, // Almacena la fecha real elegida
                Periodicidad = periodo                     // Almacena si es semanal, quincenal, mensual, etc.
            };

            await _databaseService.SaveServicioFijoAsync(nuevoServicio);
            await EnviarNotificacionNativaAsync(nuevoServicio);
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
                    Monto = movimientoGastoMontoFormat(montoReal),
                    EsIngreso = false,
                    Fecha = DateTime.Now
                };
                await _databaseService.SaveMovimientoAsync(movimientoGasto);

                // --- 4. NUEVA LÓGICA AUTOMÁTICA: CALCULAR EL SIGUIENTE CICLO DE FECHA ---
                switch (servicio.Periodicidad)
                {
                    case "Cada 3 días":
                        servicio.FechaProximoPago = servicio.FechaProximoPago.AddDays(3);
                        break;
                    case "Semanal":
                        servicio.FechaProximoPago = servicio.FechaProximoPago.AddDays(7);
                        break;
                    case "Quincenal":
                        servicio.FechaProximoPago = servicio.FechaProximoPago.AddDays(15);
                        break;
                    case "Mensual":
                    default:
                        servicio.FechaProximoPago = servicio.FechaProximoPago.AddMonths(1);
                        break;
                }

                // Actualizamos el servicio en la base de datos con su nueva fecha calculada
                await _databaseService.SaveServicioFijoAsync(servicio); // Nota: Si tienes un UpdateServicioAsync puedes usarlo aquí

                MessagingCenter.Send(this, "ServicioPagado");

                await DisplayAlert("Éxito", "Pago registrado y descontado de tu capital.", "OK");

                // Recargamos la lista visual para actualizar la fecha que se muestra en pantalla
                await CargarServiciosAsync();
            }

            // Desmarcamos el checkbox para que quede listo para el siguiente ciclo/mes
            cb.IsChecked = false;
        }

        // Método auxiliar opcional por si requieres formatear internamente el monto antes de guardar
        private double movimientoGastoMontoFormat(double monto) => monto;

        // Evento para ver los consumos anteriores
        private async void OnVerHistorialClicked(object sender, EventArgs e)
        {
            var btn = sender as ImageButton;
            var servicio = btn?.CommandParameter as ServicioFijo;
            if (servicio == null) return;

            var historial = await _databaseService.GetHistorialPorServicioAsync(servicio.Id);

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
                var servicioAEliminar = mi?.CommandParameter as ServicioFijo;

                if (servicioAEliminar != null)
                {
                    bool confirmar = await DisplayAlert("¿Eliminar servicio?",
                        $"¿Estás seguro de que quieres quitar \"{servicioAEliminar.Nombre}\" de tus servicios fijos?",
                        "Sí, eliminar", "Cancelar");

                    if (confirmar)
                    {
                        await _databaseService.DeleteServicioAsync(servicioAEliminar);

                        if (listaServicios.ItemsSource is System.Collections.IList listaLocal)
                        {
                            listaLocal.Remove(servicioAEliminar);
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

        private async Task EnviarNotificacionNativaAsync(ServicioFijo servicio)
        {
            if (await LocalNotificationCenter.Current.AreNotificationsEnabled() == false)
            {
                await LocalNotificationCenter.Current.RequestNotificationPermission();
            }

            // Cancelamos cualquier alerta previa agendada de este servicio para evitar duplicados
            LocalNotificationCenter.Current.Cancel(servicio.Id + 1000);
            LocalNotificationCenter.Current.Cancel(servicio.Id + 2000);
            LocalNotificationCenter.Current.Cancel(servicio.Id + 5000);

            // Calculamos los dias que faltan desde hoy hasta la fecha de pago
            double diasRestantes = (servicio.FechaProximoPago.Date - DateTime.Today).TotalDays;

            // ====================================================================
            // PRUEBA INMEDIATA: Si falta 1 o 2 dias, te avisa a los 2 segundos
            // ====================================================================
            if (diasRestantes == 1 || diasRestantes == 2)
            {
                var notifInmediata = new NotificationRequest
                {
                    NotificationId = servicio.Id + 5000, // ID unico de rescate
                    Title = diasRestantes == 1 ? "Pago Manana" : "Pago Proximo (En 2 dias)",
                    Description = $"Tu servicio '{servicio.Nombre}' vence pronto ({servicio.FechaProximoPago:dd/MM/yyyy}). Monto: ${servicio.MontoEstimado:F2}",
                    BadgeNumber = 1,
                    Android = new AndroidOptions
                    {
                        Priority = AndroidPriority.High,
                        VisibilityType = AndroidVisibilityType.Public
                    },
                    Schedule = new NotificationRequestSchedule
                    {
                        NotifyTime = DateTime.Now.AddSeconds(2) // Se dispara 2 segundos despues de crearlo
                    }
                };
                await LocalNotificationCenter.Current.Show(notifInmediata);
            }

            // ====================================================================
            // CLASICO: Programacion real para el futuro (A las 10:00 AM)
            // ====================================================================

            // --- ALERTA: 2 DIAS ANTES ---
            if (diasRestantes >= 2)
            {
                var fechaAlerta2Dias = servicio.FechaProximoPago.Date.AddDays(-2).AddHours(10);
                if (fechaAlerta2Dias > DateTime.Now) // Solo si la hora no ha pasado
                {
                    var notif2Dias = new NotificationRequest
                    {
                        NotificationId = servicio.Id + 1000,
                        Title = "Pago Proximo (En 2 dias)",
                        Description = $"Tu servicio '{servicio.Nombre}' vence el {servicio.FechaProximoPago:dd/MM/yyyy}.",
                        BadgeNumber = 1,
                        Android = new AndroidOptions { Priority = AndroidPriority.High, VisibilityType = AndroidVisibilityType.Public },
                        Schedule = new NotificationRequestSchedule { NotifyTime = fechaAlerta2Dias }
                    };
                    await LocalNotificationCenter.Current.Show(notif2Dias);
                }
            }

            // --- ALERTA: 1 DIA ANTES ---
            if (diasRestantes >= 1)
            {
                var fechaAlerta1Dia = servicio.FechaProximoPago.Date.AddDays(-1).AddHours(10);
                if (fechaAlerta1Dia > DateTime.Now) // Solo si la hora no ha pasado
                {
                    var notif1Dia = new NotificationRequest
                    {
                        NotificationId = servicio.Id + 2000,
                        Title = "Pago Manana",
                        Description = $"Tu servicio '{servicio.Nombre}' vence manana.",
                        BadgeNumber = 1,
                        Android = new AndroidOptions { Priority = AndroidPriority.High, VisibilityType = AndroidVisibilityType.Public },
                        Schedule = new NotificationRequestSchedule { NotifyTime = fechaAlerta1Dia }
                    };
                    await LocalNotificationCenter.Current.Show(notif1Dia);
                }
            }
        }
    }
}