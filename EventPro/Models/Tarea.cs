using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventPro.Models;

[Table("Tareas")]
public class Tarea
{
    [Key]
    public int Id { get; set; }

    public int EventoId { get; set; }
    [ForeignKey("EventoId")]
    public Evento? Evento { get; set; }

    [Required, MaxLength(200)]
    public string Titulo { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime FechaLimite { get; set; } = DateTime.Today.AddDays(3);

    [MaxLength(100)]
    public string Responsable { get; set; } = string.Empty;

    [MaxLength(30)]
    public string Estado { get; set; } = "Pendiente"; // Pendiente, En proceso, Completada, Vencida

    [MaxLength(20)]
    public string Prioridad { get; set; } = "Media"; // Alta, Media, Baja
}
