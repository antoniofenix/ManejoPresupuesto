﻿using AutoMapper;
using ClosedXML.Excel;
using ManejoPresupuesto.Models;
using ManejoPresupuesto.Servicio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;

namespace ManejoPresupuesto.Controllers
{
    public class TransaccionesController : Controller
    {
        private readonly IServicioUsuario servicioUsuario;
        private readonly IRepositorioCuentas repositorioCuentas;
        private readonly IRepositorioCategorias repositorioCategorias;
        private readonly IRepositorioTransacciones repositorioTransacciones;
        private readonly IMapper mapper;
        private readonly IServicioReportes servicioReportes;

        public TransaccionesController(IServicioUsuario servicioUsuario, IRepositorioCuentas repositorioCuentas
            , IRepositorioCategorias repositorioCategorias, IRepositorioTransacciones repositorioTransacciones
            , IMapper mapper, IServicioReportes servicioReportes)
        {
            this.servicioUsuario = servicioUsuario;
            this.repositorioCuentas = repositorioCuentas;
            this.repositorioCategorias = repositorioCategorias;
            this.repositorioTransacciones = repositorioTransacciones;
            this.mapper = mapper;
            this.servicioReportes = servicioReportes;
        }
        [HttpGet]
        public async Task<IActionResult> Index(int mes, int año)
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            var modelo = await servicioReportes.ObtenerTransaccionesDetalladas(usuarioId, mes, año, ViewBag);
            return View(modelo);
        }

        [HttpGet]
        public async Task<IActionResult> Semanal(int mes, int año)
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            IEnumerable<ResultadoObtenerPorSemana> transaccionesPorSemana = await servicioReportes.ObtenerReporteSemanal(usuarioId, mes, año, ViewBag);


            var agrupado = transaccionesPorSemana.GroupBy(x => x.Semana).Select(x => new ResultadoObtenerPorSemana()
            {
                Semana = x.Key,
                Ingresos = x.Where(x => x.TipoOperacionId == TipoOperacion.Ingreso)
                .Select(x => x.Monto).FirstOrDefault(),
                Gastos = x.Where(x => x.TipoOperacionId == TipoOperacion.Gasto)
                .Select(x => x.Monto).FirstOrDefault()
            }).ToList();

            if (año == 0 || mes == 0)
            {
                var hoy = DateTime.Today;
                año = hoy.Year;
                mes = hoy.Month;
            }

            var fechareferencia = new DateTime(año, mes, 1);
            var diasDelMes = Enumerable.Range(1, fechareferencia.AddMonths(1).AddDays(-1).Day);

            var diasSegmentados = diasDelMes.Chunk(7).ToList();


            for (int i = 0; i < diasSegmentados.Count(); i++)
            {
                var semana = i + 1;
                var fechaInicio = new DateTime(año, mes, diasSegmentados[i].First());
                var fechaFin = new DateTime(año, mes, diasSegmentados[i].Last());
                var grupoSemana = agrupado.FirstOrDefault(x => x.Semana == semana);

                if (grupoSemana is null)
                {
                    agrupado.Add(new ResultadoObtenerPorSemana()
                    {
                        Semana = semana,
                        FechaInicio = fechaInicio,
                        FechaFin = fechaFin
                    });
                }
                else
                {
                    grupoSemana.FechaInicio = fechaInicio;
                    grupoSemana.FechaFin = fechaFin;
                }
            }

            agrupado = agrupado.OrderByDescending(x => x.Semana).ToList();

            var modelo = new ReporteSemanaViewModel();
            modelo.TransaccionesPorSemana = agrupado;
            modelo.FechaReferencia = fechareferencia;

            return View(modelo);
        }

        [HttpGet]
        public async Task<IActionResult> Mensual(int año)
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            if (año == 0)
            {
                año = DateTime.Today.Year;
            }
            var transaccionesPorMes = await repositorioTransacciones.ObtenerPorMes(usuarioId, año);
            var transaccionesAgrupadas = transaccionesPorMes.GroupBy(x => x.Mes)
                .Select(x => new ResultadoObtenerPorMes()
                {
                    Mes = x.Key,
                    Ingreso = x.Where(x => x.TipoOperacionId == TipoOperacion.Ingreso).Select(x => x.Monto).FirstOrDefault(),
                    Gasto = x.Where(x => x.TipoOperacionId == TipoOperacion.Gasto).Select(x => x.Monto).FirstOrDefault()

                }).ToList();

            for (int mes = 1; mes <= 12; mes++)
            {
                var transaccion = transaccionesAgrupadas.FirstOrDefault(x => x.Mes == mes);
                var fechaReferencia = new DateTime(año, mes, 1);
                if (transaccion is null)
                {
                    transaccionesAgrupadas.Add(new ResultadoObtenerPorMes()
                    {
                        Mes = mes,
                        FechaReferencia = fechaReferencia,
                    });
                }
                else
                {
                    transaccion.FechaReferencia = fechaReferencia;
                }
            }

            transaccionesAgrupadas = transaccionesAgrupadas.OrderByDescending(x => x.Mes).ToList();
            var modelo = new ReporteMensualViewModel()
            {
                Año = año,
                TransaccionesPorMes = transaccionesAgrupadas
            };

            return View(modelo);
        }

        [HttpGet]
        public IActionResult ExcelReporte()
        {
            return View();
        }


        [HttpGet]
        public async Task<FileResult> ExportarExcelPorMes(int mes, int año)
        {
            var fechaInicio = new DateTime(año, mes, 1);
            var fechaFin = fechaInicio.AddMonths(1).AddDays(-1);
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            var transacciones = await repositorioTransacciones.ObtenerPorUsuarioId(new ParametroObtenerTransaccionesPorUsuario()
            {
                UsuarioId = usuarioId,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin
            });
            var nombreArchivo = $"Manejo Presupuesto - {fechaInicio.ToString("MMM yyy")}.xlsx";
            return GenerarExcel(nombreArchivo, transacciones);
        }



        [HttpGet]
        public async Task<FileResult> ExportarExcelPorAño(int año)
        {
            var fechaInicio = new DateTime(año, 1, 1);
            var fechaFin = fechaInicio.AddYears(1).AddDays(-1);
            var usuarioId = servicioUsuario.ObtenerUsuarioId();

            var transacciones = await repositorioTransacciones.ObtenerPorUsuarioId(new ParametroObtenerTransaccionesPorUsuario()
            {
                UsuarioId = usuarioId,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin
            });

            var nombreArchivo = $"Manejo Presupuesto - {fechaInicio.ToString("yyyy")}.xlsx";
            return GenerarExcel(nombreArchivo, transacciones);
        }


        [HttpGet]
        public async Task<FileResult> ExportarExcelTodo()
        {
            var fechaInicio = DateTime.Today.AddYears(-100);
            var fechaFin = DateTime.Today.AddYears(1000);
            var usuarioId = servicioUsuario.ObtenerUsuarioId();

            var transacciones = await repositorioTransacciones.ObtenerPorUsuarioId(new ParametroObtenerTransaccionesPorUsuario()
            {
                UsuarioId = usuarioId,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin
            });

            var nombreArchivo = $"Manejo Presupuesto - { DateTime.Today.ToString("dd-MM-yyyyy")}.xlsx";
            return GenerarExcel(nombreArchivo, transacciones);

        }

        private FileResult GenerarExcel(string nombreArchivo, IEnumerable<Transaccion> transacciones)
        {
            DataTable dataTable = new DataTable("Transacciones");
            dataTable.Columns.AddRange(new DataColumn[] {
            new DataColumn("Fecha"),
            new DataColumn("Cuenta"),
            new DataColumn("Categoria"),
            new DataColumn("Nota"),
            new DataColumn("Monto"),
            new DataColumn("Ingreso/Gasto")
            });


            foreach (var transaccion in transacciones)
            {
                dataTable.Rows.Add(transaccion.FechaTransaccion,
                    transaccion.Cuenta,
                    transaccion.Categoria,
                    transaccion.Nota,
                    transaccion.Monto,
                    transaccion.TipoOperacionId);
            }

            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(dataTable);
                using (MemoryStream ms = new MemoryStream())
                {
                    wb.SaveAs(ms);
                    return File(ms.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        nombreArchivo);
                }
            }
        }
        
        [HttpGet]
        public IActionResult Calendario()
        {
            return View();
        }



        [HttpGet]
        public async Task<JsonResult> ObtenerTransaccionesCalendario(DateTime start, DateTime end)
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            var transacciones = await repositorioTransacciones.ObtenerPorUsuarioId(new ParametroObtenerTransaccionesPorUsuario()
            {
                UsuarioId = usuarioId,
                FechaInicio = start,
                FechaFin = end
            });


            var eventosCalendario = transacciones.Select(transaccion => new EventoCalendario()
            {
                Title = transaccion.Monto.ToString("N"),
                Start = transaccion.FechaTransaccion.ToString("yyyy-MM-dd"),
                End = transaccion.FechaTransaccion.ToString("yyyy-MM-dd"),
                // operador ternario
                Color = (transaccion.TipoOperacionId == TipoOperacion.Gasto) ? "Red" : null
            });

            return Json(eventosCalendario);
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerTransaccionesPorFecha(DateTime fecha)
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            var transacciones = await repositorioTransacciones.ObtenerPorUsuarioId(new ParametroObtenerTransaccionesPorUsuario()
            {
                UsuarioId = usuarioId,
                FechaInicio = fecha,
                FechaFin = fecha
            });

            return Json(transacciones);
        }


        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            var modelo = new TransaccionCreacionViewModel();
            modelo.Cuentas = await ObtenerCuentas(usuarioId);
            modelo.Categorias = await ObtenerCategorias(usuarioId, modelo.TipoOperacionId);
            return View(modelo);
        }


        [HttpPost]
        public async Task<IActionResult> Crear(TransaccionCreacionViewModel modelo)
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            if (!ModelState.IsValid)
            {
                modelo.Cuentas = await ObtenerCuentas(usuarioId);
                modelo.Categorias = await ObtenerCategorias(usuarioId, modelo.TipoOperacionId);
                return View(modelo);
            }

            var cuenta = await repositorioCuentas.ObtenerPorId(modelo.CuentaId, usuarioId);
            if (cuenta is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }


            var categoria = await repositorioCategorias.ObtenerPorId(modelo.CategoriaId, usuarioId);
            if (categoria is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            modelo.UsuarioId = usuarioId;

            if (modelo.TipoOperacionId == TipoOperacion.Gasto)
            {
                modelo.Monto *= -1;
            }


            await repositorioTransacciones.Crear(modelo);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id, string urlRetorno = null)
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            var transaccion = await repositorioTransacciones.ObtenerPorId(id, usuarioId);
            if (transaccion is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            var modelo = mapper.Map<TransaccionActualizacionViewModel>(transaccion);
            modelo.MontoAnterior = modelo.Monto;

            if (modelo.TipoOperacionId == TipoOperacion.Gasto)
            {
                modelo.MontoAnterior = modelo.Monto * -1;
            }

            modelo.CuentaAnteriorId = transaccion.CuentaId;
            modelo.Categorias = await ObtenerCategorias(usuarioId, transaccion.TipoOperacionId);
            modelo.Cuentas = await ObtenerCuentas(usuarioId);
            modelo.UrlRetorno = urlRetorno;
            return View(modelo);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(TransaccionActualizacionViewModel modelo)
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            if (!ModelState.IsValid)
            {
                modelo.Cuentas = await ObtenerCuentas(usuarioId);
                modelo.Categorias = await ObtenerCategorias(usuarioId, modelo.TipoOperacionId);
                return View(modelo);
            }

            var cuenta = await repositorioCuentas.ObtenerPorId(modelo.CuentaId, usuarioId);
            if (cuenta is null)
            {
                return View("NoEncontrado", "Home");
            }

            var categorias = await repositorioCategorias.ObtenerPorId(modelo.CategoriaId, usuarioId);
            if (cuenta is null)
            {
                return View("NoEncontrado", "Home");
            }

            var transaccion = mapper.Map<Transaccion>(modelo);
            if (transaccion.TipoOperacionId == TipoOperacion.Gasto)
            {
                transaccion.Monto *= -1;

            }

            await repositorioTransacciones.Actualizar(transaccion,
                modelo.MontoAnterior, modelo.CuentaAnteriorId);



            if (string.IsNullOrEmpty(modelo.UrlRetorno))
            {
                return RedirectToAction("Index");
            }
            else
            {
                return LocalRedirect(modelo.UrlRetorno);
            }
        }





        [HttpGet]
        public async Task<IEnumerable<SelectListItem>> ObtenerCuentas(int usuarioId)
        {
            var cuentas = await repositorioCuentas.Buscar(usuarioId);
            return cuentas.Select(x => new SelectListItem(x.Nombre, x.Id.ToString()));
        }


        private async Task<IEnumerable<SelectListItem>> ObtenerCategorias(int usuarioId, TipoOperacion tipoOperacion)
        {
            var categorias = await repositorioCategorias.Obtener(usuarioId, tipoOperacion);
            return categorias.Select(x => new SelectListItem(x.Nombre, x.Id.ToString()));
        }

        [HttpPost]
        public async Task<IActionResult> ObtenerCategorias([FromBody] TipoOperacion tipoOperacion)
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            var categorias = await ObtenerCategorias(usuarioId, tipoOperacion);
            return Ok(categorias);
        }


        [HttpPost]
        public async Task<IActionResult> Borrar(int id, string urlRetorno = null)
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            var transaccion = await repositorioTransacciones.ObtenerPorId(id, usuarioId);
            if (transaccion is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            await repositorioTransacciones.Borrar(id);

            if (string.IsNullOrEmpty(urlRetorno))
            {
                return RedirectToAction("Index");
            }
            else
            {
                return LocalRedirect(urlRetorno);
            }
        }
    }
}
