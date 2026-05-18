using SQLite;

namespace ControlGastosApp.Models
{
    public class ConfigUsuario
    {
        [PrimaryKey]
        public int Id { get; set; } = 1; // Solo tendremos un usuario/configuración
        public string Pin { get; set; } = "1234"; // PIN por defecto para pruebas
        public double CapitalTotal { get; set; } = 0;
    }
}