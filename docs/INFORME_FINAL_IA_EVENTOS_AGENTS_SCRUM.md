# INFORME FINAL DE PROYECTO — ENTREGA SISTEMA 100% FUNCIONAL
## Inteligencia Artificial | Modalidad: Opción A — Agentes con IA

---

## 1. Datos Generales

- **Institución:** Instituto Tecnológico Superior de Cajeme (ITESCA)
- **Carrera:** Ingeniería en Sistemas Computacionales
- **Asignatura:** Inteligencia Artificial / Inteligencia Aplicada
- **Docente:** Omar López Chávez
- **Proyecto Unificado:** **EventOS-Agents integrado en EventPro** — Sistema Web Multi-Agente con Memoria RAG para la Gestión Inteligente de Eventos
- **Modalidad Seleccionada:** **Opción A: Agentes con IA** (Sistemas autónomos guiados por metas con planificación, Tool Calling sobre SQL Server, RAG y orquestación multi-agente)
- **Marco de Trabajo:** Scrum
- **Fecha de Entrega Final:** 25 de noviembre de 2026

### 1.1. Integrantes y Roles en Scrum (Conforme al Avance 1)

| # | Nombre Completo | Rol en Scrum | Responsabilidad Principal dentro del Proyecto |
|:---:|---|---|---|
| 1 | **David Eduardo Arvizu Duarte** | *Product Owner / Scrum Master* | Coordinación de requerimientos, priorización del backlog, alertas financieras y documentación del informe final. |
| 2 | **Eduardo Manzanarez Molina** | *Development Team* | Arquitectura multi-agente, diseño del Orquestador Central, ciclo de operación de agentes y gestión del proceso Scrum. |
| 3 | **Jesús Emmanuel Leal Lara** | *Development Team* | Integración del modelo de lenguaje (Gemini API + Híbrido), Prompt Engineering estructurado, Base RAG y Agente de Comunicación. |
| 4 | **Ángel Roberto Reyes Urbina** | *Development Team* | Herramientas externas (Tool Calling), APIs/Controladores, Base de Datos SQL Server (`EventProUnifiedDB`) e interconexión de servicios. |
| 5 | **Jesús Francisco Escobar Muñoz** | *Development Team* | Interfaz gráfica responsiva (Bootstrap 5 + Chart.js), Agente de Presupuesto, Reportes y compilación de evidencias de funcionamiento. |

---

## 2. Propuesta Teórica

### 2.1. Objetivo General
Diseñar, desarrollar e implementar un sistema web multi-agente de gestión de eventos capaz de planificar cronogramas, verificar disponibilidad de espacios, evaluar salud presupuestal, cotizar proveedores y redactar comunicados de forma autónoma y auditable mediante LLMs, memoria RAG y ejecución directa sobre una base de datos SQL Server.

### 2.2. Objetivos Específicos Cumplidos
1. Modelar una arquitectura multi-agente con 3 agentes especializados (**Planificador**, **Presupuesto**, **Comunicación**) + **Agente RAG**, coordinados por un **Orquestador Central**.
2. Implementar el ciclo completo de agente autónomo (**Percibir -> Razonar -> Planificar -> Actuar -> Evaluar**).
3. Dotar al sistema de **Base de Conocimiento RAG (`DocumentosConocimiento`)** para consultar reglamentos de salones, capacidades, protección civil y topes presupuestales del ITESCA.
4. Implementar **Tool Calling transaccional** mediante propuestas operativas (*Action Cards*) aplicables directamente en tablas SQL Server (`Tareas`, `RubrosPresupuesto`, `EventoProveedores`, `Comunicados`) con trazabilidad en `Bitacoras`.
5. Validar la plataforma mediante pruebas integrales de principio a fin y unificarla en un sistema web responsivo listo para producción.

### 2.3. Problemática que Resuelve y Ventajas
- **Problemática:** La fragmentación operativa en eventos institucionales genera errores humanos de cálculo presupuestal, traslapes de horarios/aforos en auditorios, retrasos con proveedores y desorganización en invitaciones.
- **Ventajas Logradas:**
  - **Disponibilidad 24/7 con motor dual:** Funciona con **Google Gemini API (`gemini-2.5-flash`)** en la nube y con un **Motor Híbrido Contextual en C#** local sin dependencia de internet (`< 50 ms`).
  - **Validación normativa automática (RAG):** Previene sobrecupo y sobrecostos antes de que ocurran.
  - **Trazabilidad total (`Bitacoras`):** Cada inferencia y acción ejecutada por los agentes queda registrada con fecha, actor y detalle auditado.

---

## 3. Arquitectura de Software e Inteligencia Artificial

### 3.1. Diagrama de Flujo del Sistema Unificado

![Arquitectura Multi-Agente Unificada EventOS-Agents + EventPro](arquitectura_eventos_agents.jpg)

```text
[ Usuario Autenticado (RBAC: Administrador / Planificador / Cliente) ]
                                  │
                                  ▼
[ Consola Multi-Agente (/Agentes) & Asistentes en Módulos Web ]
                                  │
                                  ▼
[ Orquestador Central (AiOrchestratorService.cs) ]
   ├── 1. DetectarAgente() -> Enrutamiento Semántico (Auto / Especialista)
   ├── 2. BuscarConocimientoRagAsync() -> Recuperación desde SQL Server [DocumentosConocimiento]
   ├── 3. VerificarDisponibilidadEspacioAsync() -> Validación de traslapes y aforo RAG
   ├── 4. Motor Dual IA:
   │      ├── Google Gemini API (gemini-2.5-flash, JSON Mode estructurado)
   │      └── Fallback Transparente: Motor Híbrido Contextual C#
   └── 5. Generación de Action Cards (Propuestas Operativas)
                                  │
          ┌───────────────────────┼───────────────────────┐
          ▼                       ▼                       ▼
[ Agente Planificador ]  [ Agente Presupuesto ]  [ Agente Comunicación ]
  • Tareas escalonadas     • Techo vs Gasto Real   • Invitaciones / Avisos
  • Verificación salones   • Alertas >80% / >100%  • Canal (Correo, WA, SMS)
  • Tool: Tareas.Add()     • Cotización Proveedor  • Tool: Comunicados.Add()
                                  │
                                  ▼
[ Persistencia Transaccional SQL Server (EventProUnifiedDB) + Auditoría en Bitacoras ]
```

### 3.2. Prompt Engineering y Recuperación RAG
- **Prompt Estructurado (JSON Mode):** Cuando el sistema se conecta a Google Gemini API (`gemini-2.5-flash`), la instrucción de sistema fuerza una respuesta en formato JSON (`responseMimeType = "application/json"`) inyectando el contexto real del evento desde SQL Server y los fragmentos normativos recuperados por el motor RAG.
- **Motor RAG en SQL Server (`DocumentosConocimiento`):** Almacena 5 normativas base del ITESCA (*Espacios y Salones*, *Política Financiera*, *Protocolo de Comunicación*, *Seguridad y Protección Civil*, *Contratación de Proveedores*) permitiendo además al organizador incorporar nuevos documentos desde la interfaz web. El algoritmo tokeniza y normaliza la consulta para recuperar los fragmentos con mayor puntaje de relevancia e inyectarlos al razonamiento del Orquestador.

---

## 4. Registro Completo de Actividades por Integrante (Scrum: Sprint 0 al Sprint 5)

### 4.1. Evolución de Sprints Ejecutados

| Sprint | Periodo | Objetivo Cumplido | Entregable Verificado |
|---|---|---|---|
| **Sprint 0** | 22 – 29 sep | Definición de roles Scrum, backlog inicial (US-01 a US-14) y arquitectura teórica. | Reporte Avance 1 aprobado. |
| **Sprint 1** | 30 sep – 12 oct | Entorno base, integración inicial de modelos y diseño del Agente Planificador. | Módulo de eventos y prototipo planificador. |
| **Sprint 2** | 13 – 26 oct | Agentes de Presupuesto y Comunicación, Orquestador Central y PoC demostrable. | Reporte Avance 2 (`Avance2-EventOS.pdf`). |
| **Sprint 3** | 27 oct – 9 nov | Migración unificada a **ASP.NET Core MVC + SQL Server**, Base RAG (`DocumentosConocimiento`), persistencia de Bitácora (`US-09, US-10, US-11`). | Persistencia SQL Server (`EventProUnifiedDB`). |
| **Sprint 4** | 10 – 22 nov | Unificación total con autenticación RBAC, importación CSV, módulo de Reportes con Chart.js y pruebas integrales HTTP (`US-12, US-13`). | Pruebas 100% exitosas (`verify_unified`). |
| **Sprint 5** | 23 – 25 nov | Empaquetado final para Drive (`1. Proyecto Fuente` y `2. Proyecto Ejecutable`), Script SQL y redacción del informe final (`US-14`). | Sistema 100% Funcional listo para entrega. |

### 4.2. Matriz de Cumplimiento Individual y de Historias de Usuario (US-01 a US-14)

| Integrante | Historias de Usuario | Actividades Realizadas (Sprints 1 al 5) | Resultados y Evidencias Obtenidas |
|---|---|---|---|
| **David Eduardo Arvizu Duarte** *(PO / Scrum Master)* | **US-01, US-05, US-06** | Priorización de Sprints, implementación de regla de alerta de límite presupuestal (80% preventiva / 100% crítica), validaciones de envío de comunicados y coordinación documental. | Alertas visuales activas en `/Presupuestos` y `/Agentes`; documentación unificada completa. |
| **Eduardo Manzanarez Molina** *(Development Team)* | **US-02, US-10, US-13** | Diseño del Orquestador Central en `AiOrchestratorService.cs`, flujo de las 5 etapas (`CicloAgente`), generación automática de cronogramas e implementación de `BitacoraController`. | Trazabilidad 100% auditada en SQL Server (`/Bitacora`) y código fuente ordenado con `README.md`. |
| **Jesús Emmanuel Leal Lara** *(Development Team)* | **US-09, US-14** | Implementación del motor RAG sobre la tabla `DocumentosConocimiento`, Prompt Engineering en JSON para Gemini 2.5 Flash y módulo del Agente de Comunicación multicanal. | Búsqueda normativa RAG operativa con puntajes de relevancia y redacción contextual en `/Comunicados`. |
| **Ángel Roberto Reyes Urbina** *(Development Team)* | **US-03, US-04, US-11** | Verificación automática de traslapes de salones y aforos, modelo de datos de 11 tablas en Entity Framework Core, Script `Database_EventPro_SQLServer.sql` y cotización de proveedores. | Persistencia completa en SQL Server LocalDB (`EventProUnifiedDB`) con integridad referencial. |
| **Jesús Francisco Escobar Muñoz** *(Development Team)* | **US-07, US-08, US-12** | Consola interactiva `/Agentes` con entrada en lenguaje natural, tablero de progreso y módulo estadístico `/Reportes` con Chart.js + ejecución de pruebas integrales. | Todas las vistas responsivas en Bootstrap 5.3 validadas con códigos HTTP 200 y pruebas de roles RBAC. |

---

## 5. Pruebas Integrales y Evidencias de Funcionamiento

Durante el **Sprint 4 y 5**, el equipo verificó el sistema ejecutado en `http://localhost:5045` conectado a `EventProUnifiedDB`:

1. **Prueba de Autenticación y Seguridad (RBAC):** Login verificado para `Administrador`, `Planificador` y `Cliente`. Confirmado que el rol `Cliente` es redirigido a `/Account/AccessDenied` al intentar entrar a secciones administrativas (`/Usuarios`).
2. **Prueba de Consola Multi-Agente + RAG (`POST /Agentes/Orquestar`):** Respuesta HTTP 200 con generación simultánea de traza de razonamiento, ciclo de 5 etapas, fuentes RAG recuperadas de SQL Server y propuestas operativas ejecutables.
3. **Prueba de Tool Calling Real (`POST /Agentes/AplicarAccion` y `POST /Tareas/SugerirConIa`):** Las propuestas generadas por los agentes insertaron filas reales en `Tareas`, `RubrosPresupuesto`, `EventoProveedores` y `Comunicados`, generando sus registros automáticos en `Bitacoras`.
4. **Prueba de Reportes y Exportación (`GET /Reportes` y `/Reportes/ExportarCsv`):** Gráficas Chart.js alimentadas desde SQL Server y descarga correcta de archivo `.csv`.

### Espacios para Capturas de Evidencia Final

```text
[ PEGAR AQUÍ EVIDENCIA 1: CONSOLA DEL ORQUESTADOR CON TRAZA DE RAZONAMIENTO Y FUENTES RAG ]
```

```text
[ PEGAR AQUÍ EVIDENCIA 2: TARJETAS DE PROPUESTAS (TOOL CALLING) APLICADAS EN SQL SERVER ]
```

```text
[ PEGAR AQUÍ EVIDENCIA 3: MÓDULO DE REPORTES, GRÁFICAS Y BITÁCORA AUDITADA DE AGENTES ]
```
