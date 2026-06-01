using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace ControlGastosApp.Models
{
    public class HistorialServicio
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int ServicioFijoId { get; set; } // Relación con la tabla ServicioFijo

        public DateTime FechaPago { get; set; } // Cuándo se pagó

        public double MontoReal { get; set; } // Lo que realmente cobraron en el recibo
    }
}
