using SQLite;
using ControlGastosApp.Models;

namespace ControlGastosApp.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _database;

        async Task Init()
        {
            if (_database is not null) return;
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "Finanzas.db3");
            _database = new SQLiteAsyncConnection(dbPath);

            // Creamos las tablas
            await _database.CreateTableAsync<ConfigUsuario>();
            await _database.CreateTableAsync<Movimiento>();

            // Si es la primera vez, creamos un usuario inicial
            var user = await _database.Table<ConfigUsuario>().FirstOrDefaultAsync();
            if (user == null)
            {
                await _database.InsertAsync(new ConfigUsuario { Pin = "1234", CapitalTotal = 0 });
            }
        }

        // Método para validar el PIN
        public async Task<bool> ValidarPinAsync(string pinIngresado)
        {
            await Init();
            var user = await _database.Table<ConfigUsuario>().FirstOrDefaultAsync();
            return user != null && user.Pin == pinIngresado;
        }

        // Método para obtener el capital
        public async Task<ConfigUsuario> GetConfigAsync()
        {
            await Init();
            return await _database.Table<ConfigUsuario>().FirstOrDefaultAsync();
        }

        // Método para actualizar el capital
        public async Task UpdateConfigAsync(ConfigUsuario config)
        {
            await Init();
            await _database.UpdateAsync(config);
        }

        // Método para obtener movimientos filtrados por tipo (Ingreso o Gasto)  -------------- ESTE METODO ES GENERICO ------------
        public async Task<List<Movimiento>> GetMovimientosAsync(bool esIngreso)
        {
            await Init(); // Asegura que la base de datos esté lista
            return await _database.Table<Movimiento>()
                                  .Where(m => m.EsIngreso == esIngreso)
                                  .OrderByDescending(m => m.Fecha)
                                  .ToListAsync();
        }

        // Método para guardar un nuevo movimiento (lo usaremos en el siguiente paso)
        public async Task SaveMovimientoAsync(Movimiento item)
        {
            await Init();
            await _database.InsertAsync(item);
        }

        // Método para borrar un movimiento --------- ESTE METODO ES ASINCRONICO -------------
        public async Task<int> DeleteMovimientoAsync(Movimiento movimiento)
        {
            await Init(); // Asegura que la conexión este abierta
            return await _database.DeleteAsync(movimiento);
        }
    }
}
