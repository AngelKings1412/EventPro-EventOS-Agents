using EventPro.Services;

namespace EventPro.Models.ViewModels;

public class AgentesConsoleViewModel
{
    public OrchestrationRequest Request { get; set; } = new();
    public OrchestrationResult? Resultado { get; set; }
    public List<DocumentoConocimiento> DocumentosRag { get; set; } = new();
    public string? PropuestasJson { get; set; }
}

public class EventoReporteResumen
{
    public int EventoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string Lugar { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public int Aforo { get; set; }
    public decimal PresupuestoMaximo { get; set; }
    public decimal TotalEstimado { get; set; }
    public decimal TotalReal { get; set; }
    public decimal SaldoDisponible => PresupuestoMaximo - TotalReal;
    public double PorcentajePresupuesto => PresupuestoMaximo > 0 ? Math.Round((double)(TotalReal / PresupuestoMaximo) * 100.0, 1) : 0;
    public int TareasTotales { get; set; }
    public int TareasCompletadas { get; set; }
    public int InvitadosTotales { get; set; }
    public int InvitadosConfirmados { get; set; }
}

public class ReportesViewModel
{
    public int? EventoIdSeleccionado { get; set; }
    public int TotalEventos { get; set; }
    public decimal TotalPresupuestoMaximo { get; set; }
    public decimal TotalEstimado { get; set; }
    public decimal TotalReal { get; set; }
    public decimal SaldoGlobal => TotalPresupuestoMaximo - TotalReal;
    public int TotalTareas { get; set; }
    public int TareasCompletadas { get; set; }
    public int TareasEnProceso { get; set; }
    public int TareasPendientes { get; set; }
    public int TareasVencidas { get; set; }
    public int InvitadosConfirmados { get; set; }
    public int InvitadosPendientes { get; set; }
    public int InvitadosNoAsistira { get; set; }
    public int TotalIntervencionesIa { get; set; }

    public List<EventoReporteResumen> ResumenPorEvento { get; set; } = new();
    public List<RubroPresupuesto> Rubros { get; set; } = new();
    public List<EventoProveedor> Contrataciones { get; set; } = new();
    public List<Invitado> Invitados { get; set; } = new();
}
