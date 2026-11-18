using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventPro.Models;

[Table("Comunicados")]
public class Comunicado
{
    [Key]
    public int Id { get; set; }

    public int EventoId { get; set; }
    [ForeignKey("EventoId")]
    public Evento? Evento { get; set; }

    [Required, MaxLength(200)]
    public string Asunto { get; set; } = string.Empty;

    [Required]
    public string Contenido { get; set; } = string.Empty;

    [MaxLength(40)]
    public string Canal { get; set; } = "Correo electrónico"; // Correo electrónico, WhatsApp, SMS, Circular Interna

    [MaxLength(120)]
    public string Destinatarios { get; set; } = "Todos los invitados";

    [MaxLength(80)]
    public string EstadoEnvio { get; set; } = "Enviado";

    public DateTime FechaEnvio { get; set; } = DateTime.Now;

    public int? EnviadoPorId { get; set; }
    [ForeignKey("EnviadoPorId")]
    public Usuario? EnviadoPor { get; set; }
}
