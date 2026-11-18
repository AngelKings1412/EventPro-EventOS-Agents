# DOCUMENTACIÓN TÉCNICA Y MANUAL DE USUARIO — PROYECTO FINAL
## EventPro: Sistema Web Integrado con Arquitectura Multi-Agente IA y RAG

---

## 1. Portada

- **Institución:** Instituto Tecnológico Superior de Cajeme (ITESCA)
- **Carrera:** Ingeniería en Sistemas Computacionales
- **Asignatura:** Tópicos Avanzados de Programación Web
- **Proyecto:** **EventPro** — Sistema Web Unificado para Planificación, Presupuesto, Control Operativo y Orquestación Inteligente de Eventos (Integración con EventOS-Agents)
- **Fecha de Entrega:** 26 de noviembre de 2026
- **Equipo Integrante (Scrum):**
  1. David Eduardo Arvizu Duarte
  2. Eduardo Manzanarez Molina
  3. Jesús Emmanuel Leal Lara
  4. Ángel Roberto Reyes Urbina
  5. Jesús Francisco Escobar Muñoz

---

## 2. Introducción

**EventPro** es una aplicación web integral construida sobre **ASP.NET Core MVC (.NET 10)** con **Entity Framework Core** y **Microsoft SQL Server**, diseñada para administrar de principio a fin el ciclo de vida de eventos académicos, tecnológicos, culturales, deportivos y corporativos. 

Como resultado de la unificación estratégica con el módulo de **Inteligencia Artificial (EventOS-Agents)**, el sistema no solo centraliza operaciones CRUD transaccionales con autenticación y control de acceso basado en roles (RBAC), sino que incorpora un **Orquestador Multi-Agente con recuperación aumentada por generación (RAG)** conectado en tiempo real a SQL Server para asistir a organizadores en cronogramas, salud financiera, cotización de proveedores y difusión multicanal.

---

## 3. Objetivo y Problemática Resuelta

### 3.1. Problemática que resuelve
La coordinación de eventos universitarios suele gestionarse mediante hojas de cálculo dispersas, correos aislados y registros manuales sin control presupuestal ni validación de disponibilidad de sedes. Esto provoca sobrecostos no detectados a tiempo, omisiones en tareas logísticas, traslape de auditorios y falta de reportes ejecutivos confiables.

### 3.2. Objetivo General
Implementar un prototipo web responsivo, seguro y conectado a base de datos SQL Server que integre autenticación por roles, administración completa de eventos y recursos, un módulo estadístico con gráficas e indicadores, y agentes de IA capaces de razonar sobre políticas institucionales y ejecutar acciones directamente en la base de datos.

---

## 4. Stack Tecnológico

| Capa | Tecnología / Herramienta | Función en el Sistema |
|---|---|---|
| **Framework Web** | ASP.NET Core MVC (.NET 10) | Controladores MVC, Razor Views, Middleware HTTP, CSRF y Routing |
| **ORM / Persistencia** | Entity Framework Core 10 (`SqlServer` & `Design`) | Mapeo objeto-relacional de 11 entidades, integridad referencial y Code-First / Script SQL |
| **Base de Datos** | Microsoft SQL Server / LocalDB (`EventProUnifiedDB`) | Almacenamiento relacional persistente con restricciones PK, FK e índices únicos |
| **Seguridad y Auth** | Cookie Authentication + PBKDF2-SHA256 + RBAC | Hashing seguro de contraseñas (100,000 iteraciones), sesiones HttpOnly y autorización por roles |
| **Frontend / Diseño** | Bootstrap 5.3 + Bootstrap Icons 1.11 + CSS3 | Interfaz responsiva adaptada a escritorio, tableta y móvil |
| **Reportes y Gráficas** | Chart.js 4.4 + Exportación CSV + CSS `@media print` | Gráficas comparativas de presupuesto, tareas, invitados y reportes descargables |
| **Inteligencia Artificial** | Motor Multi-Agente C# (`AiOrchestratorService`) + RAG + Google Gemini API (`gemini-2.5-flash`) | Orquestador central, 3 agentes especializados, Tool Calling en SQL Server y memoria RAG |

---

## 5. Arquitectura del Sistema

El sistema sigue el patrón **Modelo-Vista-Controlador (MVC)** enriquecido con una **Capa de Servicios de Dominio e IA**:

```text
[ Navegador Web (Bootstrap 5.3 + Chart.js) ]
                  │  HTTPS / HTTP + Cookies Auth + AntiForgeryToken
                  ▼
[ Controladores ASP.NET Core MVC ([Authorize] + Roles RBAC) ]
   ├── AccountController (Login PBKDF2, Logout, AccessDenied)
   ├── DashboardController (KPIs en tiempo real + Bitácora IA)
   ├── AgentesController (Consola Multi-Agente + RAG + Tool Calling)
   ├── Eventos / Presupuestos / Tareas / Invitados / Proveedores / Clientes / Comunicados
   ├── ReportesController (Estadísticas, Chart.js, ExportarCsv)
   └── UsuariosController [Solo Administrador] / BitacoraController
                  │
                  ▼
[ Capa de Servicios e IA ]
   ├── AiOrchestratorService (Orquestador + RAG + Verificación Espacios + Gemini/Híbrido)
   └── DbInitializer & PasswordHasher (Siembra inicial + PBKDF2-SHA256)
                  │
                  ▼
[ Entity Framework Core 10 (ApplicationDbContext) ]
                  │  Consultas LINQ Parametrizadas
                  ▼
[ Microsoft SQL Server — EventProUnifiedDB (11 Tablas) ]
```

---

## 6. Diseño de Base de Datos (Modelo Relacional — 11 Tablas)

El modelo relacional se define en `Data/ApplicationDbContext.cs` y se respalda en `Database_EventPro_SQLServer.sql`:

| # | Tabla | Descripción | Relaciones Principales |
|---|---|---|---|
| 1 | `Usuarios` | Cuentas de acceso, contraseña hasheada PBKDF2, Rol (`Administrador`, `Planificador`, `Cliente`) y estado `Activo`. | 1:N con `Eventos` y `Comunicados` |
| 2 | `Clientes` | Dependencias, empresas o comités solicitantes de eventos. | 1:N con `Eventos` |
| 3 | `Eventos` | Núcleo del sistema (`Nombre`, `Tipo`, `FechaInicio`, `FechaFin`, `Lugar`, `Aforo`, `Estado`, `PresupuestoMaximo`, `Descripcion`). | N:1 `Clientes`/`Usuarios`, 1:N hijos |
| 4 | `RubrosPresupuesto` | Partidas financieras (`Concepto`, `Categoria`, `MontoEstimado`, `MontoReal`). | N:1 con `Eventos` (Cascade) |
| 5 | `Proveedores` | Catálogo de proveedores (`Nombre`, `Servicio`, `Contacto`, `Telefono`, `CostoEstimado`, `Calificacion`). | N:M mediante `EventoProveedores` |
| 6 | `EventoProveedores` | Tabla intermedia de contrataciones (`EventoId`, `ProveedorId`, `Costo`, `FechaContratacion`). | N:1 `Eventos`, N:1 `Proveedores` |
| 7 | `Tareas` | Cronograma operativo (`Titulo`, `Responsable`, `FechaLimite`, `Prioridad`, `Estado`). | N:1 con `Eventos` (Cascade) |
| 8 | `Invitados` | Lista nominal de asistentes (`Nombre`, `Email`, `Telefono`, `Mesa`, `Confirmacion`). | N:1 con `Eventos` (Cascade) |
| 9 | `Comunicados` | Registro de avisos enviados (`Asunto`, `Contenido`, `Canal`, `FechaEnvio`, `EnviadoPorId`). | N:1 `Eventos`, N:1 `Usuarios` |
| 10 | `DocumentosConocimiento` | Base documental institucional consultada por el motor RAG de los agentes IA. | Independiente consultada por RAG |
| 11 | `Bitacoras` | Auditoría transaccional de acciones humanas, seguridad y ejecuciones de IA. | Referencia opcional a `EventoId` |

> **Nota de Despliegue:** Al iniciar la aplicación, `DbInitializer.Initialize()` crea automáticamente la base `EventProUnifiedDB` si no existe y carga los datos iniciales de demostración; adicionalmente se entrega el archivo `Database_EventPro_SQLServer.sql` para creación manual en cualquier instancia de SQL Server.

---

## 7. Módulos Implementados

1. **Autenticación y Seguridad (`/Account`)**: Inicio de sesión con validación de credenciales PBKDF2, mantenimiento de sesión mediante Cookie cifrada HttpOnly, denegación de acceso (`/Account/AccessDenied`) y cierre de sesión con auditoría.
2. **Dashboard Ejecutivo (`/Dashboard`)**: Tarjetas KPI de Eventos Activos, Presupuesto Ejecutado, Intervenciones de Agentes IA, Tareas Vencidas, próximos eventos, tareas prioritarias y últimas acciones de agentes.
3. **Consola Multi-Agente IA y RAG (`/Agentes`)**: Orquestación automática o dirigida hacia los agentes Planificador, Presupuesto, Comunicación y RAG, verificación de disponibilidad de salones, alertas de sobrecosto y ejecución de propuestas directamente en SQL Server.
4. **Eventos (`/Eventos`)**: CRUD completo (Listar, Crear, Editar, Detalles con 6 pestañas organizativas y Eliminar exclusivo para Administrador).
5. **Presupuesto por Rubros (`/Presupuestos`)**: Control de Monto Estimado vs Real, barra de progreso respecto al `PresupuestoMaximo`, alertas al 80%/100% y asistente IA de partidas.
6. **Tareas y Cronograma (`/Tareas`)**: Programación de actividades, filtros por evento y estado, cambio rápido de estado y generador automático de cronogramas con el Agente Planificador IA.
7. **Invitados e Importación CSV (`/Invitados`)**: Alta individual, conteo en tiempo real de confirmaciones, cambio de estatus con un clic e **importación masiva mediante archivo `.csv` o texto CSV**.
8. **Proveedores y Contrataciones (`/Proveedores`)**: Administración del catálogo homologado con tarifas y calificaciones, además de asignación de contratos por evento.
9. **Clientes (`/Clientes`)**: Alta, edición y eliminación de dependencias o empresas solicitantes.
10. **Comunicados Multicanal (`/Comunicados`)**: Historial de difusión por Correo, WhatsApp, SMS o Circular Interna, con redactor generativo asistido por IA.
11. **Reportes y Estadísticas (`/Reportes`)**: Módulo analítico con filtro por evento, indicadores globales, 3 gráficas dinámicas Chart.js, 3 tablas ejecutivas, **exportación a CSV (Excel)** e **impresión a PDF**.
12. **Bitácora (`/Bitacora`) y Usuarios (`/Usuarios`)**: Auditoría filtrable por tipo/texto y gestión completa de usuarios/roles exclusiva para Administrador.

---

## 8. Autenticación, Roles y Matriz de Permisos

### 8.1. Cuentas Demostrativas Pre-cargadas

| Rol | Correo de Acceso | Contraseña | Nivel de Acceso |
|---|---|---|---|
| **Administrador** | `admin@itesca.edu.mx` | `Admin123*` | Acceso total al sistema, gestión de usuarios, eliminación de eventos/clientes/proveedores y operación IA. |
| **Planificador** | `organizador@itesca.edu.mx` | `Organiza123*` | Creación/edición de eventos, rubros, tareas, invitados CSV, contrataciones, comunicados, RAG y ejecución de propuestas IA. |
| **Cliente** | `cliente@itesca.edu.mx` | `Cliente123*` | Acceso en modo consulta a Dashboard, Eventos, Presupuestos, Reportes, Bitácora y consulta de Agentes (sin permisos de edición o escritura). |

### 8.2. Restricción Técnica en Controladores
- Todos los controladores operativos poseen `[Authorize]`, bloqueando peticiones anónimas y redirigiendo a `/Account/Login`.
- Las acciones de escritura (`Create`, `Edit`, `ImportarCsv`, `AplicarAccion`, `SugerirConIa`) exigen `[Authorize(Roles = "Administrador,Planificador")]`.
- Las acciones críticas (`Delete` de eventos/clientes/proveedores y todo `UsuariosController`) exigen `[Authorize(Roles = "Administrador")]`.
- Si un usuario con rol `Cliente` intenta acceder a `/Usuarios`, el sistema lo redirige automáticamente a `/Account/AccessDenied`.

---

## 9. Módulo de Reportes y Estadísticas

Ubicado en `/Reportes`, consulta directamente mediante Entity Framework Core los registros vivos de `Eventos`, `RubrosPresupuesto`, `Tareas`, `Invitados`, `EventoProveedores` y `Bitacoras`:
- **Gráfica 1 (Barras):** Comparativa por evento entre *Techo Máximo Autorizado*, *Presupuesto Estimado* y *Gasto Real Ejecutado*.
- **Gráfica 2 (Dona):** Distribución del cronograma de tareas (*Completadas*, *En proceso*, *Pendientes*, *Vencidas*).
- **Gráfica 3 (Dona):** Estatus de asistencia de invitados (*Confirmados*, *Pendientes*, *No asistirá*).
- **Exportación:** Descarga instantánea en `.csv` compatible con Microsoft Excel (`/Reportes/ExportarCsv`) e impresión limpia en PDF con ocultamiento automático de barras de navegación (`@media print`).

---

## 10. Manual de Usuario y Evidencias Operativas

### 10.1. Requisitos e Inicio del Sistema
1. Contar con **.NET 10 Runtime / SDK** y **SQL Server LocalDB** (incluido con Visual Studio / SQL Server Express LocalDB).
2. Ejecutar desde la carpeta del proyecto:
   ```powershell
   dotnet run
   ```
   *(O en la versión compilada ejecutable, hacer doble clic en `INICIAR_SISTEMA.bat`).*
3. Abrir el navegador en `http://localhost:5045`.

### 10.2. Flujo de Uso Paso a Paso

#### Paso 1: Inicio de Sesión
Ingresa con cualquiera de las credenciales de prueba (recomendado `admin@itesca.edu.mx` / `Admin123*` para explorar todos los módulos).

```text
[ PEGAR AQUÍ CAPTURA 1: PANTALLA DE INICIO DE SESIÓN (/Account/Login) ]
```

#### Paso 2: Panel Principal (Dashboard) y Navegación
Visualiza las tarjetas resumen y utiliza el menú superior responsivo para navegar entre los módulos.

```text
[ PEGAR AQUÍ CAPTURA 2: DASHBOARD GENERAL (/Dashboard) ]
```

#### Paso 3: Orquestador Multi-Agente IA y RAG
Ingresa a **Agentes IA**, selecciona un evento y pulsa **«Orquestación Completa»**. Revisa la alerta de disponibilidad de salón, el diagnóstico financiero y haz clic en **«Aplicar en SQL Server»** sobre cualquier propuesta operativa.

```text
[ PEGAR AQUÍ CAPTURA 3: CONSOLA MULTI-AGENTE IA Y PROPUESTAS (/Agentes) ]
```

#### Paso 4: Gestión de Eventos, Presupuesto, Tareas e Invitados (CSV)
Desde **Eventos -> Detalles** inspecciona las 6 pestañas organizativas del evento, registra rubros en **Presupuesto** o importa asistentes en **Invitados** mediante CSV.

```text
[ PEGAR AQUÍ CAPTURA 4: DETALLES DE EVENTO / INVITADOS CSV ]
```

#### Paso 5: Reportes Estadísticos, Bitácora y Control de Roles
Accede a **Reportes** para visualizar las 3 gráficas Chart.js y exportar a CSV; posteriormente visita **Bitácora** y **Usuarios** (exclusivo Administrador) para validar la trazabilidad y seguridad RBAC.

```text
[ PEGAR AQUÍ CAPTURA 5: MÓDULO DE REPORTES CON GRÁFICAS CHART.JS (/Reportes) ]
```

```text
[ PEGAR AQUÍ CAPTURA 6: ADMINISTRACIÓN DE USUARIOS / BITÁCORA AUDITADA ]
```

---

## 11. Conclusiones

La integración de **EventPro** con **EventOS-Agents** demuestra exitosamente cómo una arquitectura web empresarial en **ASP.NET Core MVC + Entity Framework Core + SQL Server** puede potenciarse mediante **Agentes Autónomos de IA y memoria RAG**. Se cumplieron al 100% los requerimientos de seguridad (PBKDF2 y RBAC), diseño responsivo (Bootstrap 5.3), persistencia relacional (11 tablas), analítica estadística (Chart.js + CSV) y automatización inteligente auditada en bitácora.
