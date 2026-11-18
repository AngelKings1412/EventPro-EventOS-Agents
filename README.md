# EventPro + EventOS-Agents — Sistema Web Unificado con Multi-Agente IA y RAG (ITESCA)

Plataforma web integral desarrollada en **ASP.NET Core MVC (.NET 10)** + **Entity Framework Core 10** + **Microsoft SQL Server / LocalDB (`EventProUnifiedDB`)** + **Bootstrap 5.3** + **Chart.js 4.4**, que unifica los requerimientos de:
- **Tópicos Avanzados de Programación Web** (Autenticación PBKDF2-SHA256, Roles RBAC, 11 Tablas Relacionales, CRUDs completos, Estudio de Comunicados Multicanal y Reportes Estadísticos con Chart.js + CSV/PDF).
- **Inteligencia Artificial — Modalidad Opción A: Agentes con IA** (Orquestador Central Multi-Agente, Ciclo Operativo de 5 Etapas, Base de Conocimiento Normativa RAG en SQL Server, Motor Dual con Google Gemini `gemini-2.5-flash` + Fallback Híbrido C# y Ejecución Transaccional *Tool Calling*).

---

## 👥 Equipo de Desarrollo (ITESCA — Marco Scrum)
1. **David Eduardo Arvizu Duarte** — *Product Owner / Scrum Master*
2. **Eduardo Manzanarez Molina** — *Development Team* (Arquitectura Multi-Agente, Orquestador Central & Bitácora)
3. **Jesús Emmanuel Leal Lara** — *Development Team* (LLM Gemini API, Prompt Engineering, Base RAG & Agente de Comunicación)
4. **Ángel Roberto Reyes Urbina** — *Development Team* (SQL Server `EventProUnifiedDB`, Controladores MVC & Tool Calling)
5. **Jesús Francisco Escobar Muñoz** — *Development Team* (Interfaz Responsiva Bootstrap 5.3, UX/UI, Reportes Chart.js & Pruebas E2E)

---

## 📂 Estructura del Repositorio

```text
EventPro-EventOS-Agents/
├── EventPro/                                             # Código fuente completo ASP.NET Core MVC (.NET 10)
│   ├── Controllers/                                      # 12 Controladores protegidos con [Authorize] y Roles RBAC
│   ├── Data/ApplicationDbContext.cs                      # Contexto EF Core 10 (11 tablas relacionales)
│   ├── Models/                                           # Entidades de dominio y ViewModels
│   ├── Services/
│   │   ├── AiOrchestratorService.cs                      # Orquestador Multi-Agente + RAG + Verificación Salones + Tool Calling
│   │   └── DbInitializer.cs                              # Hashing PBKDF2-SHA256 (100k iter.) + Siembra automática de datos
│   ├── Views/                                            # Vistas Razor responsivas (Sidebar + Topbar + Módulos)
│   ├── wwwroot/                                          # Estilos CSS (auditados con Impeccable UX) y scripts
│   ├── Database_EventPro_SQLServer.sql                   # Script DDL/DML completo de las 11 tablas en SQL Server
│   └── EventPro.csproj                                   # Proyecto .NET 10
├── docs/
│   ├── DOCUMENTACION_TECNICA_Y_MANUAL_USUARIO_EVENTPRO.md # Documentación Técnica y Manual de Usuario (Programación Web)
│   └── INFORME_FINAL_IA_EVENTOS_AGENTS_SCRUM.md           # Informe Final de IA, Arquitectura Multi-Agente, RAG y Scrum
├── GUIA_PRESENTACION_FINAL_EVENTPRO_EVENTOS_AGENTS.pdf   # Guía completa para exposición (14 diapositivas + guion + Q&A)
└── README.md
```

---

## 🧩 Módulos Implementados

### 🌐 Módulos Web & Base de Datos
- **Autenticación y Roles RBAC (`/Account`)**: Inicio de sesión con `PBKDF2-SHA256` (100,000 iteraciones + salt aleatorio), Cookie Auth `HttpOnly`, cierre de sesión auditado y restricción por roles (`Administrador`, `Planificador`, `Cliente`).
- **Dashboard Ejecutivo (`/Dashboard`)**: 4 KPIs en tiempo real (*Eventos Activos*, *Presupuesto Ejecutado*, *Intervenciones IA*, *Tareas Vencidas*), próximos eventos y tareas prioritarias.
- **Eventos (`/Eventos`)**: CRUD completo y vista de Detalles organizada en **6 pestañas** (*Resumen*, *Presupuesto*, *Cronograma*, *Invitados*, *Proveedores* y *Bitácora*).
- **Presupuesto (`/Presupuestos`)**: Control de *Monto Estimado vs Monto Real*, barra porcentual frente a `PresupuestoMaximo`, alertas preventivas/críticas y análisis con IA.
- **Tareas (`/Tareas`)**: Cronograma operativo con filtros por evento/estado, cambio rápido de estatus y sugerencia de tareas con IA.
- **Invitados (`/Invitados`)**: Control de confirmaciones e **importación masiva mediante archivo o texto CSV**.
- **Proveedores (`/Proveedores`) & Clientes (`/Clientes`)**: Catálogo homologado con tarifas, calificación en estrellas y contratación por evento.
- **Estudio de Comunicados (`/Comunicados`)**: Compositor asistido por IA + historial filtrable, cálculo real de destinatarios en SQL Server (*Confirmados*, *Pendientes*, *Todos*, *Proveedores*), reenvío y eliminación.
- **Reportes y Estadísticas (`/Reportes`)**: 3 gráficas interactivas con **Chart.js** (*Comparativa Presupuestal*, *Estatus de Tareas*, *Confirmación de Invitados*), exportación a **CSV (Excel)** e **impresión limpia a PDF**.
- **Bitácora (`/Bitacora`) & Usuarios (`/Usuarios`)**: Auditoría transaccional filtrable y administración de cuentas exclusiva para `Administrador`.

### 🤖 Orquestador Multi-Agente IA & Memoria RAG (`/Agentes`)
- **Ciclo Operativo en 5 Etapas**: `1. Percibir` → `2. Razonar` → `3. Planificar` → `4. Actuar` → `5. Evaluar`.
- **Agentes Especializados**: *Agente Planificador* (salones, aforo y cronograma), *Agente de Presupuesto* (alertas financieras y proveedores), *Agente de Comunicación* (difusión multicanal) y *Agente RAG* (normativas ITESCA en `DocumentosConocimiento`).
- **Motor Dual Resiliente**: Conecta con **Google Gemini API (`gemini-2.5-flash`)** en JSON Mode estructurado y cuenta con **Motor Híbrido Contextual en C#** (`< 50 ms`) para operar aún sin conexión o sin API Key externa.
- **Tool Calling Transaccional**: Ejecución de propuestas con un clic directamente sobre `Tareas`, `RubrosPresupuesto`, `EventoProveedores` y `Comunicados`.

---

## 🚀 Requisitos y Ejecución Rápida

1. Tener instalado **.NET 10 SDK** y **SQL Server LocalDB** (incluido con Visual Studio o SQL Server Express).
2. Clonar el repositorio y ejecutar:

```powershell
git clone https://github.com/AngelKings1412/EventPro-EventOS-Agents.git
cd EventPro-EventOS-Agents/EventPro
dotnet run
```

3. Abrir en el navegador: **`http://localhost:5045`**
   *(Al iniciar por primera vez, `DbInitializer` crea automáticamente la base de datos `EventProUnifiedDB` con las 11 tablas y carga los datos de demostración).*

---

## 🔐 Credenciales de Demostración (RBAC)

| Rol | Correo | Contraseña | Nivel de Acceso |
|---|---|---|---|
| **Administrador** | `admin@itesca.edu.mx` | `Admin123*` | Acceso total al sistema, gestión de usuarios, eliminaciones y operación IA. |
| **Planificador** | `organizador@itesca.edu.mx` | `Organiza123*` | Creación/edición de eventos, rubros, tareas, invitados CSV, comunicados y agentes IA. |
| **Cliente** | `cliente@itesca.edu.mx` | `Cliente123*` | Solo lectura en módulos autorizados; bloqueado en edición y en `/Usuarios` (`AccessDenied`). |
