using EventPro.Data;
using EventPro.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EventPro.Controllers;

[Authorize]
public class EventosController : Controller
{
    private readonly ApplicationDbContext _context;

    public EventosController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Eventos
    public async Task<IActionResult> Index()
    {
        var eventos = await _context.Eventos
            .Include(e => e.Cliente)
            .Include(e => e.Organizador)
            .Include(e => e.RubrosPresupuesto)
            .Include(e => e.Tareas)
            .Include(e => e.Invitados)
            .OrderByDescending(e => e.FechaInicio)
            .ToListAsync();

        return View(eventos);
    }

    // GET: Eventos/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var evento = await _context.Eventos
            .Include(e => e.Cliente)
            .Include(e => e.Organizador)
            .Include(e => e.RubrosPresupuesto)
            .Include(e => e.Tareas)
            .Include(e => e.Invitados)
            .Include(e => e.Comunicados)
            .Include(e => e.EventoProveedores)
                .ThenInclude(ep => ep.Proveedor)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (evento == null) return NotFound();

        return View(evento);
    }

    // GET: Eventos/Create
    [Authorize(Roles = "Administrador,Planificador")]
    public async Task<IActionResult> Create()
    {
        await CargarCombos();
        return View(new Evento());
    }

    // POST: Eventos/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador,Planificador")]
    public async Task<IActionResult> Create(Evento evento)
    {
        if (ModelState.IsValid)
        {
            _context.Add(evento);
            _context.RegistrarBitacora("Evento", "Nuevo evento registrado",
                $"Se creó el evento \"{evento.Nombre}\" ({evento.Tipo}, {evento.Lugar}, Techo: ${evento.PresupuestoMaximo:N2}).",
                User.Identity?.Name ?? "Usuario");
            await _context.SaveChangesAsync();
            TempData["Success"] = $"El evento '{evento.Nombre}' fue registrado exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        await CargarCombos(evento.ClienteId, evento.OrganizadorId);
        return View(evento);
    }

    // GET: Eventos/Edit/5
    [Authorize(Roles = "Administrador,Planificador")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var evento = await _context.Eventos.FindAsync(id);
        if (evento == null) return NotFound();

        await CargarCombos(evento.ClienteId, evento.OrganizadorId);
        return View(evento);
    }

    // POST: Eventos/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador,Planificador")]
    public async Task<IActionResult> Edit(int id, Evento evento)
    {
        if (id != evento.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(evento);
                _context.RegistrarBitacora("Evento", "Evento actualizado",
                    $"Se actualizaron datos del evento \"{evento.Nombre}\" (Estado: {evento.Estado}, PresupuestoMáx: ${evento.PresupuestoMaximo:N2}).",
                    User.Identity?.Name ?? "Usuario", evento.Id);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Evento '{evento.Nombre}' actualizado correctamente.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Eventos.Any(e => e.Id == evento.Id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        await CargarCombos(evento.ClienteId, evento.OrganizadorId);
        return View(evento);
    }

    // GET: Eventos/Delete/5
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var evento = await _context.Eventos
            .Include(e => e.Cliente)
            .Include(e => e.Organizador)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (evento == null) return NotFound();

        return View(evento);
    }

    // POST: Eventos/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var evento = await _context.Eventos.FindAsync(id);
        if (evento != null)
        {
            var nombreEv = evento.Nombre;
            _context.Eventos.Remove(evento);
            _context.RegistrarBitacora("Evento", "Evento eliminado",
                $"Se eliminó el evento \"{nombreEv}\" y sus registros en cascada.",
                User.Identity?.Name ?? "Usuario");
            await _context.SaveChangesAsync();
            TempData["Success"] = "Evento eliminado correctamente.";
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task CargarCombos(int? clienteId = null, int? organizadorId = null)
    {
        ViewBag.ClienteId = new SelectList(await _context.Clientes.OrderBy(c => c.Nombre).ToListAsync(), "Id", "Nombre", clienteId);
        ViewBag.OrganizadorId = new SelectList(await _context.Usuarios.Where(u => u.Rol == "Planificador" || u.Rol == "Administrador").OrderBy(u => u.Nombre).ToListAsync(), "Id", "Nombre", organizadorId);
    }
}
