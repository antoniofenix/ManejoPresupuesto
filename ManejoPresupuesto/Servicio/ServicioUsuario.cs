namespace ManejoPresupuesto.Servicio
{
    public interface IServicioUsuario
    {
        int ObtenerUsuarioId();
    }

    public class ServicioUsuario : IServicioUsuario
    {
        public int ObtenerUsuarioId()
        {
            return 2;
        }
    }
}
