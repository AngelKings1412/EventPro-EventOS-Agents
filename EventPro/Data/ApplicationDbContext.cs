using EventPro.Models;
using Microsoft.EntityFrameworkCore;

namespace EventPro.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Evento> Eventos => Set<Evento>();
    public DbSet<RubroPresupuesto> RubrosPresupuesto => Set<RubroPresupuesto>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<EventoProveedor> EventoProveedores => Set<EventoProveedor>();
    public DbSet<Tarea> Tareas => Set<Tarea>();
    public DbSet<Invitado> Invitados => Set<Invitado>();
    public DbSet<Comunicado> Comunicados => Set<Comunicado>();
    public DbSet<Bitacora> Bitacoras => Set<Bitacora>();
    public DbSet<DocumentoConocimiento> DocumentosConocimiento => Set<DocumentoConocimiento>();

    public void RegistrarBitacora(string tipo, string accion, string detalle, string usuario, int? eventoId = null)
    {
        static string Truncar(string valor, int max) =>
            string.IsNullOrEmpty(valor) ? string.Empty : (valor.Length <= max ? valor : valor[..(max - 3)] + "...");

        Bitacoras.Add(new Bitacora
        {
            Fecha = DateTime.Now,
            Tipo = Truncar(tipo, 40),
            Accion = Truncar(accion, 200),
            Detalle = detalle,
            Usuario = Truncar(string.IsNullOrWhiteSpace(usuario) ? "Sistema" : usuario, 120),
            EventoId = eventoId
        });
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Email único para Usuario
        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Relación Evento -> Cliente (Restrict delete)
        modelBuilder.Entity<Evento>()
            .HasOne(e => e.Cliente)
            .WithMany(c => c.Eventos)
            .HasForeignKey(e => e.ClienteId)
            .OnDelete(DeleteBehavior.SetNull);

        // Relación Evento -> Organizador (Usuario)
        modelBuilder.Entity<Evento>()
            .HasOne(e => e.Organizador)
            .WithMany(u => u.EventosOrganizados)
            .HasForeignKey(e => e.OrganizadorId)
            .OnDelete(DeleteBehavior.SetNull);

        // Relación Evento -> RubrosPresupuesto (Cascade)
        modelBuilder.Entity<RubroPresupuesto>()
            .HasOne(r => r.Evento)
            .WithMany(e => e.RubrosPresupuesto)
            .HasForeignKey(r => r.EventoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relación Evento -> Tareas (Cascade)
        modelBuilder.Entity<Tarea>()
            .HasOne(t => t.Evento)
            .WithMany(e => e.Tareas)
            .HasForeignKey(t => t.EventoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relación Evento -> Invitados (Cascade)
        modelBuilder.Entity<Invitado>()
            .HasOne(i => i.Evento)
            .WithMany(e => e.Invitados)
            .HasForeignKey(i => i.EventoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relación Evento -> Comunicados (Cascade)
        modelBuilder.Entity<Comunicado>()
            .HasOne(c => c.Evento)
            .WithMany(e => e.Comunicados)
            .HasForeignKey(c => c.EventoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relación Comunicado -> EnviadoPor (Usuario)
        modelBuilder.Entity<Comunicado>()
            .HasOne(c => c.EnviadoPor)
            .WithMany(u => u.ComunicadosEnviados)
            .HasForeignKey(c => c.EnviadoPorId)
            .OnDelete(DeleteBehavior.SetNull);

        // Relación N:M Evento <-> Proveedor
        modelBuilder.Entity<EventoProveedor>()
            .HasOne(ep => ep.Evento)
            .WithMany(e => e.EventoProveedores)
            .HasForeignKey(ep => ep.EventoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EventoProveedor>()
            .HasOne(ep => ep.Proveedor)
            .WithMany(p => p.EventoProveedores)
            .HasForeignKey(ep => ep.ProveedorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
