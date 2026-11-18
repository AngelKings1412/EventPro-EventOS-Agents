using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventPro.Models;

[Table("RubrosPresupuesto")]
public class RubroPresupuesto
{
    [Key]
    public int Id { get; set; }

    public int EventoId { get; set; }
    [ForeignKey("EventoId")]
    public Evento? Evento { get; set; }

    [Required, MaxLength(150)]
    public string Concepto { get; set; } = string.Empty;

    [MaxLength(60)]
    public string Categoria { get; set; } = "Logística";

    [Column(TypeName = "decimal(18,2)")]
    public decimal MontoEstimado { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal MontoReal { get; set; }
}
