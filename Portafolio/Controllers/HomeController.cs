using Microsoft.AspNetCore.Mvc;
using Portafolio.Models;
using Portafolio.Servicios;
using System.Diagnostics;

namespace Portafolio.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IRepositorioProyectos _repositorio;
        private readonly IServiciosEmail _serviciosEmail;

        public HomeController(ILogger<HomeController> logger, IRepositorioProyectos repositorio,IServiciosEmail serviciosEmail)
        {
            _logger = logger;
            _repositorio = repositorio;
            _serviciosEmail = serviciosEmail;
        }

        // Trace
        // Debug
        // Information
        // Warning
        // Error
        // Critical

        public IActionResult Index()
        {
            var proyectos = _repositorio.ObtenerProyectos().Take(3).ToList();
            var modelo = new HomeIndexViewModel() { Proyectos = proyectos };
            return View(modelo);
        }


        public IActionResult Proyectos()
        {
            var proyectos = _repositorio.ObtenerProyectos();
            return View(proyectos);
        }

        [HttpGet]
        public IActionResult Contacto()
        {            
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Contacto(ContactoViewModel contactoViewModel)
        {
            await _serviciosEmail.Enviar(contactoViewModel);
            return RedirectToAction("Gracias");
        }

        public IActionResult Gracias()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}