using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventPro.Models;

[Table("Clientes")]
public class Cliente
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(30)]
    public string Telefono { get; set; } = string.Empty;

    [MaxLength(150)]
    public string Empresa { get; set; } = string.Empty;

    public ICollection<Evento> Eventos { get; set; } = new List<Evento>();
}
