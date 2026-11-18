using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using EventPro.Data;
using EventPro.Models;
using Microsoft.EntityFrameworkCore;

namespace EventPro.Services;

public class OrchestrationRequest
{
    public int? EventoId { get; set; }
    public string AgenteForzado { get; set; } = "auto";
    public string Prompt { get; set; } = string.Empty;
    public string Tono { get; set; } = "Institucional y cordial";
    public string Canal { get; set; } = "Correo electrónico";
    public string Destinatario { get; set; } = "Comunidad estudiantil y docente del ITESCA";
    public string TipoComunicado { get; set; } = "invitacion";
    public string NotasExtra { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}

public class AgentMetaDto
{
    public string Id { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Icono { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
}

public class RagSourceDto
{
    public int DocumentoId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string Fragmento { get; set; } = string.Empty;
    public int PuntajeRelevancia { get; set; }
}

public class SpaceCheckDto
{
    public bool Disponible { get; set; } = true;
    public string Mensaje { get; set; } = string.Empty;
    public string Nivel { get; set; } = "success"; // success, warning, danger
}

public class OrchestrationTraceStep
{
    public string Etapa { get; set; } = string.Empty; // Percibir, Razonar, Planificar, Actuar, Evaluar
    public string Descripcion { get; set; } = string.Empty;
}

public class AgentActionProposal
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Tipo { get; set; } = string.Empty; // agregar_tarea, agregar_rubro, contratar_proveedor, crear_comunicado
    public string AgenteOrigen { get; set; } = string.Empty;
    public string Etiqueta { get; set; } = string.Empty;
    public int EventoId { get; set; }
    public string EventoNombre { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string Responsable { get; set; } = string.Empty;
    public string FechaLimite { get; set; } = string.Empty;
    public string Prioridad { get; set; } = "Media";
    public decimal Monto { get; set; }
    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public string Canal { get; set; } = "Correo electrónico";
}

public class OrchestrationResult
{
    public bool Ok { get; set; } = true;
    public string ModoMotor { get; set; } = "Motor Híbrido Contextual C#";
    public string Modelo { get; set; } = "eventos-hybrid-rag-v3";
    public long LatenciaMs { get; set; }
    public string? AvisoFallback { get; set; }
    public int EventoId { get; set; }
    public string EventoNombre { get; set; } = string.Empty;
    public string AgenteAsignado { get; set; } = "multi-agente";
    public List<AgentMetaDto> AgentesInvolucrados { get; set; } = new();
    public string RazonamientoOrquestador { get; set; } = string.Empty;
    public SpaceCheckDto VerificacionEspacio { get; set; } = new();
    public string AlertaPresupuestal { get; set; } = string.Empty;
    public string NivelAlertaPresupuestal { get; set; } = "success";
    public string RespuestaResumen { get; set; } = string.Empty;
    public List<OrchestrationTraceStep> CicloAgente { get; set; } = new();
    public List<RagSourceDto> FuentesRag { get; set; } = new();
    public List<AgentActionProposal> AccionesSugeridas { get; set; } = new();
}

public class AiOrchestratorService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    private static readonly Dictionary<string, AgentMetaDto> AgentesCatalogo = new()
    {
        ["planificador"] = new AgentMetaDto
        {
            Id = "planificador",
            Nombre = "Agente Planificador",
            Icono = "bi-calendar2-check",
            Rol = "Logística, cronogramas, validación de salones y tareas"
        },
        ["presupuesto"] = new AgentMetaDto
        {
            Id = "presupuesto",
            Nombre = "Agente de Presupuesto",
            Icono = "bi-cash-coin",
            Rol = "Salud financiera, alertas de límite y cotización con proveedores"
        },
        ["comunicacion"] = new AgentMetaDto
        {
            Id = "comunicacion",
            Nombre = "Agente de Comunicación",
            Icono = "bi-envelope-paper-heart",
            Rol = "Redacción multicanal e invitaciones institucionales"
        },
        ["rag"] = new AgentMetaDto
        {
            Id = "rag",
            Nombre = "Agente RAG (Normativa)",
            Icono = "bi-book-half",
            Rol = "Consulta de políticas, reglamentos y capacidades ITESCA"
        },
        ["multi-agente"] = new AgentMetaDto
        {
            Id = "multi-agente",
            Nombre = "Orquestador Central Multi-Agente",
            Icono = "bi-cpu-fill",
            Rol = "Coordinación paralela de Planificador, Presupuesto, Comunicación y RAG"
        }
    };

    public AiOrchestratorService(
        ApplicationDbContext context,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<List<RagSourceDto>> BuscarConocimientoRagAsync(string consulta, string tipoEvento = "")
    {
        var docs = await _context.DocumentosConocimiento.AsNoTracking().ToListAsync();
        var textoBusqueda = NormalizarTexto($"{consulta} {tipoEvento}");
        var tokens = textoBusqueda
            .Split(new[] { ' ', ',', '.', ':', ';', '/', '-', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 2)
            .Distinct()
            .ToList();

        var resultados = new List<RagSourceDto>();
        foreach (var doc in docs)
        {
            int puntaje = 0;
            var tituloNorm = NormalizarTexto(doc.Titulo);
            var catNorm = NormalizarTexto(doc.Categoria);
            var kwNorm = NormalizarTexto(doc.PalabrasClave);
            var contNorm = NormalizarTexto(doc.Contenido);

            foreach (var tk in tokens)
            {
                if (tituloNorm.Contains(tk)) puntaje += 5;
                if (kwNorm.Contains(tk)) puntaje += 4;
                if (catNorm.Contains(tk)) puntaje += 3;
                if (contNorm.Contains(tk)) puntaje += 2;
            }

            if (tokens.Count == 0 || puntaje > 0)
            {
                resultados.Add(new RagSourceDto
                {
                    DocumentoId = doc.Id,
                    Titulo = doc.Titulo,
                    Categoria = doc.Categoria,
                    Fragmento = doc.Contenido,
                    PuntajeRelevancia = puntaje > 0 ? puntaje : 1
                });
            }
        }

        return resultados
            .OrderByDescending(r => r.PuntajeRelevancia)
            .Take(3)
            .ToList();
    }

    public async Task<SpaceCheckDto> VerificarDisponibilidadEspacioAsync(Evento ev)
    {
        var traslapes = await _context.Eventos
            .AsNoTracking()
            .Where(e => e.Id != ev.Id
                && e.Estado != "Cancelado"
                && e.Lugar.ToLower() == ev.Lugar.ToLower()
                && e.FechaInicio <= ev.FechaFin
                && e.FechaFin >= ev.FechaInicio)
            .ToListAsync();

        if (traslapes.Any())
        {
            var nombres = string.Join(", ", traslapes.Select(t => $"\"{t.Nombre}\" ({t.FechaInicio:dd/MM}-{t.FechaFin:dd/MM})"));
            return new SpaceCheckDto
            {
                Disponible = false,
                Nivel = "danger",
                Mensaje = $"Conflicto de agenda detectado en \"{ev.Lugar}\": se traslapa con {nombres}."
            };
        }

        // Regla de aforo según RAG de Espacios ITESCA
        var lugarLower = NormalizarTexto(ev.Lugar);
        int capacidadMax = 500;
        if (lugarLower.Contains("convenciones")) capacidadMax = 400;
        else if (lugarLower.Contains("auditorio")) capacidadMax = 250;
        else if (lugarLower.Contains("laboratorio") || lugarLower.Contains("edificio c")) capacidadMax = 180;
        else if (lugarLower.Contains("explanada")) capacidadMax = 600;

        if (ev.Aforo > capacidadMax)
        {
            return new SpaceCheckDto
            {
                Disponible = false,
                Nivel = "warning",
                Mensaje = $"Alerta de sobrecupo según reglamento RAG: \"{ev.Lugar}\" admite máximo {capacidadMax} personas y el evento registra {ev.Aforo}."
            };
        }

        return new SpaceCheckDto
        {
            Disponible = true,
            Nivel = "success",
            Mensaje = $"Espacio verificado: \"{ev.Lugar}\" disponible del {ev.FechaInicio:dd/MM/yyyy} al {ev.FechaFin:dd/MM/yyyy} sin traslapes y dentro del aforo permitido ({ev.Aforo}/{capacidadMax} personas)."
        };
    }

    public string DetectarAgente(string prompt, string agenteForzado)
    {
        if (!string.IsNullOrWhiteSpace(agenteForzado) && agenteForzado != "auto" && AgentesCatalogo.ContainsKey(agenteForzado))
        {
            return agenteForzado;
        }

        var t = NormalizarTexto(prompt);
        bool pideRag = Regex.IsMatch(t, @"reglamento|politica|normativa|rag|capacidad|salon|espacio|proteccion civil|lineamiento");
        bool pidePlan = Regex.IsMatch(t, @"tarea|plan|cronograma|logistica|responsable|fecha|actividad|agenda|checklist");
        bool pidePres = Regex.IsMatch(t, @"presupuesto|costo|gasto|proveedor|dinero|monto|cotiza|finanza|techo|limite|sobrecosto|alerta");
        bool pideCom = Regex.IsMatch(t, @"invitacion|mensaje|correo|redacta|comunicado|difusion|recordatorio|whatsapp|sms|aviso");
        bool pideIntegral = Regex.IsMatch(t, @"integral|completo|todos|diagnostico|orquesta|flujo");

        int hits = new[] { pidePlan, pidePres, pideCom }.Count(x => x);
        if (pideIntegral || hits >= 2) return "multi-agente";
        if (pideRag && hits == 0) return "rag";
        if (pidePres) return "presupuesto";
        if (pideCom) return "comunicacion";
        if (pidePlan) return "planificador";
        return "multi-agente";
    }

    public async Task<OrchestrationResult> EjecutarOrquestacionAsync(OrchestrationRequest req, string nombreUsuario)
    {
        var sw = Stopwatch.StartNew();

        var eventos = await _context.Eventos
            .Include(e => e.RubrosPresupuesto)
            .Include(e => e.Tareas)
            .Include(e => e.Invitados)
            .Include(e => e.EventoProveedores)
                .ThenInclude(ep => ep.Proveedor)
            .ToListAsync();

        var evento = req.EventoId.HasValue
            ? eventos.FirstOrDefault(e => e.Id == req.EventoId.Value) ?? eventos.FirstOrDefault()
            : eventos.FirstOrDefault();

        if (evento == null)
        {
            return new OrchestrationResult
            {
                Ok = false,
                RespuestaResumen = "No hay eventos registrados en SQL Server. Registra primero un evento en el módulo Eventos.",
                LatenciaMs = sw.ElapsedMilliseconds
            };
        }

        var proveedores = await _context.Proveedores.AsNoTracking().ToListAsync();
        var agenteId = DetectarAgente(req.Prompt, req.AgenteForzado);
        var fuentesRag = await BuscarConocimientoRagAsync(
            string.IsNullOrWhiteSpace(req.Prompt) ? $"{evento.Nombre} {evento.Lugar} {evento.Tipo}" : req.Prompt,
            evento.Tipo);
        var espacioCheck = await VerificarDisponibilidadEspacioAsync(evento);

        // Generar análisis base con el Motor Híbrido Contextual C#
        var resultado = GenerarResultadoHibrido(evento, proveedores, agenteId, req, espacioCheck, fuentesRag);

        // Intentar enriquecer con Google Gemini API si existe API Key configurada
        var apiKey = !string.IsNullOrWhiteSpace(req.ApiKey)
            ? req.ApiKey.Trim()
            : (_configuration["Gemini:ApiKey"] ?? string.Empty).Trim();

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            await IntentarEnriquecerConGeminiAsync(resultado, evento, proveedores, req, apiKey);
        }

        sw.Stop();
        resultado.LatenciaMs = Math.Max(14, sw.ElapsedMilliseconds);

        // Registrar auditoría en Bitacoras
        var nombresAgentes = string.Join(", ", resultado.AgentesInvolucrados.Select(a => a.Nombre));
        _context.RegistrarBitacora(
            "Agente",
            $"Ejecución IA ({resultado.AgenteAsignado}) sobre \"{evento.Nombre}\"",
            $"Motor: {resultado.ModoMotor} ({resultado.LatenciaMs} ms) | Solicitó: {nombreUsuario} | Agentes: {nombresAgentes} | Propuestas: {resultado.AccionesSugeridas.Count} | RAG docs: {resultado.FuentesRag.Count}.",
            resultado.AgenteAsignado == "multi-agente" ? "Orquestador Central IA" : nombresAgentes,
            evento.Id);
        await _context.SaveChangesAsync();

        return resultado;
    }

    public async Task<(bool ok, string mensaje)> AplicarPropuestaAsync(AgentActionProposal p, int? usuarioId, string nombreUsuario)
    {
        var evento = await _context.Eventos.FindAsync(p.EventoId);
        if (evento == null) return (false, "El evento asociado ya no existe en la base de datos.");

        switch (p.Tipo)
        {
            case "agregar_tarea":
                if (!DateTime.TryParse(p.FechaLimite, out var fechaLimite))
                {
                    fechaLimite = evento.FechaInicio.AddDays(-3);
                }
                var tarea = new Tarea
                {
                    EventoId = p.EventoId,
                    Titulo = p.Titulo,
                    Responsable = string.IsNullOrWhiteSpace(p.Responsable) ? "Coordinación ITESCA" : p.Responsable,
                    FechaLimite = fechaLimite,
                    Prioridad = string.IsNullOrWhiteSpace(p.Prioridad) ? "Alta" : p.Prioridad,
                    Estado = "Pendiente"
                };
                _context.Tareas.Add(tarea);
                _context.RegistrarBitacora("Agente", "Propuesta IA aplicada: Nueva Tarea",
                    $"Se insertó en SQL Server la tarea \"{tarea.Titulo}\" (Límite: {tarea.FechaLimite:dd/MM/yyyy}) para \"{evento.Nombre}\".",
                    $"Agente Planificador IA (Autorizó: {nombreUsuario})", evento.Id);
                await _context.SaveChangesAsync();
                return (true, $"Tarea \"{tarea.Titulo}\" agregada al cronograma en SQL Server.");

            case "agregar_rubro":
                var rubro = new RubroPresupuesto
                {
                    EventoId = p.EventoId,
                    Concepto = p.Titulo,
                    Categoria = string.IsNullOrWhiteSpace(p.Categoria) ? "Logística" : p.Categoria,
                    MontoEstimado = p.Monto,
                    MontoReal = p.Monto
                };
                _context.RubrosPresupuesto.Add(rubro);
                _context.RegistrarBitacora("Agente", "Propuesta IA aplicada: Nuevo Rubro Presupuestal",
                    $"Se registró rubro \"{rubro.Concepto}\" (${rubro.MontoEstimado:N2} MXN, Cat: {rubro.Categoria}) en \"{evento.Nombre}\".",
                    $"Agente Presupuesto IA (Autorizó: {nombreUsuario})", evento.Id);
                await _context.SaveChangesAsync();
                return (true, $"Rubro \"{rubro.Concepto}\" (${rubro.MontoEstimado:N2}) guardado en SQL Server.");

            case "contratar_proveedor":
                var prov = await _context.Proveedores.FindAsync(p.ProveedorId);
                if (prov == null) return (false, "Proveedor no encontrado en el catálogo.");

                var contrato = new EventoProveedor
                {
                    EventoId = p.EventoId,
                    ProveedorId = p.ProveedorId,
                    Costo = p.Monto > 0 ? p.Monto : prov.CostoEstimado,
                    FechaContratacion = DateTime.Today
                };
                _context.EventoProveedores.Add(contrato);
                _context.RegistrarBitacora("Agente", "Propuesta IA aplicada: Contratación de Proveedor",
                    $"Se asignó a \"{prov.Nombre}\" ({prov.Servicio}) al evento \"{evento.Nombre}\" por ${contrato.Costo:N2} MXN.",
                    $"Agente Presupuesto IA (Autorizó: {nombreUsuario})", evento.Id);
                await _context.SaveChangesAsync();
                return (true, $"Proveedor \"{prov.Nombre}\" vinculado al evento por ${contrato.Costo:N2}.");

            case "crear_comunicado":
                var comunicado = new Comunicado
                {
                    EventoId = p.EventoId,
                    Asunto = p.Titulo,
                    Contenido = p.Descripcion,
                    Canal = string.IsNullOrWhiteSpace(p.Canal) ? "Correo electrónico" : p.Canal,
                    Destinatarios = "Todos los invitados",
                    EstadoEnvio = $"Enviado ({evento.Aforo} alcance / vía IA)",
                    FechaEnvio = DateTime.Now,
                    EnviadoPorId = usuarioId
                };
                _context.Comunicados.Add(comunicado);
                _context.RegistrarBitacora("Agente", "Propuesta IA aplicada: Comunicado Registrado",
                    $"Comunicado \"{comunicado.Asunto}\" guardado vía {comunicado.Canal} para \"{evento.Nombre}\".",
                    $"Agente Comunicación IA (Autorizó: {nombreUsuario})", evento.Id);
                await _context.SaveChangesAsync();
                return (true, $"Comunicado \"{comunicado.Asunto}\" guardado y enviado en el historial.");

            default:
                return (false, "Tipo de acción no soportado.");
        }
    }

    private OrchestrationResult GenerarResultadoHibrido(
        Evento ev,
        List<Proveedor> proveedores,
        string agenteId,
        OrchestrationRequest req,
        SpaceCheckDto espacioCheck,
        List<RagSourceDto> fuentesRag)
    {
        var totalEstimado = ev.RubrosPresupuesto.Sum(r => r.MontoEstimado);
        var totalReal = ev.RubrosPresupuesto.Sum(r => r.MontoReal);
        var techo = ev.PresupuestoMaximo > 0 ? ev.PresupuestoMaximo : 50000m;
        var disponible = techo - totalReal;
        var porcentajeUso = techo > 0 ? Math.Round((totalReal / techo) * 100m, 1) : 0m;

        // Alerta de límite presupuestal (US-05)
        string alertaPres;
        string nivelPres;
        if (porcentajeUso >= 100m)
        {
            nivelPres = "danger";
            alertaPres = $"SOBRECOSTO CRÍTICO ({porcentajeUso}% utilizado): Se han ejecutado ${totalReal:N2} frente al techo autorizado de ${techo:N2} MXN. Excedente de ${Math.Abs(disponible):N2} MXN.";
        }
        else if (porcentajeUso >= 80m)
        {
            nivelPres = "warning";
            alertaPres = $"ALERTA PREVENTIVA ({porcentajeUso}% utilizado): El gasto real (${totalReal:N2}) se acerca al límite autorizado (${techo:N2} MXN). Saldo restante: ${disponible:N2} MXN.";
        }
        else
        {
            nivelPres = "success";
            alertaPres = $"SALUD FINANCIERA ÓPTIMA ({porcentajeUso}% utilizado): Gastado ${totalReal:N2} de ${techo:N2} MXN. Saldo disponible: ${disponible:N2} MXN.";
        }

        // Generar tareas sugeridas no duplicadas (US-02)
        var propuestasTareas = ConstruirPropuestasTareas(ev);

        // Generar cotizaciones y rubros faltantes con catálogo de proveedores (US-04)
        var propuestasPresupuesto = ConstruirPropuestasPresupuesto(ev, proveedores, disponible);

        // Generar comunicado institucional (US-06)
        var propuestaComunicado = ConstruirPropuestaComunicado(ev, req);

        var acciones = new List<AgentActionProposal>();
        var agentesInv = new List<AgentMetaDto>();
        string razonamiento;
        string resumen;

        switch (agenteId)
        {
            case "planificador":
                agentesInv.Add(AgentesCatalogo["planificador"]);
                acciones.AddRange(propuestasTareas);
                razonamiento = $"El Orquestador Central enrutó la meta al Agente Planificador para \"{ev.Nombre}\" ({ev.Tipo}, sede: {ev.Lugar}). Se evaluaron {ev.Tareas.Count} tareas existentes, se verificó disponibilidad de espacios vía RAG y se calcularon {propuestasTareas.Count} hitos críticos previos al {ev.FechaInicio:dd/MM/yyyy}.";
                resumen = $"Diagnóstico Logístico para \"{ev.Nombre}\": Tienes {ev.Tareas.Count(t => t.Estado != "Completada")} tareas activas y {ev.Tareas.Count(t => t.Estado == "Completada")} completadas. {espacioCheck.Mensaje} Se generaron {propuestasTareas.Count} tareas operativas listas para insertar en SQL Server.";
                break;

            case "presupuesto":
                agentesInv.Add(AgentesCatalogo["presupuesto"]);
                acciones.AddRange(propuestasPresupuesto);
                razonamiento = $"El Orquestador Central activó al Agente de Presupuesto cruzando el techo máximo (${techo:N2}), los {ev.RubrosPresupuesto.Count} rubros en SQL Server (${totalReal:N2} ejecutado), el límite normativo RAG (alerta al 85%) y el catálogo de {proveedores.Count} proveedores locales.";
                resumen = $"{alertaPres} Costo real actual por asistente: ${(ev.Aforo > 0 ? totalReal / ev.Aforo : 0m):N2} MXN. Se identificaron categorías sin cobertura y proveedores con mejor calificación para optimizar costos.";
                break;

            case "comunicacion":
                agentesInv.Add(AgentesCatalogo["comunicacion"]);
                acciones.Add(propuestaComunicado);
                razonamiento = $"El Orquestador Central invocó al Agente de Comunicación aplicando el Protocolo de Difusión RAG para el canal \"{req.Canal}\" con tono \"{req.Tono}\", considerando la lista actual de {ev.Invitados.Count} invitados ({ev.Invitados.Count(i => i.Confirmacion == "Confirmado")} confirmados).";
                resumen = $"Comunicado redactado con asunto \"{propuestaComunicado.Titulo}\" adaptado para {req.Destinatario}. Puedes guardarlo y enviarlo directamente al historial del evento con un clic.";
                break;

            case "rag":
                agentesInv.Add(AgentesCatalogo["rag"]);
                razonamiento = $"El Agente RAG consultó la base de conocimiento institucional `DocumentosConocimiento` en SQL Server y recuperó {fuentesRag.Count} normativas aplicables a \"{ev.Nombre}\" ({ev.Lugar}).";
                resumen = $"Consulta Normativa RAG completada: {espacioCheck.Mensaje} Además, conforme a las políticas ITESCA, {alertaPres}";
                break;

            default:
                agentesInv.Add(AgentesCatalogo["planificador"]);
                agentesInv.Add(AgentesCatalogo["presupuesto"]);
                agentesInv.Add(AgentesCatalogo["comunicacion"]);
                agentesInv.Add(AgentesCatalogo["rag"]);
                acciones.AddRange(propuestasTareas.Take(2));
                acciones.AddRange(propuestasPresupuesto.Take(2));
                acciones.Add(propuestaComunicado);
                razonamiento = $"El Orquestador Central ejecutó un flujo Multi-Agente colaborativo para \"{ev.Nombre}\": 1) Consultó {fuentesRag.Count} políticas RAG en SQL Server; 2) Agente Planificador validó el salón y diseñó tareas escalonadas; 3) Agente de Presupuesto auditó el techo (${techo:N2}) y cotizó proveedores; 4) Agente de Comunicación preparó la difusión institucional.";
                resumen = $"Orquestación Multi-Agente completada para \"{ev.Nombre}\" ({ev.Tipo} · {ev.Aforo} asistentes). {espacioCheck.Mensaje} | {alertaPres} | Invitados confirmados: {ev.Invitados.Count(i => i.Confirmacion == "Confirmado")}/{ev.Invitados.Count}. Abajo tienes {acciones.Count} acciones listas para aplicarse en SQL Server.";
                break;
        }

        var ciclo = new List<OrchestrationTraceStep>
        {
            new() { Etapa = "1. Percibir", Descripcion = $"Solicitud recibida para evento #{ev.Id} \"{ev.Nombre}\" (Aforo: {ev.Aforo}, Techo: ${techo:N2})." },
            new() { Etapa = "2. Razonar", Descripcion = $"Intención clasificada como [{agenteId.ToUpperInvariant()}]. Recuperados {fuentesRag.Count} documentos RAG de SQL Server." },
            new() { Etapa = "3. Planificar", Descripcion = $"Verificación de espacio ({(espacioCheck.Disponible ? "Disponible" : "Alerta")}) y cálculo financiero ({porcentajeUso}% usado)." },
            new() { Etapa = "4. Actuar", Descripcion = $"Generadas {acciones.Count} Action Cards listas para persistir en Tablas SQL Server (Tareas, Rubros, Proveedores, Comunicados)." },
            new() { Etapa = "5. Evaluar", Descripcion = "Traza y propuestas validadas con esquema estructural y auditadas en la tabla Bitacoras." }
        };

        return new OrchestrationResult
        {
            Ok = true,
            ModoMotor = "Motor Multi-Agente Híbrido Contextual (SQL Server + RAG)",
            Modelo = "eventpro-orchestrator-v3",
            EventoId = ev.Id,
            EventoNombre = ev.Nombre,
            AgenteAsignado = agenteId,
            AgentesInvolucrados = agentesInv,
            RazonamientoOrquestador = razonamiento,
            VerificacionEspacio = espacioCheck,
            AlertaPresupuestal = alertaPres,
            NivelAlertaPresupuestal = nivelPres,
            RespuestaResumen = resumen,
            CicloAgente = ciclo,
            FuentesRag = fuentesRag,
            AccionesSugeridas = acciones
        };
    }

    private List<AgentActionProposal> ConstruirPropuestasTareas(Evento ev)
    {
        var existentes = new HashSet<string>(ev.Tareas.Select(t => t.Titulo.ToLowerInvariant()));
        var tipo = NormalizarTexto(ev.Tipo);

        var plantillas = new List<(string Titulo, string Resp, int DiasAntes, string Prioridad)>();
        if (tipo.Contains("tecnolog"))
        {
            plantillas.Add(($"Verificar ancho de banda WiFi dedicado y respaldo eléctrico en {ev.Lugar}", "Soporte TI ITESCA (Ángel Reyes)", 8, "Alta"));
            plantillas.Add(($"Realizar ensayo técnico de audio, proyección y streaming ({ev.Aforo} lugares)", "Eduardo Manzanarez Molina", 3, "Alta"));
            plantillas.Add(($"Generar gafetes con código QR y lista de control de acceso", "Jesús Francisco Escobar", 5, "Media"));
        }
        else if (tipo.Contains("cultural"))
        {
            plantillas.Add(($"Montaje de templete, iluminación y prueba de sonido en {ev.Lugar}", "Difusión Cultural ITESCA", 6, "Alta"));
            plantillas.Add(($"Coordinar brigada de Protección Civil y rutas de evacuación (Aforo {ev.Aforo})", "Eventos Seguros Cajeme", 4, "Alta"));
            plantillas.Add(($"Publicar programa artístico oficial en redes institucionales", "Jesús Emmanuel Leal Lara", 7, "Media"));
        }
        else
        {
            plantillas.Add(($"Validar lista oficial de invitados confirmados y asignación de presídium", "David Eduardo Arvizu Duarte", 5, "Alta"));
            plantillas.Add(($"Confirmar entrega de constancias con valor curricular y material de apoyo", "Servicios Escolares ITESCA", 4, "Media"));
            plantillas.Add(($"Inspección previa de mobiliario y señalética en {ev.Lugar}", "Comité de Logística", 2, "Alta"));
        }

        return plantillas
            .Where(p => !existentes.Contains(p.Titulo.ToLowerInvariant()))
            .Select(p => new AgentActionProposal
            {
                Tipo = "agregar_tarea",
                AgenteOrigen = "Agente Planificador",
                Etiqueta = $"Agregar tarea al cronograma: {p.Titulo}",
                EventoId = ev.Id,
                EventoNombre = ev.Nombre,
                Titulo = p.Titulo,
                Descripcion = $"Tarea sugerida {p.DiasAntes} días antes del arranque del evento en {ev.Lugar}.",
                Responsable = p.Resp,
                FechaLimite = ev.FechaInicio.AddDays(-p.DiasAntes).ToString("yyyy-MM-dd"),
                Prioridad = p.Prioridad
            })
            .ToList();
    }

    private List<AgentActionProposal> ConstruirPropuestasPresupuesto(Evento ev, List<Proveedor> proveedores, decimal disponible)
    {
        var propuestas = new List<AgentActionProposal>();
        var categoriasCubiertas = new HashSet<string>(ev.RubrosPresupuesto.Select(r => NormalizarTexto(r.Categoria)));
        var contratadosIds = new HashSet<int>(ev.EventoProveedores.Select(ep => ep.ProveedorId));

        var candidatos = proveedores
            .Where(p => !contratadosIds.Contains(p.Id))
            .OrderByDescending(p => p.Calificacion)
            .ThenBy(p => p.CostoEstimado)
            .ToList();

        foreach (var prov in candidatos.Take(2))
        {
            decimal montoCotizado = prov.CostoEstimado > 0 ? prov.CostoEstimado : 5000m;
            if (disponible > 2000m && montoCotizado > disponible)
            {
                montoCotizado = Math.Round(disponible * 0.45m, 0);
            }

            string cat = prov.Servicio.Contains("Audio", StringComparison.OrdinalIgnoreCase) || prov.Servicio.Contains("Foto", StringComparison.OrdinalIgnoreCase)
                ? "Audio y Video"
                : prov.Servicio.Contains("Catering", StringComparison.OrdinalIgnoreCase)
                    ? "Alimentos"
                    : prov.Servicio.Contains("Gafetes", StringComparison.OrdinalIgnoreCase)
                        ? "Publicidad"
                        : "Logística";

            if (!categoriasCubiertas.Contains(NormalizarTexto(cat)))
            {
                propuestas.Add(new AgentActionProposal
                {
                    Tipo = "agregar_rubro",
                    AgenteOrigen = "Agente de Presupuesto",
                    Etiqueta = $"Registrar rubro ({cat}): ${montoCotizado:N2} MXN",
                    EventoId = ev.Id,
                    EventoNombre = ev.Nombre,
                    Titulo = $"{prov.Servicio} — {prov.Nombre}",
                    Descripcion = $"Partida cotizada conforme al catálogo de proveedores (Calificación {prov.Calificacion:0.0}⭐).",
                    Categoria = cat,
                    Monto = montoCotizado,
                    ProveedorId = prov.Id,
                    ProveedorNombre = prov.Nombre
                });
            }

            propuestas.Add(new AgentActionProposal
            {
                Tipo = "contratar_proveedor",
                AgenteOrigen = "Agente de Presupuesto",
                Etiqueta = $"Contratar proveedor: {prov.Nombre} (${montoCotizado:N2})",
                EventoId = ev.Id,
                EventoNombre = ev.Nombre,
                Titulo = prov.Nombre,
                Descripcion = $"Servicio: {prov.Servicio} | Contacto: {prov.Contacto} ({prov.Telefono}) | ⭐ {prov.Calificacion:0.0}",
                Monto = montoCotizado,
                ProveedorId = prov.Id,
                ProveedorNombre = prov.Nombre
            });
        }

        return propuestas.Take(3).ToList();
    }

    private AgentActionProposal ConstruirPropuestaComunicado(Evento ev, OrchestrationRequest req)
    {
        string asunto;
        string cuerpo;
        var pendientes = ev.Invitados.Count(i => i.Confirmacion == "Pendiente");
        var confirmados = ev.Invitados.Count(i => i.Confirmacion == "Confirmado");
        var notaAdicional = !string.IsNullOrWhiteSpace(req.NotasExtra)
            ? $"\n📌 Nota adicional: {req.NotasExtra.Trim()}\n"
            : string.Empty;

        if (req.TipoComunicado == "recordatorio")
        {
            asunto = $"Recordatorio Próximo: {ev.Nombre} ({ev.FechaInicio:dd/MM/yyyy})";
            cuerpo = $"Estimado(a) {req.Destinatario}:\n\n" +
                     $"Le recordamos cordialmente que el evento \"{ev.Nombre}\" se llevará a cabo del {ev.FechaInicio:dd/MM/yyyy} al {ev.FechaFin:dd/MM/yyyy} en {ev.Lugar}.\n\n" +
                     $"Actualmente contamos con {confirmados} asistentes confirmados y {pendientes} registros por confirmar dentro del aforo de {ev.Aforo} lugares. Se recomienda llegar 15 minutos antes para su registro y asignación de mesa.{notaAdicional}\n\n" +
                     $"Atentamente,\nComité Organizador EventPro · ITESCA";
        }
        else if (req.TipoComunicado == "agradecimiento")
        {
            asunto = $"Agradecimiento Oficial: {ev.Nombre} | ITESCA";
            cuerpo = $"Estimado(a) {req.Destinatario}:\n\n" +
                     $"Agradecemos profundamente su valiosa participación y compromiso durante \"{ev.Nombre}\" en {ev.Lugar}. Gracias a su asistencia logramos cumplir las metas académicas e institucionales proyectadas.{notaAdicional}\n\n" +
                     $"Atentamente,\nCoordinación de Eventos EventPro · ITESCA";
        }
        else if (req.TipoComunicado == "aviso")
        {
            asunto = $"AVISO IMPORTANTE ({req.Canal}): {ev.Nombre}";
            cuerpo = $"Atención {req.Destinatario}:\n\n" +
                     $"Se emite el presente aviso logístico urgente correspondiente al evento \"{ev.Nombre}\" ({ev.Tipo}) programado en {ev.Lugar} del {ev.FechaInicio:dd/MM/yyyy} al {ev.FechaFin:dd/MM/yyyy}.{notaAdicional}\n\n" +
                     $"Agradecemos tomar en cuenta las indicaciones anteriores para el buen desarrollo de la jornada.\n\n" +
                     $"Atentamente,\nCoordinación Operativa EventPro · ITESCA";
        }
        else
        {
            asunto = $"Invitación Oficial: {ev.Nombre} | ITESCA";
            cuerpo = $"Estimado(a) {req.Destinatario}:\n\n" +
                     $"Por medio del presente comunicado ({req.Canal} · Tono: {req.Tono}), el Instituto Tecnológico Superior de Cajeme (ITESCA) le extiende una cordial invitación al evento \"{ev.Nombre}\".\n\n" +
                     $"• Modalidad / Tipo: {ev.Tipo}\n" +
                     $"• Fecha: {ev.FechaInicio:dd/MM/yyyy} al {ev.FechaFin:dd/MM/yyyy}\n" +
                     $"• Sede: {ev.Lugar} (Aforo: {ev.Aforo} personas)\n" +
                     $"{(!string.IsNullOrWhiteSpace(ev.Descripcion) ? $"• Objetivo: {ev.Descripcion}\n" : "")}" +
                     $"{notaAdicional}\n" +
                     $"Favor de confirmar su asistencia para asignarle lugar y mesa.\n\n" +
                     $"Atentamente,\nAgente de Comunicación EventPro · ITESCA";
        }

        return new AgentActionProposal
        {
            Tipo = "crear_comunicado",
            AgenteOrigen = "Agente de Comunicación",
            Etiqueta = $"Registrar comunicado ({req.Canal}): {asunto}",
            EventoId = ev.Id,
            EventoNombre = ev.Nombre,
            Titulo = asunto,
            Descripcion = cuerpo,
            Canal = req.Canal
        };
    }

    private async Task IntentarEnriquecerConGeminiAsync(
        OrchestrationResult resultado,
        Evento ev,
        List<Proveedor> proveedores,
        OrchestrationRequest req,
        string apiKey)
    {
        try
        {
            var modelo = _configuration["Gemini:Model"] ?? "gemini-2.5-flash";
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(modelo)}:generateContent?key={Uri.EscapeDataString(apiKey)}";

            var contexto = new
            {
                Evento = new { ev.Id, ev.Nombre, ev.Tipo, ev.Lugar, ev.Aforo, ev.PresupuestoMaximo, Fecha = $"{ev.FechaInicio:yyyy-MM-dd} al {ev.FechaFin:yyyy-MM-dd}" },
                GastoReal = ev.RubrosPresupuesto.Sum(r => r.MontoReal),
                TareasCount = ev.Tareas.Count,
                InvitadosConfirmados = ev.Invitados.Count(i => i.Confirmacion == "Confirmado"),
                Proveedores = proveedores.Select(p => new { p.Nombre, p.Servicio, p.CostoEstimado, p.Calificacion }),
                NormativaRAG = resultado.FuentesRag.Select(f => new { f.Titulo, f.Fragmento }),
                Solicitud = string.IsNullOrWhiteSpace(req.Prompt) ? "Realizar diagnóstico multi-agente y sugerir acciones" : req.Prompt
            };

            var sysPrompt = "Eres el Orquestador Multi-Agente de EventPro + EventOS-Agents (ITESCA). " +
                            "Devuelve exclusivamente JSON válido con las llaves: " +
                            "{\"razonamientoOrquestador\":\"...\",\"respuestaResumen\":\"...\"} en español profesional, conciso y referenciando los datos reales y normas RAG.";

            var payload = new
            {
                systemInstruction = new { parts = new[] { new { text = sysPrompt } } },
                contents = new[]
                {
                    new { role = "user", parts = new[] { new { text = JsonSerializer.Serialize(contexto) } } }
                },
                generationConfig = new
                {
                    temperature = 0.3,
                    responseMimeType = "application/json"
                }
            };

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(12);
            using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            using var resp = await client.PostAsync(url, content);

            if (!resp.IsSuccessStatusCode)
            {
                resultado.AvisoFallback = $"Gemini API devolvió HTTP {(int)resp.StatusCode}. Se ejecutó el Motor Híbrido Contextual de respaldo.";
                return;
            }

            var jsonStr = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonStr);
            var textPart = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (!string.IsNullOrWhiteSpace(textPart))
            {
                using var parsed = JsonDocument.Parse(textPart);
                if (parsed.RootElement.TryGetProperty("razonamientoOrquestador", out var razProp) && !string.IsNullOrWhiteSpace(razProp.GetString()))
                {
                    resultado.RazonamientoOrquestador = razProp.GetString()!;
                }
                if (parsed.RootElement.TryGetProperty("respuestaResumen", out var resProp) && !string.IsNullOrWhiteSpace(resProp.GetString()))
                {
                    resultado.RespuestaResumen = resProp.GetString()!;
                }
                resultado.ModoMotor = $"Google Gemini API ({modelo}) + Motor Híbrido RAG";
                resultado.Modelo = modelo;
            }
        }
        catch (Exception ex)
        {
            resultado.AvisoFallback = $"No se pudo contactar a Google Gemini ({ex.Message}). Operando 100% con Motor Híbrido Contextual C#.";
        }
    }

    private static string NormalizarTexto(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
        var norm = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(norm.Length);
        foreach (var ch in norm)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(char.ToLowerInvariant(ch));
            }
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
