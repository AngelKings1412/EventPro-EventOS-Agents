# EventPro + EventOS-Agents — Sistema Web Unificado con Multi-Agente IA y RAG (ITESCA)

Plataforma web unificada desarrollada en **ASP.NET Core MVC (.NET 10)** + **Entity Framework Core** + **Microsoft SQL Server / LocalDB** + **Bootstrap 5.3**, que integra todos los requerimientos de **Tópicos Avanzados de Programación Web** y **Inteligencia Artificial (Opción A: Agentes con IA)**.

---

## 👥 Equipo de Desarrollo (ITESCA — Marco Scrum)
1. **David Eduardo Arvizu Duarte** — *Product Owner / Scrum Master*
2. **Eduardo Manzanarez Molina** — *Development Team* (Arquitectura Multi-Agente & Orquestador)
3. **Jesús Emmanuel Leal Lara** — *Development Team* (LLM, Prompt Engineering, RAG & Agente de Comunicación)
4. **Ángel Roberto Reyes Urbina** — *Development Team* (SQL Server, APIs/Controladores & Tool Calling)
5. **Jesús Francisco Escobar Muñoz** — *Development Team* (Frontend Bootstrap, Reportes Chart.js & Agente de Presupuesto)

---

## 🧩 Módulos Integrados

### 🌐 Módulos Web & Base de Datos (Tópicos Avanzados)
- **Autenticación y Roles RBAC (`/Account`)**: Login PBKDF2-SHA256, Cookie Auth HttpOnly, cierre de sesión y perfiles `Administrador`, `Planificador`, `Cliente`.
- **Dashboard Ejecutivo (`/Dashboard`)**: KPIs en tiempo real e historial de ejecuciones IA.
- **Eventos (`/Eventos`)**: CRUD completo con pestañas de Presupuesto, Tareas, Invitados, Proveedores y Comunicados.
- **Presupuesto (`/Presupuestos`)**: Control estimado vs real, techo máximo autorizado y alertas automáticas (80% / 100%).
- **Tareas (`/Tareas`)**: Cronograma con filtros por estado/evento.
- **Invitados (`/Invitados`)**: Control de confirmaciones e **importación masiva desde CSV**.
- **Proveedores (`/Proveedores`) & Clientes (`/Clientes`)**: Catálogos completos y contrataciones de servicios.
- **Comunicados (`/Comunicados`)**: Historial multicanal (Correo, WhatsApp, SMS, Circular).
- **Reportes y Estadísticas (`/Reportes`)**: 3 gráficas interactivas Chart.js, 3 tablas consolidadas, **exportación a CSV (Excel)** e **impresión a PDF**.
- **Usuarios (`/Usuarios`)**: Administración exclusiva para rol `Administrador`.

### 🤖 Módulos Multi-Agente IA & RAG (Inteligencia Artificial)
- **Consola del Orquestador Multi-Agente (`/Agentes`)**: Auto-routing semántico, ciclo de 5 etapas (*Percibir -> Razonar -> Planificar -> Actuar -> Evaluar*), motor dual (**Google Gemini API `gemini-2.5-flash` + Motor Híbrido Contextual C#**).
- **3 Agentes Especializados**: Planificador (cronograma y disponibilidad de salones), Presupuesto (salud financiera y cotizaciones), Comunicación (redacción institucional).
- **Tool Calling Transaccional**: Propuestas operativas ejecutables con 1 clic directamente en tablas SQL Server.
- **Base de Conocimiento RAG (`DocumentosConocimiento`)**: Normativas, reglamentos de espacios/aforos ITESCA y políticas presupuestales consultadas en vivo.
- **Bitácora Auditada (`/Bitacora`)**: Registro inmutable de acciones humanas, accesos de seguridad e intervenciones de los agentes.

---

## 🗄️ Base de Datos (11 Tablas SQL Server)
`Usuarios`, `Clientes`, `Proveedores`, `Eventos`, `RubrosPresupuesto`, `EventoProveedores`, `Tareas`, `Invitados`, `Comunicados`, `DocumentosConocimiento`, `Bitacoras`.
- Respaldo completo DDL incluido en `Database_EventPro_SQLServer.sql`.

---

## 🚀 Cómo Ejecutar el Proyecto

```powershell
cd "EventPro"
dotnet run
```

Abre el navegador en **`http://localhost:5045`** (o el puerto indicado en consola). Al arrancar por primera vez, Entity Framework Core crea e inicializa automáticamente la base de datos **`EventProUnifiedDB`** en SQL Server LocalDB.

---

## 🔐 Cuentas Demostrativas

| Rol | Correo | Contraseña | Permisos |
|---|---|---|---|
| **Administrador** | `admin@itesca.edu.mx` | `Admin123*` | Acceso total, gestión de usuarios, eliminaciones y operación IA. |
| **Planificador** | `organizador@itesca.edu.mx` | `Organiza123*` | Gestión de eventos, rubros, tareas, invitados CSV, comunicados y agentes IA. |
| **Cliente** | `cliente@itesca.edu.mx` | `Cliente123*` | Consulta de eventos, reportes y bitácora (sin permisos de edición o administración). |
