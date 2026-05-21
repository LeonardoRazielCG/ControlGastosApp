using SQLite;

namespace ControlGastosApp.Models
{
    public class Movimiento
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Concepto { get; set; }

        public double Monto { get; set; }

        public DateTime Fecha { get; set; }

        // Esta propiedad diferencia si es gasto (false) o ingreso (true)
        public bool EsIngreso { get; set; }
    }
}