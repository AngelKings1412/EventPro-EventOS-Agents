using EventPro.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventPro.Controllers;

[Authorize]
public class BitacoraController : Controller
{
    private readonly ApplicationDbContext _context;

    public BitacoraController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? tipo, string? buscar)
    {
        var all = await _context.Bitacoras.OrderByDescending(b => b.Fecha).ToListAsync();

        ViewBag.TotalRegistros = all.Count;
        ViewBag.TotalAgentes = all.Count(b => b.Tipo == "Agente");
        ViewBag.TotalOperaciones = all.Count(b => b.Tipo != "Agente");
        ViewBag.TipoActual = tipo;
        ViewBag.BuscarActual = buscar;

        var filtrados = all.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(tipo))
            filtrados = filtrados.Where(b => b.Tipo.Equals(tipo, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(buscar))
            filtrados = filtrados.Where(b =>
                b.Accion.Contains(buscar, StringComparison.OrdinalIgnoreCase) ||
                b.Detalle.Contains(buscar, StringComparison.OrdinalIgnoreCase) ||
                b.Usuario.Contains(buscar, StringComparison.OrdinalIgnoreCase));

        return View(filtrados.ToList());
    }
}
