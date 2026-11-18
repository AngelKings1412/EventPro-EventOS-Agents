using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventPro.Models;

[Table("Bitacoras")]
public class Bitacora
{
    [Key]
    public int Id { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Now;

    [Required, MaxLength(40)]
    public string Tipo { get; set; } = "Sistema"; // Agente, Evento, Presupuesto, Tarea, Comunicado, Seguridad, Sistema

    [Required, MaxLength(200)]
    public string Accion { get; set; } = string.Empty;

    public string Detalle { get; set; } = string.Empty;

    [MaxLength(120)]
    public string Usuario { get; set; } = "Sistema";

    public int? EventoId { get; set; }
}
