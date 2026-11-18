using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventPro.Models;

[Table("Eventos")]
public class Evento
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre del evento es obligatorio")]
    [MaxLength(200)]
    public string Nombre { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime FechaInicio { get; set; } = DateTime.Today.AddDays(7);

    [DataType(DataType.Date)]
    public DateTime FechaFin { get; set; } = DateTime.Today.AddDays(7);

    [MaxLength(200)]
    public string Lugar { get; set; } = string.Empty;

    [MaxLength(40)]
    public string Estado { get; set; } = "Borrador"; // Borrador, Planificado, En coordinación, Finalizado, Cancelado

    [MaxLength(60)]
    public string Tipo { get; set; } = "Académico"; // Académico, Tecnológico, Cultural, Deportivo, Corporativo

    [Column(TypeName = "decimal(18,2)")]
    public decimal PresupuestoMaximo { get; set; } = 50000m;

    [MaxLength(500)]
    public string Descripcion { get; set; } = string.Empty;

    public int Aforo { get; set; } = 100;

    public int? ClienteId { get; set; }
    [ForeignKey("ClienteId")]
    public Cliente? Cliente { get; set; }

    public int? OrganizadorId { get; set; }
    [ForeignKey("OrganizadorId")]
    public Usuario? Organizador { get; set; }

    public ICollection<RubroPresupuesto> RubrosPresupuesto { get; set; } = new List<RubroPresupuesto>();
    public ICollection<Tarea> Tareas { get; set; } = new List<Tarea>();
    public ICollection<Invitado> Invitados { get; set; } = new List<Invitado>();
    public ICollection<Comunicado> Comunicados { get; set; } = new List<Comunicado>();
    public ICollection<EventoProveedor> EventoProveedores { get; set; } = new List<EventoProveedor>();
}
