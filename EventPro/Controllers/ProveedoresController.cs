using EventPro.Data;
using EventPro.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EventPro.Controllers;

[Authorize]
public class ProveedoresController : Controller
{
    private readonly ApplicationDbContext _context;

    public ProveedoresController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var proveedores = await _context.Proveedores
            .OrderByDescending(p => p.Calificacion)
            .ThenBy(p => p.Nombre)
            .ToListAsync();

        var contrataciones = await _context.EventoProveedores
            .Include(ep => ep.Evento)
            .Include(ep => ep.Proveedor)
            .OrderByDescending(ep => ep.FechaContratacion)
            .ToListAsync();

        ViewBag.Eventos = new SelectList(await _context.Eventos.OrderBy(e => e.Nombre).ToListAsync(), "Id", "Nombre");
        ViewBag.ProveedoresSelect = new SelectList(proveedores, "Id", "Nombre");
        ViewBag.Contrataciones = contrataciones;

        return View(proveedores);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador,Planificador")]
    public async Task<IActionResult> Create(Proveedor proveedor)
    {
        if (ModelState.IsValid)
        {
            _context.Proveedores.Add(proveedor);
            _context.RegistrarBitacora("Presupuesto", "Proveedor registrado en catálogo",
                $"Se registró a \"{proveedor.Nombre}\" ({proveedor.Servicio}, tarifa base ${proveedor.CostoEstimado:N2}).",
                User.Identity?.Name ?? "Usuario");
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Proveedor \"{proveedor.Nombre}\" agregado al catálogo.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador,Planificador")]
    public async Task<IActionResult> Contratar(int eventoId, int proveedorId, decimal costo, DateTime fechaContratacion)
    {
        var prov = await _context.Proveedores.FindAsync(proveedorId);
        var ev = await _context.Eventos.FindAsync(eventoId);
        if (prov != null && ev != null)
        {
            var contrato = new EventoProveedor
            {
                EventoId = eventoId,
                ProveedorId = proveedorId,
                Costo = costo > 0 ? costo : prov.CostoEstimado,
                FechaContratacion = fechaContratacion == default ? DateTime.Today : fechaContratacion
            };
            _context.EventoProveedores.Add(contrato);
            _context.RegistrarBitacora("Presupuesto", "Proveedor contratado para evento",
                $"Proveedor \"{prov.Nombre}\" asignado a \"{ev.Nombre}\" por ${contrato.Costo:N2} MXN.",
                User.Identity?.Name ?? "Usuario", ev.Id);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Proveedor \"{prov.Nombre}\" contratado para \"{ev.Nombre}\".";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador,Planificador")]
    public async Task<IActionResult> DeleteContratacion(int id)
    {
        var ep = await _context.EventoProveedores.FindAsync(id);
        if (ep != null)
        {
            _context.EventoProveedores.Remove(ep);
            _context.RegistrarBitacora("Presupuesto", "Contratación cancelada",
                $"Se eliminó la contratación #{id}.", User.Identity?.Name ?? "Usuario", ep.EventoId);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Contratación eliminada.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Delete(int id)
    {
        var prov = await _context.Proveedores.FindAsync(id);
        if (prov != null)
        {
            _context.Proveedores.Remove(prov);
            _context.RegistrarBitacora("Presupuesto", "Proveedor eliminado",
                $"Se eliminó a \"{prov.Nombre}\" del catálogo.", User.Identity?.Name ?? "Usuario");
            await _context.SaveChangesAsync();
            TempData["Success"] = "Proveedor eliminado del catálogo.";
        }
        return RedirectToAction(nameof(Index));
    }
}
