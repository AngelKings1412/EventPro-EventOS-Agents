using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventPro.Models;

[Table("DocumentosConocimiento")]
public class DocumentoConocimiento
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "El título es obligatorio"), MaxLength(200)]
    public string Titulo { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Categoria { get; set; } = "Normativa ITESCA"; // Espacios y Salones, Política Financiera, Protocolo Comunicación, Proveedores, Seguridad y Logística

    [Required(ErrorMessage = "El contenido es obligatorio")]
    public string Contenido { get; set; } = string.Empty;

    [MaxLength(300)]
    public string PalabrasClave { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime FechaActualizacion { get; set; } = DateTime.Today;
}
