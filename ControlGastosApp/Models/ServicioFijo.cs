using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SQLite;

namespace ControlGastosApp.Models
{
    public class ServicioFijo : INotifyPropertyChanged
    {
        private int _id;
        private string _nombre;
        private double _montoEstimado;
        private DateTime _fechaProximoPago;
        private string _periodicidad;

        [PrimaryKey, AutoIncrement]
        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Nombre
        {
            get => _nombre;
            set { _nombre = value; OnPropertyChanged(); }
        }

        public double MontoEstimado
        {
            get => _montoEstimado;
            set { _montoEstimado = value; OnPropertyChanged(); }
        }

        public DateTime FechaProximoPago
        {
            get => _fechaProximoPago;
            set
            {
                _fechaProximoPago = value;
                OnPropertyChanged();
                // CRUCIAL: Cuando la fecha cambie, le avisamos al XAML que recalcule la alerta
                OnPropertyChanged(nameof(DebeMostrarAlerta));
            }
        }

        public string Periodicidad
        {
            get => _periodicidad;
            set { _periodicidad = value; OnPropertyChanged(); }
        }

        // ==== PROPIEDAD EVALUADORA DE ALERTAS ====
        [Ignore]
        public bool DebeMostrarAlerta
        {
            get
            {
                if (FechaProximoPago.Date == DateTime.MinValue.Date) return false;

                DateTime fechaPagoPura = FechaProximoPago.Date;
                DateTime fechaHoyPura = DateTime.Today;

                TimeSpan diferencia = fechaPagoPura - fechaHoyPura;
                double diasRestantes = diferencia.TotalDays;

                // Devuelve TRUE si faltan 2 días o menos (o si ya se pasó la fecha)
                return diasRestantes <= 2;
            }
        }

        // ==== LOGICA DE NOTIFICACIÓN PARA MAUI ====
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}