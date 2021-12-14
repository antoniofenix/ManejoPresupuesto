using Dapper;
using ManejoPresupuesto.Models;
using ManejoPresupuesto.Servicio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ManejoPresupuesto.Controllers
{
    public class TiposCuentaController : Controller
    {
        private readonly IRepositorioTipoCuenta repositorioTipoCuenta;
        private readonly IServicioUsuario servicioUsuario;

        public TiposCuentaController(IRepositorioTipoCuenta repositorioTipoCuenta,IServicioUsuario servicioUsuario)
        {
            this.repositorioTipoCuenta = repositorioTipoCuenta;
            this.servicioUsuario = servicioUsuario;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            var tiposCuentas = await repositorioTipoCuenta.Obtener(usuarioId);
            return View(tiposCuentas);

        }

        [HttpGet]
        public IActionResult Crear()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            var tiposCuentas = await repositorioTipoCuenta.ObtenerPorId(id,usuarioId);
            if(tiposCuentas is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }
            return View(tiposCuentas);
        }

        [HttpGet]
        public async Task<IActionResult> Borrar(int id)
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            var tiposCuentas = await repositorioTipoCuenta.ObtenerPorId(id, usuarioId);
            if (tiposCuentas is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            return View(tiposCuentas);
        }


        [HttpGet]
        public async Task<IActionResult> VerificarExiteTipoCuenta(string nombre)
        {
            var userId = servicioUsuario.ObtenerUsuarioId();
            var yaExiste = await repositorioTipoCuenta.Existe(nombre, userId);
            if (yaExiste)
            {
                return Json($"El nombre { nombre} ya existe");
            }
            return Json(true);

        }

        [HttpPost]
        public async Task<IActionResult> BorrarTipoCuenta(int id)
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            var tiposCuentas = await repositorioTipoCuenta.ObtenerPorId(id, usuarioId);
            if (tiposCuentas is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }
            await repositorioTipoCuenta.Borrar(id);
            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> Editar(TipoCuenta tipoCuenta)
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            var tiposCuentasExiste = await repositorioTipoCuenta.ObtenerPorId(tipoCuenta.Id, usuarioId);
            if (tiposCuentasExiste is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            await repositorioTipoCuenta.Actualizar(tipoCuenta);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Crear(TipoCuenta tipoCuenta)
        {
            if (!ModelState.IsValid)
            {
                return View(tipoCuenta);
            }

            tipoCuenta.UsuarioId = servicioUsuario.ObtenerUsuarioId();

            var yaExiste = await repositorioTipoCuenta.Existe(tipoCuenta.Nombre, tipoCuenta.UsuarioId);
            if (yaExiste)
            {
                ModelState.AddModelError(nameof(tipoCuenta.Nombre),
                    $"El nombre {tipoCuenta.Nombre} ya existe");
                return View(tipoCuenta);
            }
            await repositorioTipoCuenta.Crear(tipoCuenta);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Ordenar ([FromBody] int [] ids)
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            var tiposCuentas = await repositorioTipoCuenta.Obtener(usuarioId);
            var idsTiposCuentas = tiposCuentas.Select(x => x.Id);

            var idsTiposCuentasNoPertenecenAlUsuario = ids.Except(idsTiposCuentas).ToList();

            if(idsTiposCuentasNoPertenecenAlUsuario.Count > 0)
            {
                return Forbid();
            }

             var tiposCuentasOrdenados = ids.Select((valor, indice) =>
             new TipoCuenta() { Id = valor, Orden = indice + 1 }).AsEnumerable();

            await repositorioTipoCuenta.Ordenar(tiposCuentasOrdenados);
            return Ok();
        }
       
    }
}
