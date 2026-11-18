using EventPro.Data;
using EventPro.Models;
using EventPro.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventPro.Controllers;

[Authorize(Roles = "Administrador")]
public class UsuariosController : Controller
{
    private readonly ApplicationDbContext _context;

    public UsuariosController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var usuarios = await _context.Usuarios.OrderBy(u => u.Rol).ThenBy(u => u.Nombre).ToListAsync();
        return View(usuarios);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string nombre, string email, string password, string rol)
    {
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            TempData["Error"] = "Todos los campos son obligatorios para crear un usuario.";
            return RedirectToAction(nameof(Index));
        }

        if (await _context.Usuarios.AnyAsync(u => u.Email.ToLower() == email.Trim().ToLower()))
        {
            TempData["Error"] = $"Ya existe un usuario registrado con el correo {email}.";
            return RedirectToAction(nameof(Index));
        }

        var nuevo = new Usuario
        {
            Nombre = nombre.Trim(),
            Email = email.Trim(),
            PasswordHash = PasswordHasher.Hash(password),
            Rol = string.IsNullOrWhiteSpace(rol) ? "Planificador" : rol,
            Activo = true
        };

        _context.Usuarios.Add(nuevo);
        _context.RegistrarBitacora("Seguridad", "Nuevo usuario creado",
            $"Se dio de alta al usuario \"{nuevo.Nombre}\" ({nuevo.Email}) con rol {nuevo.Rol}.",
            User.Identity?.Name ?? "Administrador");
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Usuario \"{nuevo.Nombre}\" ({nuevo.Rol}) creado con hash PBKDF2.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarRol(int id, string nuevoRol)
    {
        var u = await _context.Usuarios.FindAsync(id);
        if (u != null)
        {
            if (u.Email.Equals("admin@itesca.edu.mx", StringComparison.OrdinalIgnoreCase) && nuevoRol != "Administrador")
            {
                TempData["Error"] = "La cuenta principal de Administrador (admin@itesca.edu.mx) no puede degradarse de rol.";
                return RedirectToAction(nameof(Index));
            }
            u.Rol = nuevoRol;
            _context.RegistrarBitacora("Seguridad", "Cambio de rol de usuario",
                $"Rol de {u.Email} actualizado a {nuevoRol}.", User.Identity?.Name ?? "Administrador");
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Rol de {u.Nombre} cambiado a {nuevoRol}.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActivo(int id)
    {
        var u = await _context.Usuarios.FindAsync(id);
        if (u != null)
        {
            if (u.Email.Equals("admin@itesca.edu.mx", StringComparison.OrdinalIgnoreCase) && u.Activo)
            {
                TempData["Error"] = "Por seguridad no se puede desactivar la cuenta principal de Administrador (admin@itesca.edu.mx).";
                return RedirectToAction(nameof(Index));
            }
            u.Activo = !u.Activo;
            _context.RegistrarBitacora("Seguridad", u.Activo ? "Usuario activado" : "Usuario desactivado",
                $"Estado de {u.Email} cambiado a {(u.Activo ? "Activo" : "Inactivo")}.",
                User.Identity?.Name ?? "Administrador");
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Usuario {u.Nombre} {(u.Activo ? "activado" : "desactivado")}.";
        }
        return RedirectToAction(nameof(Index));
    }
}
