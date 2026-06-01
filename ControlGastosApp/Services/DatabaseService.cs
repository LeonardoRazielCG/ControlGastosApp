using SQLite;
using ControlGastosApp.Models;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

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

            // Creamos las tablas originales
            await _database.CreateTableAsync<ConfigUsuario>();
            await _database.CreateTableAsync<Movimiento>();

            // NUEVAS TABLAS: Para los servicios predeterminados y su historial de consumo
            await _database.CreateTableAsync<ServicioFijo>();
            await _database.CreateTableAsync<HistorialServicio>();

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

        // Método para guardar un nuevo movimiento
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

        // ===================================================================================
        // NUEVOS MÉTODOS: CONTROL DE SERVICIOS FIJOS E HISTORIAL DE CONSUMO
        // ===================================================================================

        /// <summary>
        /// Obtiene la lista completa de todos los servicios predeterminados por el usuario
        /// </summary>
        public async Task<List<ServicioFijo>> GetServiciosFijosAsync()
        {
            await Init();
            return await _database.Table<ServicioFijo>().ToListAsync();
        }

        /// <summary>
        /// Guarda un nuevo servicio o actualiza uno ya existente
        /// </summary>
        public async Task<int> SaveServicioFijoAsync(ServicioFijo servicio)
        {
            await Init();
            if (servicio.Id != 0)
            {
                return await _database.UpdateAsync(servicio); // Si ya tiene Id, lo actualiza
            }
            else
            {
                return await _database.InsertAsync(servicio); // Si es nuevo, lo inserta
            }
        }

        /// <summary>
        /// Elimina de forma definitiva un servicio predeterminado
        /// </summary>
        public async Task<int> DeleteServicioFijoAsync(ServicioFijo servicio)
        {
            await Init();
            return await _database.DeleteAsync(servicio);
        }

        /// <summary>
        /// Registra un nuevo pago en el historial para llevar el control de consumo
        /// </summary>
        public async Task<int> SaveHistorialServicioAsync(HistorialServicio historial)
        {
            await Init();
            return await _database.InsertAsync(historial);
        }

        /// <summary>
        /// Trae todo el historial de pagos de un servicio en orden cronológico inverso (para las gráficas)
        /// </summary>
        public async Task<List<HistorialServicio>> GetHistorialPorServicioAsync(int servicioId)
        {
            await Init();
            return await _database.Table<HistorialServicio>()
                                  .Where(h => h.ServicioFijoId == servicioId)
                                  .OrderByDescending(h => h.FechaPago)
                                  .ToListAsync();
        }

        public Task<int> DeleteServicioAsync(ServicioFijo servicio)
        {
            // Utiliza la instancia de tu conexión SQLite Async (comúnmente llamada _database o Connection)
            return _database.DeleteAsync(servicio);
        }

    }
}
