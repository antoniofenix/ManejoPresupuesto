using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel;

namespace ManejoPresupuesto.Models
{
    public class CuentaCreacionViewModel : Cuenta
    {
        [System.ComponentModel.EditorBrowsable(EditorBrowsableState.Never)]
        public IEnumerable<SelectListItem> TiposCuentas { get; set; }        
    }
}
