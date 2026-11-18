using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventPro.Models;

[Table("Proveedores")]
public class Proveedor
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Contacto { get; set; } = string.Empty;

    [MaxLength(30)]
    public string Telefono { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Servicio { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal CostoEstimado { get; set; } = 5000m;

    public double Calificacion { get; set; } = 4.8;

    public ICollection<EventoProveedor> EventoProveedores { get; set; } = new List<EventoProveedor>();
}
