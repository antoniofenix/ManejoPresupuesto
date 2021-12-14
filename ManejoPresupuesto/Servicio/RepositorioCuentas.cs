using Dapper;
using ManejoPresupuesto.Helpers;
using ManejoPresupuesto.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ManejoPresupuesto.Servicio
{
    public interface IRepositorioCuentas
    {
        Task Actualizar(CuentaCreacionViewModel cuenta);
        Task Borrar(int id);
        Task<IEnumerable<Cuenta>> Buscar(int usuarioId);
        Task Crear(Cuenta cuenta);
        Task<Cuenta> ObtenerPorId(int id, int usuarioId);
    }

    public class RepositorioCuentas : IRepositorioCuentas
    {
        private readonly string connectionString;

        public RepositorioCuentas(IConfiguration configuration)
        {
            this.connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task Crear(Cuenta cuenta)
        {
            using var connection = new SqlConnection(connectionString);
            var query = @"
            INSERT INTO [dbo].[Cuentas]
                       ([Nombre]
                       ,[TipoCuentaId]
                       ,[Balance]
                       ,[Descripcion])
                 VALUES
                       (
		               @Nombre,
		               @TipoCuentaId,
		               @Balance,
		               @Descripcion
		               );

            SELECT SCOPE_IDENTITY();";
            var id = await connection.QuerySingleAsync<int>(query, cuenta);
            cuenta.Id = id;

        }

        public async Task<IEnumerable<Cuenta>> Buscar(int usuarioId)
        {
            using var connection = new SqlConnection(connectionString);
            var query = @"
            SELECT Cuentas.Id,Cuentas.Nombre,Balance,tc.Nombre as TipoCuenta
            FROM Cuentas
            INNER JOIN TiposCuentas tc
            ON TC.Id = Cuentas.TipoCuentaId
            WHERE tc.UsuarioId = @UsuarioId
            ORDER BY tc.Orden";
            return await connection.QueryAsync<Cuenta>(query, new { usuarioId });

        }

        public async Task<Cuenta> ObtenerPorId(int id, int usuarioId)
        {
            using var connection = new SqlConnection(connectionString);
            var query = @"
            SELECT Cuentas.Id,Cuentas.Nombre,Balance,Descripcion ,Cuentas.TipoCuentaId
            FROM Cuentas
            INNER JOIN TiposCuentas tc
            ON tc.Id = Cuentas.TipoCuentaId
            WHERE tc.UsuarioId = @UsuarioId
            AND Cuentas.Id = @Id
            ORDER BY tc.Orden";

            return await connection.QueryFirstOrDefaultAsync<Cuenta>(query, new { id, usuarioId });

        }

        public async Task Actualizar(CuentaCreacionViewModel cuenta)
        {
            using var connection = new SqlConnection(connectionString);
            var query = @"
            UPDATE Cuentas SET 
                 Nombre =@Nombre
                ,Balance = @Balance
                ,Descripcion = @Descripcion
                ,TipoCuentaId =@TipoCuentaId
            WHERE Id =@Id;
            ";

            await connection.ExecuteAsync(query, cuenta);

        }
        public async Task Borrar(int id)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.ExecuteAsync("DELETE Cuentas WHERE Id = @Id", new { id });
        }
    }
}
