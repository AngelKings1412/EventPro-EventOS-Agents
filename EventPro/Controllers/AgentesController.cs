using System.Security.Claims;
using System.Text.Json;
using EventPro.Data;
using EventPro.Models;
using EventPro.Models.ViewModels;
using EventPro.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EventPro.Controllers;

[Authorize(Roles = "Administrador,Planificador")]
public class AgentesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AiOrchestratorService _orchestrator;

    public AgentesController(ApplicationDbContext context, AiOrchestratorService orchestrator)
    {
        _context = context;
        _orchestrator = orchestrator;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? eventoId, string? agente)
    {
        var vm = new AgentesConsoleViewModel();
        vm.Request.EventoId = eventoId;
        if (!string.IsNullOrWhiteSpace(agente))
        {
            vm.Request.AgenteForzado = agente;
        }

        await CargarDatosVistaAsync(vm);

        // Ejecutar diagnóstico inicial automático si ya viene seleccionado eventoId o tomando el primer evento activo
        if (vm.Request.EventoId.HasValue)
        {
            var usuario = User.Identity?.Name ?? "Usuario";
            vm.Resultado = await _orchestrator.EjecutarOrquestacionAsync(vm.Request, usuario);
            vm.PropuestasJson = JsonSerializer.Serialize(vm.Resultado.AccionesSugeridas);
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Orquestar(AgentesConsoleViewModel vm)
    {
        var usuario = User.Identity?.Name ?? "Usuario";
        vm.Resultado = await _orchestrator.EjecutarOrquestacionAsync(vm.Request, usuario);
        vm.PropuestasJson = JsonSerializer.Serialize(vm.Resultado.AccionesSugeridas);
        await CargarDatosVistaAsync(vm);
        return View("Index", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador,Planificador")]
    public async Task<IActionResult> AplicarAccion(AgentActionProposal propuesta, string? returnPrompt, string? returnAgente)
    {
        int? userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int uid) ? uid : null;
        var usuario = User.Identity?.Name ?? "Usuario";

        var (ok, mensaje) = await _orchestrator.AplicarPropuestaAsync(propuesta, userId, usuario);
        TempData[ok ? "Success" : "Error"] = mensaje;

        var vm = new AgentesConsoleViewModel
        {
            Request = new OrchestrationRequest
            {
                EventoId = propuesta.EventoId,
                AgenteForzado = string.IsNullOrWhiteSpace(returnAgente) ? "multi-agente" : returnAgente,
                Prompt = returnPrompt ?? string.Empty
            }
        };
        vm.Resultado = await _orchestrator.EjecutarOrquestacionAsync(vm.Request, usuario);
        vm.PropuestasJson = JsonSerializer.Serialize(vm.Resultado.AccionesSugeridas);
        await CargarDatosVistaAsync(vm);
        return View("Index", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador,Planificador")]
    public async Task<IActionResult> AplicarTodas(string propuestasJson, int eventoId)
    {
        int? userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int uid) ? uid : null;
        var usuario = User.Identity?.Name ?? "Usuario";

        var lista = string.IsNullOrWhiteSpace(propuestasJson)
            ? new List<AgentActionProposal>()
            : JsonSerializer.Deserialize<List<AgentActionProposal>>(propuestasJson) ?? new List<AgentActionProposal>();

        int aplicadas = 0;
        foreach (var p in lista)
        {
            var (ok, _) = await _orchestrator.AplicarPropuestaAsync(p, userId, usuario);
            if (ok) aplicadas++;
        }

        TempData["Success"] = $"Se ejecutaron {aplicadas} propuesta(s) del Orquestador Multi-Agente directamente en SQL Server.";
        return RedirectToAction(nameof(Index), new { eventoId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador,Planificador")]
    public async Task<IActionResult> AgregarDocumentoRag(DocumentoConocimiento doc, int? eventoId)
    {
        if (ModelState.IsValid)
        {
            doc.FechaActualizacion = DateTime.Today;
            _context.DocumentosConocimiento.Add(doc);
            _context.RegistrarBitacora("Agente", "Documento agregado a Base RAG",
                $"Se incorporó \"{doc.Titulo}\" ({doc.Categoria}) a DocumentosConocimiento.",
                User.Identity?.Name ?? "Usuario", eventoId);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Documento \"{doc.Titulo}\" incorporado al motor RAG.";
        }
        return RedirectToAction(nameof(Index), new { eventoId });
    }

    private async Task CargarDatosVistaAsync(AgentesConsoleViewModel vm)
    {
        var eventos = await _context.Eventos.OrderBy(e => e.FechaInicio).ToListAsync();
        if (!vm.Request.EventoId.HasValue && eventos.Any())
        {
            vm.Request.EventoId = eventos.First().Id;
        }
        ViewBag.Eventos = new SelectList(eventos, "Id", "Nombre", vm.Request.EventoId);
        vm.DocumentosRag = await _context.DocumentosConocimiento.OrderByDescending(d => d.FechaActualizacion).ToListAsync();
    }
}
