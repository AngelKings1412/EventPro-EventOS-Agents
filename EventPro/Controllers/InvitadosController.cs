using System.Text;
using EventPro.Data;
using EventPro.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EventPro.Controllers;

[Authorize]
public class InvitadosController : Controller
{
    private readonly ApplicationDbContext _context;

    public InvitadosController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int? eventoId, string? confirmacion)
    {
        var eventos = await _context.Eventos.OrderBy(e => e.Nombre).ToListAsync();
        ViewBag.Eventos = new SelectList(eventos, "Id", "Nombre", eventoId);
        ViewBag.EventoIdActual = eventoId;
        ViewBag.ConfirmacionActual = confirmacion;

        var query = _context.Invitados.Include(i => i.Evento).AsQueryable();

        if (eventoId.HasValue && eventoId.Value > 0)
            query = query.Where(i => i.EventoId == eventoId.Value);

        if (!string.IsNullOrEmpty(confirmacion))
            query = query.Where(i => i.Confirmacion == confirmacion);

        var invitados = await query.OrderBy(i => i.EventoId).ThenBy(i => i.Nombre).ToListAsync();

        ViewBag.TotalConfirmados = invitados.Count(i => i.Confirmacion == "Confirmado");
        ViewBag.TotalPendientes = invitados.Count(i => i.Confirmacion == "Pendiente");
        ViewBag.TotalNoAsiste = invitados.Count(i => i.Confirmacion == "No asistirá");

        return View(invitados);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador,Planificador")]
    public async Task<IActionResult> Create(Invitado invitado)
    {
        if (ModelState.IsValid)
        {
            _context.Invitados.Add(invitado);
            _context.RegistrarBitacora("Evento", "Invitado registrado",
                $"Se registró a \"{invitado.Nombre}\" ({invitado.Confirmacion}, Mesa: {invitado.Mesa}).",
                User.Identity?.Name ?? "Usuario", invitado.EventoId);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Invitado \"{invitado.Nombre}\" registrado correctamente.";
        }
        return RedirectToAction(nameof(Index), new { eventoId = invitado.EventoId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador,Planificador")]
    public async Task<IActionResult> CambiarConfirmacion(int id, string nuevaConfirmacion)
    {
        var inv = await _context.Invitados.FindAsync(id);
        if (inv != null)
        {
            inv.Confirmacion = nuevaConfirmacion;
            _context.RegistrarBitacora("Evento", "Confirmación de invitado actualizada",
                $"Asistencia de \"{inv.Nombre}\" marcada como {nuevaConfirmacion}.",
                User.Identity?.Name ?? "Usuario", inv.EventoId);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Asistencia de {inv.Nombre} actualizada a {nuevaConfirmacion}.";
        }
        return RedirectToAction(nameof(Index), new { eventoId = inv?.EventoId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador,Planificador")]
    public async Task<IActionResult> ImportarCsv(int eventoId, IFormFile? archivoCsv, string? textoCsv)
    {
        string contenido = string.Empty;
        if (archivoCsv != null && archivoCsv.Length > 0)
        {
            using var reader = new StreamReader(archivoCsv.OpenReadStream(), Encoding.UTF8);
            contenido = await reader.ReadToEndAsync();
        }
        else if (!string.IsNullOrWhiteSpace(textoCsv))
        {
            contenido = textoCsv;
        }

        if (string.IsNullOrWhiteSpace(contenido))
        {
            TempData["Error"] = "Debes subir un archivo .csv o pegar las filas CSV para importar invitados.";
            return RedirectToAction(nameof(Index), new { eventoId });
        }

        var lineas = contenido.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        int importados = 0;

        foreach (var lineaRaw in lineas)
        {
            var linea = lineaRaw.Trim();
            if (string.IsNullOrWhiteSpace(linea)) continue;

            var cols = linea.Split(new[] { ',', ';' });
            var nombre = cols.Length > 0 ? cols[0].Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(nombre) || nombre.Equals("Nombre", StringComparison.OrdinalIgnoreCase))
                continue;

            var email = cols.Length > 1 ? cols[1].Trim() : string.Empty;
            var telefono = cols.Length > 2 ? cols[2].Trim() : string.Empty;
            var mesa = cols.Length > 3 ? cols[3].Trim() : "General";
            var confirmacion = cols.Length > 4 && !string.IsNullOrWhiteSpace(cols[4].Trim())
                ? cols[4].Trim()
                : "Pendiente";

            if (confirmacion != "Confirmado" && confirmacion != "Pendiente" && confirmacion != "No asistirá")
                confirmacion = "Pendiente";

            _context.Invitados.Add(new Invitado
            {
                EventoId = eventoId,
                Nombre = nombre,
                Email = email,
                Telefono = telefono,
                Mesa = string.IsNullOrWhiteSpace(mesa) ? "General" : mesa,
                Confirmacion = confirmacion
            });
            importados++;
        }

        if (importados > 0)
        {
            _context.RegistrarBitacora("Evento", "Importación masiva CSV de invitados",
                $"Se importaron {importados} invitado(s) desde CSV al evento #{eventoId}.",
                User.Identity?.Name ?? "Usuario", eventoId);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Se importaron exitosamente {importados} invitado(s) desde CSV a SQL Server.";
        }
        else
        {
            TempData["Error"] = "No se detectaron filas válidas en el CSV (formato: Nombre,Email,Teléfono,Mesa,Confirmación).";
        }

        return RedirectToAction(nameof(Index), new { eventoId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador,Planificador")]
    public async Task<IActionResult> Delete(int id)
    {
        var inv = await _context.Invitados.FindAsync(id);
        int? evId = inv?.EventoId;
        if (inv != null)
        {
            _context.Invitados.Remove(inv);
            _context.RegistrarBitacora("Evento", "Invitado eliminado",
                $"Se eliminó a \"{inv.Nombre}\" de la lista de invitados.",
                User.Identity?.Name ?? "Usuario", evId);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Invitado eliminado.";
        }
        return RedirectToAction(nameof(Index), new { eventoId = evId });
    }
}
