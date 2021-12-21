using Dapper;
using ManejoPresupuesto.Models;
using Microsoft.Data.SqlClient;

namespace ManejoPresupuesto.Servicio
{
    public interface IRepositorioTransacciones
    {
        Task Actualizar(Transaccion transaccion, decimal montoAnterior, int cuentaAnterior);
        Task Borrar(int id);
        Task Crear(Transaccion transaccion);
        Task<IEnumerable<Transaccion>> ObtenerPorCuemtaId(ObtenerTransaccionesPorCuenta modelo);
        Task<Transaccion> ObtenerPorId(int id, int usuarioId);
        Task<IEnumerable<Transaccion>> ObtenerPorUsuarioId(ParametroObtenerTransaccionesPorUsuario modelo);
        Task<IEnumerable<ResultadoObtenerPorSemana>> ObtenerPorSemana(ParametroObtenerTransaccionesPorUsuario modelo);
        Task<IEnumerable<ResultadoObtenerPorMes>> ObtenerPorMes(int usuarioId, int año);
    }

    public class RepositorioTransacciones : IRepositorioTransacciones
    {
        private readonly string connectionString;

        public RepositorioTransacciones(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task Crear(Transaccion transaccion)
        {
            using var connection = new SqlConnection(connectionString);
            var id = await connection.QuerySingleAsync<int>("spTransacciones_Insertar",
                new
                {
                    transaccion.UsuarioId,
                    transaccion.FechaTransaccion,
                    transaccion.Monto,
                    transaccion.CategoriaId,
                    transaccion.CuentaId,
                    transaccion.Nota
                },
                commandType: System.Data.CommandType.StoredProcedure);

            transaccion.Id = id;
        }

        public async Task Actualizar(Transaccion transaccion, decimal montoAnterior,
            int cuentaAnteriorId)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.ExecuteAsync("spTransacciones_Actualizar",
                new
                {
                    transaccion.Id,
                    transaccion.FechaTransaccion,
                    transaccion.Monto,
                    transaccion.CategoriaId,
                    transaccion.CuentaId,
                    transaccion.Nota,
                    montoAnterior,
                    cuentaAnteriorId
                }, commandType: System.Data.CommandType.StoredProcedure);
        }

        public async Task<Transaccion> ObtenerPorId(int id, int usuarioId)
        {
            using var connection = new SqlConnection(connectionString);
            return await connection.QueryFirstOrDefaultAsync<Transaccion>(
                @"SELECT Transacciones.*, cat.TipoOperacionId
                FROM Transacciones
                INNER JOIN Categorias cat
                ON cat.Id = Transacciones.CategoriaId
                WHERE Transacciones.Id = @Id AND Transacciones.UsuarioId = @UsuarioId",
                new { id, usuarioId });
        }

        public async Task Borrar(int id)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.ExecuteAsync("spTransacciones_Borrar",
                new { id }, commandType: System.Data.CommandType.StoredProcedure);
        }


        public async Task<IEnumerable<Transaccion>> ObtenerPorCuemtaId(ObtenerTransaccionesPorCuenta modelo)
        {
            using var connection = new SqlConnection(connectionString);
            var query = @"
            SELECT t.Id,t.Monto,t.FechaTransaccion , c.Nombre as Categoria, cu.Nombre as Cuenta , c.TipoOperacionId
            FROM Transacciones t
            INNER JOIN Categorias c
            ON c.Id = t.CategoriaId
            INNER JOIN Cuentas cu
            ON cu.Id = t.CuentaId
            WHERE t.CuentaId = @CuentaId AND t.UsuarioId = @UsuarioId
            AND FechaTransaccion BETWEEN @FechaInicio AND @FechaFin";

            return await connection.QueryAsync<Transaccion>(query,modelo); 

        }


        public async Task<IEnumerable<Transaccion>> ObtenerPorUsuarioId(ParametroObtenerTransaccionesPorUsuario modelo)
        {
            using var connection = new SqlConnection(connectionString);
            var query = @"
            SELECT t.Id,t.Monto,t.FechaTransaccion , c.Nombre as Categoria, cu.Nombre as Cuenta , c.TipoOperacionId ,Nota
            FROM Transacciones t
            INNER JOIN Categorias c
            ON c.Id = t.CategoriaId
            INNER JOIN Cuentas cu
            ON cu.Id = t.CuentaId
            WHERE t.UsuarioId = @UsuarioId
            AND FechaTransaccion BETWEEN @FechaInicio AND @FechaFin
            ORDER BY t.FechaTransaccion DESC ";            
            return await connection.QueryAsync<Transaccion>(query, modelo);

        }

        public async Task<IEnumerable<ResultadoObtenerPorSemana>> ObtenerPorSemana(ParametroObtenerTransaccionesPorUsuario modelo)
        {
            using var connection = new SqlConnection(connectionString);
            var query = @"
                SELECT DATEDIFF(D,@FechaInicio,FechaTransaccion) / 7 + 1 AS SEMANA
                , SUM(Monto) AS Monto
                , cat.TipoOperacionId
                FROM Transacciones
                INNER JOIN Categorias cat
                ON cat.Id = Transacciones.CategoriaId
                WHERE  Transacciones.UsuarioId = @UsuarioId
                AND FechaTransaccion BETWEEN @FechaInicio AND @FechaFin
                GROUP BY DATEDIFF(D,@FechaInicio,FechaTransaccion) / 7 + 1 , cat.TipoOperacionId
            ";
            return await connection.QueryAsync<ResultadoObtenerPorSemana>(query, modelo);

        }


        public async Task<IEnumerable<ResultadoObtenerPorMes>> ObtenerPorMes(int usuarioId,int año)
        {
            using var connection = new SqlConnection(connectionString);
            var query = @"
            SELECT MONTH(FechaTransaccion) as Mes
            ,SUM(Monto) as Monto , cat.TipoOperacionId
            FROM
            Transacciones
            INNER JOIN Categorias cat
            ON cat.Id = Transacciones.CategoriaId
            WHERE Transacciones.UsuarioId = @UsuarioId 
            AND YEAR(FechaTransaccion) = @Año
            GROUP BY MONTH(FechaTransaccion),cat.TipoOperacionId
            ";
            return await connection.QueryAsync<ResultadoObtenerPorMes>(query, new { usuarioId, año });
        }
    }
}