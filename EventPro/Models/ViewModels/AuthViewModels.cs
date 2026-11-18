using System.ComponentModel.DataAnnotations;

namespace EventPro.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "El correo es obligatorio")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class DashboardViewModel
{
    public int EventosActivos { get; set; }
    public int EventosPorIniciar { get; set; }
    public decimal PresupuestoComprometido { get; set; }
    public int TareasVencidas { get; set; }
    public int IntervencionesIa { get; set; }
    public int DocumentosRag { get; set; }
    public int InvitadosConfirmados { get; set; }
    public List<Evento> ProximosEventos { get; set; } = new();
    public List<Tarea> TareasPorVencer { get; set; } = new();
    public List<Bitacora> UltimasBitacorasIa { get; set; } = new();
}
