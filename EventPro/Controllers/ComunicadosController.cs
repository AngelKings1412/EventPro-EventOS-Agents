using System.Security.Claims;
using System.Text.Json;
using EventPro.Data;
using EventPro.Models;
using EventPro.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EventPro.Controllers;

[Authorize]
public class ComunicadosController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AiOrchestratorService _orchestrator;

    public ComunicadosController(ApplicationDbContext context, AiOrchestratorService orchestrator)
    {
        _context = context;
        _orchestrator = orchestrator;
    }

    public async Task<IActionResult> Index(int? eventoId, string? canal, int? cargarId)
    {
        var eventos = await _context.Eventos.OrderBy(e => e.Nombre).ToListAsync();
        ViewBag.EventosMetaJson = JsonSerializer.Serialize(eventos.Select(e => new
        {
            e.Id,
            e.Nombre,
            e.Tipo,
            Fecha = $"{e.FechaInicio:dd/MM/yyyy} al {e.FechaFin:dd/MM/yyyy}",
            e.Lugar,
            e.Aforo,
            e.Descripcion
        }));

        var allComunicados = await _context.Comunicados
            .Include(c => c.Evento)
            .Include(c => c.EnviadoPor)
            .OrderByDescending(c => c.FechaEnvio)
            .ToListAsync();

        ViewBag.TotalComunicados = allComunicados.Count;
        ViewBag.TotalCorreo = allComunicados.Count(c => c.Canal.Contains("Correo", StringComparison.OrdinalIgnoreCase));
        ViewBag.TotalWhatsAppSms = allComunicados.Count(c =>
            c.Canal.Contains("WhatsApp", StringComparison.OrdinalIgnoreCase) ||
            c.Canal.Contains("SMS", StringComparison.OrdinalIgnoreCase));
        ViewBag.TotalCirculares = allComunicados.Count(c => c.Canal.Contains("Circular", StringComparison.OrdinalIgnoreCase));

        // Cargar borrador desde historial (Reutilizar) si viene cargarId
        int? draftEventoId = TempData["DraftEventoId"] as int?;
        if (cargarId.HasValue)
        {
            var previo = allComunicados.FirstOrDefault(c => c.Id == cargarId.Value);
            if (previo != null)
            {
                draftEventoId = previo.EventoId;
                ViewBag.DraftCanal = previo.Canal;
                ViewBag.DraftDestinatarios = previo.Destinatarios;
                ViewBag.DraftAsunto = previo.Asunto;
                ViewBag.DraftContenido = previo.Contenido;
                ViewBag.DraftOrigen = "Cargado desde historial";
            }
        }
        else if (TempData["DraftAsunto"] != null)
        {
            ViewBag.DraftCanal = TempData["DraftCanal"] as string ?? "Correo electrónico";
            ViewBag.DraftDestinatarios = TempData["DraftDestinatarios"] as string ?? "Todos los invitados";
            ViewBag.DraftTipo = TempData["DraftTipo"] as string ?? "invitacion";
            ViewBag.DraftTono = TempData["DraftTono"] as string ?? "Institucional y cordial";
            ViewBag.DraftNotasExtra = TempData["DraftNotasExtra"] as string ?? "";
            ViewBag.DraftAsunto = TempData["DraftAsunto"] as string ?? "";
            ViewBag.DraftContenido = TempData["DraftContenido"] as string ?? "";
            ViewBag.DraftOrigen = "Agente de Comunicación IA";
        }

        int? eventoEditor = draftEventoId ?? eventoId ?? eventos.FirstOrDefault()?.Id;
        ViewBag.EventosFiltro = new SelectList(eventos, "Id", "Nombre", eventoId);
        ViewBag.EventosEditor = new SelectList(eventos, "Id", "Nombre", eventoEditor);
        ViewBag.EventoIdFiltro = eventoId;
        ViewBag.CanalFiltro = canal;

        var filtrados = allComunicados.AsEnumerable();
        if (eventoId.HasValue && eventoId.Value > 0)
            filtrados = filtrados.Where(c => c.EventoId == eventoId.Value);

        if (!string.IsNullOrWhiteSpace(canal))
            filtrados = filtrados.Where(c => c.Canal.Equals(canal, StringComparison.OrdinalIgnoreCase));

        return View(filtrados.ToList());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador,Planificador")]
    public async Task<IActionResult> Create(Comunicado comunicado)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Verifica que el evento, asunto y contenido del comunicado sean válidos.";
            return RedirectToAction(nameof(Index), new { eventoId = comunicado.EventoId });
        }

        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
        {
            comunicado.EnviadoPorId = userId;
        }

        comunicado.FechaEnvio = DateTime.Now;
        if (string.IsNullOrWhiteSpace(comunicado.Destinatarios))
            comunicado.Destinatarios = "Todos los invitados";

        // Calcular número real de destinatarios desde SQL Server según la lista seleccionada
        int conteoDestinatarios = await CalcularDestinatariosRealesAsync(comunicado.EventoId, comunicado.Destinatarios);
        comunicado.EstadoEnvio = $"Enviado ({conteoDestinatarios} destinatario(s))";

        _context.Comunicados.Add(comunicado);
        _context.RegistrarBitacora(
            "Comunicado",
            "Comunicado enviado",
            $"Asunto: \"{comunicado.Asunto}\" | Canal: {comunicado.Canal} | Audiencia: {comunicado.Destinatarios} ({conteoDestinatarios} destinatarios).",
            User.Identity?.Name ?? "Usuario",
            comunicado.EventoId);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Comunicado \"{comunicado.Asunto}\" registrado y enviado vía {comunicado.Canal} a {conteoDestinatarios} destinatario(s).";
        return RedirectToAction(nameof(Index), new { eventoId = comunicado.EventoId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador,Planificador")]
    public async Task<IActionResult> RedactarConIa(
        int eventoId,
        string tipoComunicado = "invitacion",
        string canal = "Correo electrónico",
        string tono = "Institucional y cordial",
        string destinatario = "Todos los invitados",
        string? notasExtra = "")
    {
        var usuario = User.Identity?.Name ?? "Usuario";
        var res = await _orchestrator.EjecutarOrquestacionAsync(new OrchestrationRequest
        {
            EventoId = eventoId,
            AgenteForzado = "comunicacion",
            TipoComunicado = tipoComunicado,
            Canal = canal,
            Tono = tono,
            Destinatario = string.IsNullOrWhiteSpace(destinatario) ? "Comunidad estudiantil y docente del ITESCA" : destinatario,
            NotasExtra = notasExtra ?? string.Empty,
            Prompt = $"Redacta comunicado tipo {tipoComunicado} dirigido a {destinatario} por {canal} con tono {tono}"
        }, usuario);

        var borrador = res.AccionesSugeridas.FirstOrDefault(a => a.Tipo == "crear_comunicado");
        if (borrador != null)
        {
            TempData["DraftEventoId"] = borrador.EventoId;
            TempData["DraftCanal"] = borrador.Canal;
            TempData["DraftDestinatarios"] = destinatario;
            TempData["DraftTipo"] = tipoComunicado;
            TempData["DraftTono"] = tono;
            TempData["DraftNotasExtra"] = notasExtra ?? string.Empty;
            TempData["DraftAsunto"] = borrador.Titulo;
            TempData["DraftContenido"] = borrador.Descripcion;
            TempData["Success"] = $"Agente de Comunicación IA ({res.LatenciaMs} ms) generó el borrador ({tipoComunicado}) para {canal}.";
        }
        return RedirectToAction(nameof(Index), new { eventoId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador,Planificador")]
    public async Task<IActionResult> Reenviar(int id)
    {
        var orig = await _context.Comunicados.FindAsync(id);
        if (orig == null)
        {
            TempData["Error"] = "No se encontró el comunicado a reenviar.";
            return RedirectToAction(nameof(Index));
        }

        int? userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int uid) ? uid : null;
        int conteo = await CalcularDestinatariosRealesAsync(orig.EventoId, orig.Destinatarios);

        var copia = new Comunicado
        {
            EventoId = orig.EventoId,
            Asunto = orig.Asunto.StartsWith("[Reenvío]") ? orig.Asunto : $"[Reenvío] {orig.Asunto}",
            Contenido = orig.Contenido,
            Canal = orig.Canal,
            Destinatarios = orig.Destinatarios,
            EstadoEnvio = $"Reenviado ({conteo} destinatario(s))",
            FechaEnvio = DateTime.Now,
            EnviadoPorId = userId
        };

        _context.Comunicados.Add(copia);
        _context.RegistrarBitacora(
            "Comunicado",
            "Comunicado reenviado",
            $"Reenvío de \"{orig.Asunto}\" vía {orig.Canal} a {conteo} destinatario(s).",
            User.Identity?.Name ?? "Usuario",
            orig.EventoId);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Comunicado reenviado exitosamente vía {orig.Canal} a {conteo} destinatario(s).";
        return RedirectToAction(nameof(Index), new { eventoId = orig.EventoId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _context.Comunicados.FindAsync(id);
        int? evId = c?.EventoId;
        if (c != null)
        {
            _context.Comunicados.Remove(c);
            _context.RegistrarBitacora(
                "Comunicado",
                "Comunicado eliminado",
                $"Se eliminó del historial el comunicado \"{c.Asunto}\".",
                User.Identity?.Name ?? "Usuario",
                evId);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Comunicado eliminado del historial.";
        }
        return RedirectToAction(nameof(Index), new { eventoId = evId });
    }

    private async Task<int> CalcularDestinatariosRealesAsync(int eventoId, string destinatarios)
    {
        var destLower = (destinatarios ?? string.Empty).ToLowerInvariant();
        if (destLower.Contains("confirmad"))
        {
            return await _context.Invitados.CountAsync(i => i.EventoId == eventoId && i.Confirmacion == "Confirmado");
        }
        if (destLower.Contains("pendient"))
        {
            return await _context.Invitados.CountAsync(i => i.EventoId == eventoId && i.Confirmacion == "Pendiente");
        }
        if (destLower.Contains("proveedor"))
        {
            return await _context.EventoProveedores.CountAsync(ep => ep.EventoId == eventoId);
        }

        int totalInvitados = await _context.Invitados.CountAsync(i => i.EventoId == eventoId);
        if (totalInvitados > 0) return totalInvitados;

        var ev = await _context.Eventos.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventoId);
        return ev != null && ev.Aforo > 0 ? ev.Aforo : 1;
    }
}
