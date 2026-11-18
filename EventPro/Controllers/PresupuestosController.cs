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
public class PresupuestosController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AiOrchestratorService _orchestrator;

    public PresupuestosController(ApplicationDbContext context, AiOrchestratorService orchestrator)
    {
        _context = context;
        _orchestrator = orchestrator;
    }

    // GET: Presupuestos?eventoId=1
    public async Task<IActionResult> Index(int? eventoId)
    {
        var eventos = await _context.Eventos.OrderBy(e => e.Nombre).ToListAsync();
        ViewBag.Eventos = new SelectList(eventos, "Id", "Nombre", eventoId);
        ViewBag.EventoIdParaIa = eventoId ?? eventos.FirstOrDefault()?.Id;

        var query = _context.RubrosPresupuesto.Include(r => r.Evento).AsQueryable();

        decimal techoAutorizado;
        if (eventoId.HasValue && eventoId.Value > 0)
        {
            query = query.Where(r => r.EventoId == eventoId.Value);
            var evActual = eventos.FirstOrDefault(e => e.Id == eventoId.Value);
            ViewBag.EventoActual = evActual;
            techoAutorizado = evActual?.PresupuestoMaximo ?? 0m;
        }
        else
        {
            techoAutorizado = eventos.Sum(e => e.PresupuestoMaximo);
        }

        var rubros = await query.ToListAsync();
        decimal totalEstimado = rubros.Sum(r => r.MontoEstimado);
        decimal totalReal = rubros.Sum(r => r.MontoReal);

        ViewBag.TotalEstimado = totalEstimado;
        ViewBag.TotalReal = totalReal;
        ViewBag.Diferencia = totalEstimado - totalReal;
        ViewBag.TechoAutorizado = techoAutorizado;

        double pctUso = techoAutorizado > 0 ? Math.Round((double)(totalReal / techoAutorizado) * 100.0, 1) : 0;
        ViewBag.PorcentajeTecho = pctUso;

        return View(rubros);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador,Planificador")]
    public async Task<IActionResult> Create(RubroPresupuesto rubro)
    {
        if (ModelState.IsValid)
        {
            _context.RubrosPresupuesto.Add(rubro);
            _context.RegistrarBitacora("Presupuesto", "Rubro presupuestal agregado",
                $"Rubro \"{rubro.Concepto}\" ({rubro.Categoria}): Estimado ${rubro.MontoEstimado:N2} / Real ${rubro.MontoReal:N2}.",
                User.Identity?.Name ?? "Usuario", rubro.EventoId);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Rubro presupuestal agregado.";
        }
        return RedirectToAction(nameof(Index), new { eventoId = rubro.EventoId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador,Planificador")]
    public async Task<IActionResult> AnalizarConIa(int eventoId)
    {
        var usuario = User.Identity?.Name ?? "Usuario";
        int? userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int uid) ? uid : null;

        var res = await _orchestrator.EjecutarOrquestacionAsync(new OrchestrationRequest
        {
            EventoId = eventoId,
            AgenteForzado = "presupuesto",
            Prompt = "Analiza salud financiera, alertas de límite y sugiere partidas faltantes con proveedores."
        }, usuario);

        int agregados = 0;
        var sugerenciasRubros = res.AccionesSugeridas.Where(a => a.Tipo == "agregar_rubro").Take(1);
        foreach (var p in sugerenciasRubros)
        {
            var (ok, _) = await _orchestrator.AplicarPropuestaAsync(p, userId, usuario);
            if (ok) agregados++;
        }

        TempData["Success"] = $"Agente de Presupuesto IA ({res.LatenciaMs} ms): {res.AlertaPresupuestal} {(agregados > 0 ? $"Se registró {agregados} partida recomendada." : "")}";
        return RedirectToAction(nameof(Index), new { eventoId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador,Planificador")]
    public async Task<IActionResult> Delete(int id)
    {
        var rubro = await _context.RubrosPresupuesto.FindAsync(id);
        int eventoId = 0;
        if (rubro != null)
        {
            eventoId = rubro.EventoId;
            _context.RubrosPresupuesto.Remove(rubro);
            _context.RegistrarBitacora("Presupuesto", "Rubro presupuestal eliminado",
                $"Se eliminó el rubro \"{rubro.Concepto}\".", User.Identity?.Name ?? "Usuario", eventoId);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Rubro eliminado.";
        }
        return RedirectToAction(nameof(Index), new { eventoId = eventoId > 0 ? eventoId : (int?)null });
    }
}
