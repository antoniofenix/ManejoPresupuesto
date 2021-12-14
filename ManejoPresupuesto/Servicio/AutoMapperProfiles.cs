using AutoMapper;
using ManejoPresupuesto.Models;

namespace ManejoPresupuesto.Servicio
{
    public class AutoMapperProfiles :Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<IRepositorioCuentas, CuentaCreacionViewModel>();
            CreateMap<TransaccionActualizacionViewModel, Transaccion>().ReverseMap();
        }
    }
}
