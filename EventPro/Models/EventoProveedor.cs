using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventPro.Models;

[Table("EventoProveedores")]
public class EventoProveedor
{
    [Key]
    public int Id { get; set; }

    public int EventoId { get; set; }
    [ForeignKey("EventoId")]
    public Evento? Evento { get; set; }

    public int ProveedorId { get; set; }
    [ForeignKey("ProveedorId")]
    public Proveedor? Proveedor { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Costo { get; set; }

    [DataType(DataType.Date)]
    public DateTime FechaContratacion { get; set; } = DateTime.Today;
}
