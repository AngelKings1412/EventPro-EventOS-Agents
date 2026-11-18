using System.Security.Claims;
using EventPro.Data;
using EventPro.Models;
using EventPro.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EventPro.Controllers;

[Authorize]
public class TareasController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AiOrchestratorService _orchestrator;

    public TareasController(ApplicationDbContext context, AiOrchestratorService orchestrator)
    {
        _context = context;
        _orchestrator = orchestrator;
    }

    public async Task<IActionResult> Index(int? eventoId, string? estado)
    {
        var eventos = await _context.Eventos.OrderBy(e => e.Nombre).ToListAsync();
        ViewBag.Eventos = new SelectList(eventos, "Id", "Nombre", eventoId);
        ViewBag.EventoIdActual = eventoId ?? eventos.FirstOrDefault()?.Id;
        ViewBag.EstadoActual = estado;

        var query = _context.Tareas.Include(t => t.Evento).AsQueryable();

        if (eventoId.HasValue && eventoId.Value > 0)
            query = query.Where(t => t.EventoId == eventoId.Value);

        if (!string.IsNullOrEmpty(estado))
            query = query.Where(t => t.Estado == estado);

        var tareas = await query.OrderBy(t => t.FechaLimite).ToListAsync();
        return View(tareas);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador,Planificador")]
    public async Task<IActionResult> Create(Tarea tarea)
    {
        if (ModelState.IsValid)
        {
            _context.Tareas.Add(tarea);
            _context.RegistrarBitacora("Tarea", "Tarea programada",
                $"Nueva tarea \"{tarea.Titulo}\" asignada a {tarea.Responsable}.",
                User.Identity?.Name ?? "Usuario", tarea.EventoId);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Tarea programada correctamente.";
        }
        return RedirectToAction(nameof(Index), new { eventoId = tarea.EventoId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador,Planificador")]
    public async Task<IActionResult> SugerirConIa(int eventoId)
    {
        var usuario = User.Identity?.Name ?? "Usuario";
        int? userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int uid) ? uid : null;

        var res = await _orchestrator.EjecutarOrquestacionAsync(new OrchestrationRequest
        {
            EventoId = eventoId,
            AgenteForzado = "planificador",
            Prompt = "Genera e inserta tareas escalonadas en el cronograma verificando disponibilidad de salón."
        }, usuario);

        int agregadas = 0;
        foreach (var p in res.AccionesSugeridas.Where(a => a.Tipo == "agregar_tarea"))
        {
            var (ok, _) = await _orchestrator.AplicarPropuestaAsync(p, userId, usuario);
            if (ok) agregadas++;
        }

        if (agregadas > 0)
            TempData["Success"] = $"Agente Planificador IA generó e insertó {agregadas} tarea(s) en SQL Server ({res.LatenciaMs} ms). {res.VerificacionEspacio.Mensaje}";
        else
            TempData["Success"] = $"Agente Planificador IA verificó el cronograma: todas las tareas recomendadas ya están registradas. {res.VerificacionEspacio.Mensaje}";

        return RedirectToAction(nameof(Index), new { eventoId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador,Planificador")]
    public async Task<IActionResult> CambiarEstado(int id, string nuevoEstado)
    {
        var tarea = await _context.Tareas.FindAsync(id);
        if (tarea != null)
        {
            tarea.Estado = nuevoEstado;
            _context.RegistrarBitacora("Tarea", "Estado de tarea actualizado",
                $"Tarea \"{tarea.Titulo}\" marcada como {nuevoEstado}.",
                User.Identity?.Name ?? "Usuario", tarea.EventoId);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index), new { eventoId = tarea?.EventoId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador,Planificador")]
    public async Task<IActionResult> Delete(int id)
    {
        var tarea = await _context.Tareas.FindAsync(id);
        int? evId = tarea?.EventoId;
        if (tarea != null)
        {
            _context.Tareas.Remove(tarea);
            _context.RegistrarBitacora("Tarea", "Tarea eliminada",
                $"Se eliminó la tarea \"{tarea.Titulo}\".", User.Identity?.Name ?? "Usuario", evId);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Tarea eliminada.";
        }
        return RedirectToAction(nameof(Index), new { eventoId = evId });
    }
}
