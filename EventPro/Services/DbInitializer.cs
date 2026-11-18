using System.Security.Cryptography;
using EventPro.Data;
using EventPro.Models;

namespace EventPro.Services;

public static class PasswordHasher
{
    private const int Iteraciones = 100_000;

    public static string Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iteraciones, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string almacenado)
    {
        var partes = almacenado.Split('.');
        if (partes.Length != 2) return false;
        byte[] salt = Convert.FromBase64String(partes[0]);
        byte[] esperado = Convert.FromBase64String(partes[1]);
        byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iteraciones, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(actual, esperado);
    }
}

public static class DbInitializer
{
    public static void Initialize(ApplicationDbContext context)
    {
        try
        {
            context.Database.EnsureCreated();
            // Verificar que todas las tablas nuevas existan (incluyendo Bitacoras y DocumentosConocimiento)
            _ = context.Bitacoras.Any();
            _ = context.DocumentosConocimiento.Any();
            _ = context.Eventos.Select(e => new { e.Tipo, e.PresupuestoMaximo, e.Descripcion }).FirstOrDefault();
            _ = context.Proveedores.Select(p => new { p.CostoEstimado, p.Calificacion }).FirstOrDefault();
            _ = context.Comunicados.Select(c => new { c.Destinatarios, c.EstadoEnvio }).FirstOrDefault();
        }
        catch
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
        }

        if (context.Usuarios.Any()) return;

        // 1. Usuarios con roles (Administrador, Planificador, Cliente) y cuentas demostrativas
        var admin = new Usuario
        {
            Nombre = "David Eduardo Arvizu Duarte (Coord. General)",
            Email = "admin@itesca.edu.mx",
            PasswordHash = PasswordHasher.Hash("Admin123*"),
            Rol = "Administrador",
            Activo = true
        };

        var planificador = new Usuario
        {
            Nombre = "Eduardo Manzanarez Molina (Planificador)",
            Email = "organizador@itesca.edu.mx",
            PasswordHash = PasswordHasher.Hash("Organiza123*"),
            Rol = "Planificador",
            Activo = true
        };

        var clienteUser = new Usuario
        {
            Nombre = "Lic. Andrea Morales (Cliente Solicitante)",
            Email = "cliente@itesca.edu.mx",
            PasswordHash = PasswordHasher.Hash("Cliente123*"),
            Rol = "Cliente",
            Activo = true
        };

        var teamDavid = new Usuario
        {
            Nombre = "David Eduardo Arvizu Duarte",
            Email = "david.arvizu@itesca.edu.mx",
            PasswordHash = PasswordHasher.Hash("Admin123*"),
            Rol = "Administrador",
            Activo = true
        };

        var teamEduardo = new Usuario
        {
            Nombre = "Eduardo Manzanarez Molina",
            Email = "eduardo.manzanarez@itesca.edu.mx",
            PasswordHash = PasswordHasher.Hash("Organiza123*"),
            Rol = "Planificador",
            Activo = true
        };

        context.Usuarios.AddRange(admin, planificador, clienteUser, teamDavid, teamEduardo);
        context.SaveChanges();

        // 2. Clientes institucionales
        var cliente1 = new Cliente
        {
            Nombre = "División de Ingeniería en Sistemas Computacionales",
            Email = "sistemas@itesca.edu.mx",
            Telefono = "644-410-8650",
            Empresa = "ITESCA"
        };
        var cliente2 = new Cliente
        {
            Nombre = "Sociedad de Alumnos ITESCA",
            Email = "sociedad.alumnos@itesca.edu.mx",
            Telefono = "644-410-8651",
            Empresa = "Comité Estudiantil"
        };
        var cliente3 = new Cliente
        {
            Nombre = "Coordinación de Extensión y Difusión Cultural",
            Email = "cultura@itesca.edu.mx",
            Telefono = "644-410-8655",
            Empresa = "ITESCA"
        };
        context.Clientes.AddRange(cliente1, cliente2, cliente3);
        context.SaveChanges();

        // 3. Catálogo de 6 Proveedores verificados con CostoEstimado y Calificación
        var p1 = new Proveedor { Nombre = "AudioPro Sonora", Contacto = "Carlos Ruiz", Telefono = "644-123-4567", Servicio = "Audio y Proyección", CostoEstimado = 14500m, Calificacion = 4.9 };
        var p2 = new Proveedor { Nombre = "Banquetes Sabor Sonorense", Contacto = "Leticia Valenzuela", Telefono = "644-234-5678", Servicio = "Catering y Coffee Break", CostoEstimado = 18500m, Calificacion = 4.8 };
        var p3 = new Proveedor { Nombre = "Impresos Horizonte", Contacto = "Jorge Soto", Telefono = "644-345-6789", Servicio = "Gafetes, Lonas y Señalética", CostoEstimado = 4800m, Calificacion = 4.7 };
        var p4 = new Proveedor { Nombre = "PhotoFrame Estudio", Contacto = "Ana Lucía Castro", Telefono = "644-456-7890", Servicio = "Fotografía y Streaming", CostoEstimado = 7800m, Calificacion = 4.9 };
        var p5 = new Proveedor { Nombre = "Eventos Seguros Cajeme", Contacto = "Marco Antonio Ibarra", Telefono = "644-567-8901", Servicio = "Mobiliario, Vallas y Protección Civil", CostoEstimado = 6500m, Calificacion = 4.6 };
        var p6 = new Proveedor { Nombre = "Redes y Conectividad Yaqui", Contacto = "Ing. Héctor Valdez", Telefono = "644-678-9012", Servicio = "Enlace Dedicado WiFi y Streaming", CostoEstimado = 8200m, Calificacion = 4.8 };
        context.Proveedores.AddRange(p1, p2, p3, p4, p5, p6);
        context.SaveChanges();

        // 4. Eventos completos (con Tipo, PresupuestoMaximo y Descripcion)
        var ev1 = new Evento
        {
            Nombre = "Congreso de Inteligencia Artificial y Ciberseguridad 2026",
            Tipo = "Tecnológico",
            FechaInicio = DateTime.Today.AddDays(15),
            FechaFin = DateTime.Today.AddDays(17),
            Lugar = "Centro de Convenciones ITESCA",
            Estado = "Planificado",
            Aforo = 350,
            PresupuestoMaximo = 55000m,
            Descripcion = "Ciclo de conferencias magistrales y talleres prácticos sobre agentes autónomos, LLMs y ciberseguridad aplicada.",
            ClienteId = cliente1.Id,
            OrganizadorId = planificador.Id
        };

        var ev2 = new Evento
        {
            Nombre = "Hackathon Universitario de Innovación Tecnológica",
            Tipo = "Tecnológico",
            FechaInicio = DateTime.Today.AddDays(25),
            FechaFin = DateTime.Today.AddDays(26),
            Lugar = "Laboratorios de Cómputo Edificio C",
            Estado = "En coordinación",
            Aforo = 180,
            PresupuestoMaximo = 35000m,
            Descripcion = "Maratón de programación de 24 horas para resolver retos reales de empresas de Cajeme con software e IA.",
            ClienteId = cliente2.Id,
            OrganizadorId = planificador.Id
        };

        var ev3 = new Evento
        {
            Nombre = "Feria Cultural y Muestra Emprendedora ITESCA",
            Tipo = "Cultural",
            FechaInicio = DateTime.Today.AddDays(10),
            FechaFin = DateTime.Today.AddDays(11),
            Lugar = "Explanada Cívica Principal ITESCA",
            Estado = "Planificado",
            Aforo = 450,
            PresupuestoMaximo = 40000m,
            Descripcion = "Encuentro artístico, stands de proyectos estudiantiles e integración con la comunidad universitaria.",
            ClienteId = cliente3.Id,
            OrganizadorId = admin.Id
        };

        context.Eventos.AddRange(ev1, ev2, ev3);
        context.SaveChanges();

        // 5. Rubros Presupuestales
        context.RubrosPresupuesto.AddRange(
            new RubroPresupuesto { EventoId = ev1.Id, Concepto = "Renta de equipo audiovisual y pantallas LED", Categoria = "Audio y Video", MontoEstimado = 15000m, MontoReal = 14500m },
            new RubroPresupuesto { EventoId = ev1.Id, Concepto = "Coffee break para ponentes y asistentes (3 días)", Categoria = "Alimentos", MontoEstimado = 22000m, MontoReal = 21200m },
            new RubroPresupuesto { EventoId = ev1.Id, Concepto = "Impresión de kits, gafetes y reconocimientos", Categoria = "Publicidad", MontoEstimado = 6500m, MontoReal = 6500m },
            new RubroPresupuesto { EventoId = ev2.Id, Concepto = "Premios y reconocimientos para equipos ganadores", Categoria = "Logística", MontoEstimado = 18000m, MontoReal = 17500m },
            new RubroPresupuesto { EventoId = ev2.Id, Concepto = "Pizzas, bebidas y snacks para 24 horas continuas", Categoria = "Alimentos", MontoEstimado = 12000m, MontoReal = 12000m },
            new RubroPresupuesto { EventoId = ev3.Id, Concepto = "Toldos, templetes y mesas de exhibición", Categoria = "Logística", MontoEstimado = 14000m, MontoReal = 13200m },
            new RubroPresupuesto { EventoId = ev3.Id, Concepto = "Lonas y señalética promocional", Categoria = "Publicidad", MontoEstimado = 5000m, MontoReal = 4800m }
        );

        // 6. Tareas y Cronograma
        context.Tareas.AddRange(
            new Tarea { EventoId = ev1.Id, Titulo = "Confirmar agenda de ponentes magistrales nacionales", Responsable = "Eduardo Manzanarez Molina", FechaLimite = DateTime.Today.AddDays(5), Prioridad = "Alta", Estado = "En proceso" },
            new Tarea { EventoId = ev1.Id, Titulo = "Prueba técnica de streaming y red de alta velocidad", Responsable = "Ángel Roberto Reyes Urbina", FechaLimite = DateTime.Today.AddDays(10), Prioridad = "Alta", Estado = "Pendiente" },
            new Tarea { EventoId = ev1.Id, Titulo = "Diseño y publicación de convocatoria en portal oficial", Responsable = "Jesús Emmanuel Leal Lara", FechaLimite = DateTime.Today.AddDays(-2), Prioridad = "Media", Estado = "Completada" },
            new Tarea { EventoId = ev2.Id, Titulo = "Configurar servidores dedicados y credenciales de acceso", Responsable = "Ángel Roberto Reyes Urbina", FechaLimite = DateTime.Today.AddDays(8), Prioridad = "Alta", Estado = "Pendiente" },
            new Tarea { EventoId = ev2.Id, Titulo = "Validar rúbricas del jurado evaluador y premios", Responsable = "David Eduardo Arvizu Duarte", FechaLimite = DateTime.Today.AddDays(-1), Prioridad = "Alta", Estado = "Pendiente" },
            new Tarea { EventoId = ev3.Id, Titulo = "Asignar stands de emprendimiento y tomas eléctricas", Responsable = "Jesús Francisco Escobar Muñoz", FechaLimite = DateTime.Today.AddDays(4), Prioridad = "Media", Estado = "En proceso" }
        );

        // 7. Invitados
        context.Invitados.AddRange(
            new Invitado { EventoId = ev1.Id, Nombre = "Dr. Alejandro Morales", Email = "amorales@cinvestav.mx", Telefono = "55-1122-3344", Confirmacion = "Confirmado", Mesa = "Presídium" },
            new Invitado { EventoId = ev1.Id, Nombre = "Dra. Sofía Navarro", Email = "snavarro@itesm.mx", Telefono = "81-9988-7766", Confirmacion = "Confirmado", Mesa = "Presídium" },
            new Invitado { EventoId = ev1.Id, Nombre = "Ing. Manuel Félix", Email = "mfelix@gmail.com", Telefono = "644-555-1234", Confirmacion = "Pendiente", Mesa = "Bloque A" },
            new Invitado { EventoId = ev1.Id, Nombre = "Mtra. Claudia Quiñónez", Email = "cquinonez@itesca.edu.mx", Telefono = "644-410-8622", Confirmacion = "Confirmado", Mesa = "Bloque A" },
            new Invitado { EventoId = ev2.Id, Nombre = "Comité Evaluador de Proyectos", Email = "jurado@itesca.edu.mx", Telefono = "644-777-8899", Confirmacion = "Confirmado", Mesa = "Mesa Jurado" },
            new Invitado { EventoId = ev2.Id, Nombre = "Ing. Patricia Bórquez", Email = "pborquez@techsonora.mx", Telefono = "644-888-9900", Confirmacion = "No asistirá", Mesa = "Mesa Jurado" },
            new Invitado { EventoId = ev3.Id, Nombre = "Patronato Universitario Cajeme", Email = "patronato@itesca.edu.mx", Telefono = "644-321-4321", Confirmacion = "Confirmado", Mesa = "VIP 1" }
        );

        // 8. Contrataciones Evento-Proveedor
        context.EventoProveedores.AddRange(
            new EventoProveedor { EventoId = ev1.Id, ProveedorId = p1.Id, Costo = 14500m, FechaContratacion = DateTime.Today.AddDays(-10) },
            new EventoProveedor { EventoId = ev1.Id, ProveedorId = p2.Id, Costo = 21200m, FechaContratacion = DateTime.Today.AddDays(-8) },
            new EventoProveedor { EventoId = ev2.Id, ProveedorId = p2.Id, Costo = 12000m, FechaContratacion = DateTime.Today.AddDays(-5) },
            new EventoProveedor { EventoId = ev3.Id, ProveedorId = p5.Id, Costo = 13200m, FechaContratacion = DateTime.Today.AddDays(-4) }
        );

        // 9. Comunicados
        context.Comunicados.AddRange(
            new Comunicado
            {
                EventoId = ev1.Id,
                Asunto = "Invitación Oficial: Congreso de IA y Ciberseguridad 2026",
                Contenido = "Estimada comunidad universitaria y profesional:\n\nNos complace invitarles a la inauguración oficial del Congreso de IA y Ciberseguridad 2026 en el Centro de Convenciones ITESCA.\n\nFavor de confirmar su registro oportunamente.",
                Canal = "Correo electrónico",
                Destinatarios = "Invitados Confirmados y Presídium",
                EstadoEnvio = "Enviado (4 destinatarios)",
                FechaEnvio = DateTime.Now.AddDays(-3),
                EnviadoPorId = admin.Id
            },
            new Comunicado
            {
                EventoId = ev2.Id,
                Asunto = "Convocatoria Abierta: Hackathon Universitario ITESCA 2026",
                Contenido = "Se invita a todos los estudiantes de Ingeniería en Sistemas Computacionales a inscribir sus equipos de hasta 4 integrantes para el reto de 24 horas en Laboratorios del Edificio C.",
                Canal = "Circular Interna",
                Destinatarios = "Comunidad Estudiantil ISC",
                EstadoEnvio = "Enviado (180 destinatarios)",
                FechaEnvio = DateTime.Now.AddDays(-1),
                EnviadoPorId = planificador.Id
            }
        );

        // 10. Base de Conocimiento RAG (Reglamentos, Espacios, Políticas y Normativas ITESCA - US-09)
        context.DocumentosConocimiento.AddRange(
            new DocumentoConocimiento
            {
                Titulo = "Catálogo Oficial y Reglas de Espacios y Salones ITESCA",
                Categoria = "Espacios y Salones",
                PalabrasClave = "salon, auditorio, centro de convenciones, laboratorio, explanada, aforo, disponibilidad, espacio, horario",
                Contenido = "1) Centro de Convenciones ITESCA: Capacidad máxima 400 personas, requiere solicitud con 10 días de anticipación y técnico audiovisual asignado. 2) Auditorio Principal ITESCA: Capacidad máxima 250 personas, horario de 08:00 a 20:00 hrs. 3) Laboratorios de Cómputo Edificio C: Capacidad 180 participantes distribuidos en 4 salas, requiere soporte TI para red dedicada y extensión eléctrica aprobada para hackathones. 4) Explanada Cívica Principal ITESCA: Capacidad hasta 600 personas, requiere toldos y plan de Protección Civil para eventos al aire libre.",
                FechaActualizacion = DateTime.Today.AddDays(-15)
            },
            new DocumentoConocimiento
            {
                Titulo = "Política de Control Presupuestal y Topes de Gasto",
                Categoria = "Política Financiera",
                PalabrasClave = "presupuesto, gasto, limite, sobrecosto, cotizacion, factura, alerta, proveedor, financiamiento",
                Contenido = "Todo evento universitario debe operar con un techo presupuestal autorizado (PresupuestoMaximo). Cuando el gasto real alcance o supere el 85% del techo presupuestal, el Agente de Presupuesto debe emitir una alerta preventiva al Product Owner/Organizador. Ningún rubro debe superar el 100% sin autorización del Administrador. Se requiere al menos comparar el costo estimado con el catálogo homologado de proveedores locales antes de formalizar contratación.",
                FechaActualizacion = DateTime.Today.AddDays(-12)
            },
            new DocumentoConocimiento
            {
                Titulo = "Protocolo de Comunicación Oficial y Difusión Multicanal",
                Categoria = "Protocolo Comunicación",
                PalabrasClave = "comunicado, invitacion, recordatorio, correo, sms, whatsapp, circular, tono, difusion, asistentes",
                Contenido = "Las invitaciones formales para autoridades y ponentes externos deben enviarse por Correo Institucional con al menos 7 días naturales de anticipación incluyendo sede, fecha, aforo y enlace de confirmación. Los recordatorios urgentes a estudiantes deben enviarse 48 horas antes mediante SMS/WhatsApp o Circular Interna, indicando llegar 15 minutos antes para agilizar el control de acceso y asignación de mesa.",
                FechaActualizacion = DateTime.Today.AddDays(-10)
            },
            new DocumentoConocimiento
            {
                Titulo = "Lineamientos de Seguridad, Protección Civil y Logística de Eventos",
                Categoria = "Seguridad y Logística",
                PalabrasClave = "seguridad, proteccion civil, primeros auxilios, acceso, aforo, riesgo, cronograma, logistica",
                Contenido = "En eventos con aforo superior a 150 personas es obligatorio contar con señalética de rutas de evacuación, brigada de primeros auxilios o personal de Eventos Seguros Cajeme, y registro de lista nominal de invitados confirmados para control de ingreso en puerta.",
                FechaActualizacion = DateTime.Today.AddDays(-8)
            },
            new DocumentoConocimiento
            {
                Titulo = "Política de Contratación de Proveedores Externos",
                Categoria = "Proveedores",
                PalabrasClave = "proveedor, servicio, audio, banquete, catering, fotografia, streaming, contrato, tarifas",
                Contenido = "Los proveedores deben encontrarse activos en el catálogo institucional con calificación igual o superior a 4.5 estrellas. Servicios de audio y streaming para congresos académicos deben probarse 48 horas antes del arranque para evitar fallas técnicas.",
                FechaActualizacion = DateTime.Today.AddDays(-5)
            }
        );

        // 11. Registros iniciales de Bitácora Auditada
        context.Bitacoras.AddRange(
            new Bitacora
            {
                Fecha = DateTime.Now.AddHours(-6),
                Tipo = "Sistema",
                Accion = "Inicialización de Base de Datos Unificada",
                Detalle = "Se inicializó EventProUnifiedDB en SQL Server con las 11 tablas operativas y el motor multi-agente.",
                Usuario = "Sistema EF Core"
            },
            new Bitacora
            {
                Fecha = DateTime.Now.AddHours(-4),
                Tipo = "Agente",
                Accion = "Orquestación Multi-Agente inicial (RAG + SQL Server)",
                Detalle = "Orquestador Central verificó disponibilidad del Centro de Convenciones ITESCA (RAG) y auditó el presupuesto del Congreso de IA.",
                Usuario = "Orquestador Central IA",
                EventoId = ev1.Id
            },
            new Bitacora
            {
                Fecha = DateTime.Now.AddHours(-2),
                Tipo = "Agente",
                Accion = "Alerta preventiva de techo presupuestal detectada",
                Detalle = "Agente de Presupuesto evaluó Hackathon Universitario: 84.3% de utilización del presupuesto máximo ($29,500 de $35,000).",
                Usuario = "Agente Presupuesto IA",
                EventoId = ev2.Id
            },
            new Bitacora
            {
                Fecha = DateTime.Now.AddMinutes(-45),
                Tipo = "Seguridad",
                Accion = "Configuración de roles RBAC completada",
                Detalle = "Políticas de acceso configuradas para Administrador, Planificador y Cliente con cookies HttpOnly.",
                Usuario = "David Eduardo Arvizu Duarte"
            }
        );

        context.SaveChanges();
    }
}
