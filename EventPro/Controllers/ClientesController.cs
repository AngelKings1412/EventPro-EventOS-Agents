using EventPro.Data;
using EventPro.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventPro.Controllers;

[Authorize]
public class ClientesController : Controller
{
    private readonly ApplicationDbContext _context;

    public ClientesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var clientes = await _context.Clientes
            .Include(c => c.Eventos)
            .OrderBy(c => c.Nombre)
            .ToListAsync();
        return View(clientes);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador,Planificador")]
    public async Task<IActionResult> Create(Cliente cliente)
    {
        if (ModelState.IsValid)
        {
            _context.Clientes.Add(cliente);
            _context.RegistrarBitacora("Sistema", "Cliente registrado",
                $"Se dio de alta al cliente \"{cliente.Nombre}\" ({cliente.Empresa}).",
                User.Identity?.Name ?? "Usuario");
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Cliente \"{cliente.Nombre}\" registrado correctamente.";
        }
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrador,Planificador")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var cliente = await _context.Clientes.FindAsync(id);
        if (cliente == null) return NotFound();
        return View(cliente);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador,Planificador")]
    public async Task<IActionResult> Edit(int id, Cliente cliente)
    {
        if (id != cliente.Id) return NotFound();
        if (ModelState.IsValid)
        {
            _context.Update(cliente);
            _context.RegistrarBitacora("Sistema", "Cliente actualizado",
                $"Se actualizó al cliente \"{cliente.Nombre}\".", User.Identity?.Name ?? "Usuario");
            await _context.SaveChangesAsync();
            TempData["Success"] = "Cliente actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        return View(cliente);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Delete(int id)
    {
        var cliente = await _context.Clientes.FindAsync(id);
        if (cliente != null)
        {
            _context.Clientes.Remove(cliente);
            _context.RegistrarBitacora("Sistema", "Cliente eliminado",
                $"Se eliminó al cliente \"{cliente.Nombre}\".", User.Identity?.Name ?? "Usuario");
            await _context.SaveChangesAsync();
            TempData["Success"] = "Cliente eliminado del catálogo.";
        }
        return RedirectToAction(nameof(Index));
    }
}
