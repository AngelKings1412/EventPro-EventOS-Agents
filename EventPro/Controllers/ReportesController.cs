using System.Text;
using EventPro.Data;
using EventPro.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EventPro.Controllers;

[Authorize]
public class ReportesController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReportesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int? eventoId)
    {
        var todosEventos = await _context.Eventos
            .Include(e => e.RubrosPresupuesto)
            .Include(e => e.Tareas)
            .Include(e => e.Invitados)
            .Include(e => e.EventoProveedores)
                .ThenInclude(ep => ep.Proveedor)
            .OrderBy(e => e.FechaInicio)
            .ToListAsync();

        ViewBag.Eventos = new SelectList(todosEventos, "Id", "Nombre", eventoId);

        var filtrados = (eventoId.HasValue && eventoId.Value > 0)
            ? todosEventos.Where(e => e.Id == eventoId.Value).ToList()
            : todosEventos;

        var resumenes = filtrados.Select(e => new EventoReporteResumen
        {
            EventoId = e.Id,
            Nombre = e.Nombre,
            Tipo = e.Tipo,
            Estado = e.Estado,
            Lugar = e.Lugar,
            FechaInicio = e.FechaInicio,
            Aforo = e.Aforo,
            PresupuestoMaximo = e.PresupuestoMaximo,
            TotalEstimado = e.RubrosPresupuesto.Sum(r => r.MontoEstimado),
            TotalReal = e.RubrosPresupuesto.Sum(r => r.MontoReal),
            TareasTotales = e.Tareas.Count,
            TareasCompletadas = e.Tareas.Count(t => t.Estado == "Completada"),
            InvitadosTotales = e.Invitados.Count,
            InvitadosConfirmados = e.Invitados.Count(i => i.Confirmacion == "Confirmado")
        }).ToList();

        var tareas = filtrados.SelectMany(e => e.Tareas).ToList();
        var invitados = filtrados.SelectMany(e => e.Invitados).ToList();
        var rubros = filtrados.SelectMany(e => e.RubrosPresupuesto).ToList();
        var contrataciones = filtrados.SelectMany(e => e.EventoProveedores).ToList();

        var vm = new ReportesViewModel
        {
            EventoIdSeleccionado = eventoId,
            TotalEventos = filtrados.Count,
            TotalPresupuestoMaximo = filtrados.Sum(e => e.PresupuestoMaximo),
            TotalEstimado = rubros.Sum(r => r.MontoEstimado),
            TotalReal = rubros.Sum(r => r.MontoReal),
            TotalTareas = tareas.Count,
            TareasCompletadas = tareas.Count(t => t.Estado == "Completada"),
            TareasEnProceso = tareas.Count(t => t.Estado == "En proceso"),
            TareasPendientes = tareas.Count(t => t.Estado == "Pendiente" && t.FechaLimite >= DateTime.Today),
            TareasVencidas = tareas.Count(t => t.Estado != "Completada" && t.FechaLimite < DateTime.Today),
            InvitadosConfirmados = invitados.Count(i => i.Confirmacion == "Confirmado"),
            InvitadosPendientes = invitados.Count(i => i.Confirmacion == "Pendiente"),
            InvitadosNoAsistira = invitados.Count(i => i.Confirmacion == "No asistirá"),
            TotalIntervencionesIa = await _context.Bitacoras.CountAsync(b => b.Tipo == "Agente" && (!eventoId.HasValue || b.EventoId == eventoId.Value)),
            ResumenPorEvento = resumenes,
            Rubros = rubros,
            Contrataciones = contrataciones,
            Invitados = invitados
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> ExportarCsv(int? eventoId)
    {
        var query = _context.Eventos
            .Include(e => e.RubrosPresupuesto)
            .Include(e => e.Tareas)
            .Include(e => e.Invitados)
            .AsQueryable();

        if (eventoId.HasValue && eventoId.Value > 0)
            query = query.Where(e => e.Id == eventoId.Value);

        var eventos = await query.OrderBy(e => e.FechaInicio).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("ID,Evento,Tipo,Estado,Lugar,FechaInicio,Aforo,PresupuestoMaximo,TotalEstimado,TotalReal,SaldoDisponible,TareasCompletadas,TareasTotales,InvitadosConfirmados,InvitadosTotales");

        foreach (var e in eventos)
        {
            var est = e.RubrosPresupuesto.Sum(r => r.MontoEstimado);
            var real = e.RubrosPresupuesto.Sum(r => r.MontoReal);
            var saldo = e.PresupuestoMaximo - real;
            var tc = e.Tareas.Count(t => t.Estado == "Completada");
            var ic = e.Invitados.Count(i => i.Confirmacion == "Confirmado");

            sb.AppendLine($"{e.Id},\"{e.Nombre.Replace("\"", "\"\"")}\",{e.Tipo},{e.Estado},\"{e.Lugar}\",{e.FechaInicio:yyyy-MM-dd},{e.Aforo},{e.PresupuestoMaximo:F2},{est:F2},{real:F2},{saldo:F2},{tc},{e.Tareas.Count},{ic},{e.Invitados.Count}");
        }

        _context.RegistrarBitacora("Sistema", "Exportación de Reporte CSV",
            $"Se descargó el reporte estadístico CSV ({eventos.Count} evento(s)).",
            User.Identity?.Name ?? "Usuario", eventoId);
        await _context.SaveChangesAsync();

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"Reporte_EventPro_{DateTime.Now:yyyyMMdd_HHmm}.csv");
    }
}
