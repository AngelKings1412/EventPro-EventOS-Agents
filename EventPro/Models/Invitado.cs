using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventPro.Models;

[Table("Invitados")]
public class Invitado
{
    [Key]
    public int Id { get; set; }

    public int EventoId { get; set; }
    [ForeignKey("EventoId")]
    public Evento? Evento { get; set; }

    [Required, MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(30)]
    public string Telefono { get; set; } = string.Empty;

    [MaxLength(30)]
    public string Confirmacion { get; set; } = "Pendiente"; // Confirmado, Pendiente, No asistirá

    [MaxLength(50)]
    public string Mesa { get; set; } = string.Empty;
}
