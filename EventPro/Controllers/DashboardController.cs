using EventPro.Data;
using EventPro.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventPro.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var eventos = await _context.Eventos
            .Include(e => e.Cliente)
            .Include(e => e.Organizador)
            .Include(e => e.RubrosPresupuesto)
            .Include(e => e.Tareas)
            .OrderBy(e => e.FechaInicio)
            .ToListAsync();

        var tareas = await _context.Tareas
            .Include(t => t.Evento)
            .ToListAsync();

        var totalComprometido = await _context.RubrosPresupuesto
            .SumAsync(r => (decimal?)r.MontoReal) ?? 0;

        var intervencionesIa = await _context.Bitacoras.CountAsync(b => b.Tipo == "Agente");
        var docsRag = await _context.DocumentosConocimiento.CountAsync();
        var confirmados = await _context.Invitados.CountAsync(i => i.Confirmacion == "Confirmado");
        var ultimasIa = await _context.Bitacoras
            .Where(b => b.Tipo == "Agente")
            .OrderByDescending(b => b.Fecha)
            .Take(5)
            .ToListAsync();

        var vm = new DashboardViewModel
        {
            EventosActivos = eventos.Count(e => e.Estado == "Planificado" || e.Estado == "En coordinación"),
            EventosPorIniciar = eventos.Count(e => e.FechaInicio >= DateTime.Today && e.FechaInicio <= DateTime.Today.AddDays(15)),
            PresupuestoComprometido = totalComprometido,
            TareasVencidas = tareas.Count(t => t.FechaLimite < DateTime.Today && t.Estado != "Completada"),
            IntervencionesIa = intervencionesIa,
            DocumentosRag = docsRag,
            InvitadosConfirmados = confirmados,
            ProximosEventos = eventos.Take(5).ToList(),
            TareasPorVencer = tareas
                .Where(t => t.Estado != "Completada")
                .OrderBy(t => t.FechaLimite)
                .Take(5)
                .ToList(),
            UltimasBitacorasIa = ultimasIa
        };

        return View(vm);
    }
}
