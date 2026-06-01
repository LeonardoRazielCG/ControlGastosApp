using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace ControlGastosApp.Models
{
    public class ServicioFijo
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Nombre { get; set; } // Ej: "Luz CFE", "Internet", "Agua"

        public double MontoEstimado { get; set; } // El pago promedio esperado

        public int DiaVencimiento { get; set; } // Día del mes en que vence (Ej: 15)

        public string Periodicidad { get; set; } // "Mensual", "Bimestral", "Anual"
    }
}