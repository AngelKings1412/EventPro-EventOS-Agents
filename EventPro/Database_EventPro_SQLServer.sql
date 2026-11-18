-- ============================================================================
-- Script SQL Server Completo: EventPro + EventOS-Agents (Sistema Unificado)
-- Asignatura: Tópicos Avanzados de Programación Web + Inteligencia Artificial
-- Institución: Instituto Tecnológico Superior de Cajeme (ITESCA)
-- Motor: Microsoft SQL Server / LocalDB
-- ============================================================================

IF DB_ID(N'EventProUnifiedDB') IS NULL
BEGIN
    CREATE DATABASE [EventProUnifiedDB];
END
GO

USE [EventProUnifiedDB];
GO

-- 1. Tabla Usuarios
IF OBJECT_ID(N'dbo.Usuarios', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Usuarios (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Nombre NVARCHAR(100) NOT NULL,
        Email NVARCHAR(150) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(255) NOT NULL,
        Rol NVARCHAR(30) NOT NULL DEFAULT 'Planificador', -- Administrador, Planificador, Cliente
        Activo BIT NOT NULL DEFAULT 1
    );
END
GO

-- 2. Tabla Clientes
IF OBJECT_ID(N'dbo.Clientes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Clientes (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Nombre NVARCHAR(150) NOT NULL,
        Email NVARCHAR(150) NOT NULL DEFAULT '',
        Telefono NVARCHAR(30) NOT NULL DEFAULT '',
        Empresa NVARCHAR(150) NOT NULL DEFAULT ''
    );
END
GO

-- 3. Tabla Proveedores
IF OBJECT_ID(N'dbo.Proveedores', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Proveedores (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Nombre NVARCHAR(150) NOT NULL,
        Contacto NVARCHAR(100) NOT NULL DEFAULT '',
        Telefono NVARCHAR(30) NOT NULL DEFAULT '',
        Servicio NVARCHAR(100) NOT NULL DEFAULT '',
        CostoEstimado DECIMAL(18,2) NOT NULL DEFAULT 5000.00,
        Calificacion FLOAT NOT NULL DEFAULT 4.8
    );
END
GO

-- 4. Tabla Eventos
IF OBJECT_ID(N'dbo.Eventos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Eventos (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Nombre NVARCHAR(200) NOT NULL,
        FechaInicio DATE NOT NULL,
        FechaFin DATE NOT NULL,
        Lugar NVARCHAR(200) NOT NULL DEFAULT '',
        Estado NVARCHAR(40) NOT NULL DEFAULT 'Planificado',
        Tipo NVARCHAR(60) NOT NULL DEFAULT 'Académico',
        PresupuestoMaximo DECIMAL(18,2) NOT NULL DEFAULT 50000.00,
        Descripcion NVARCHAR(500) NOT NULL DEFAULT '',
        Aforo INT NOT NULL DEFAULT 100,
        ClienteId INT NULL CONSTRAINT FK_Eventos_Clientes FOREIGN KEY REFERENCES dbo.Clientes(Id) ON DELETE SET NULL,
        OrganizadorId INT NULL CONSTRAINT FK_Eventos_Usuarios FOREIGN KEY REFERENCES dbo.Usuarios(Id) ON DELETE SET NULL
    );
END
GO

-- 5. Tabla RubrosPresupuesto
IF OBJECT_ID(N'dbo.RubrosPresupuesto', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RubrosPresupuesto (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        EventoId INT NOT NULL CONSTRAINT FK_Rubros_Eventos FOREIGN KEY REFERENCES dbo.Eventos(Id) ON DELETE CASCADE,
        Concepto NVARCHAR(150) NOT NULL,
        Categoria NVARCHAR(60) NOT NULL DEFAULT 'Logística',
        MontoEstimado DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        MontoReal DECIMAL(18,2) NOT NULL DEFAULT 0.00
    );
END
GO

-- 6. Tabla EventoProveedores (Relación N:M Evento <-> Proveedor)
IF OBJECT_ID(N'dbo.EventoProveedores', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EventoProveedores (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        EventoId INT NOT NULL CONSTRAINT FK_EP_Eventos FOREIGN KEY REFERENCES dbo.Eventos(Id) ON DELETE CASCADE,
        ProveedorId INT NOT NULL CONSTRAINT FK_EP_Proveedores FOREIGN KEY REFERENCES dbo.Proveedores(Id) ON DELETE CASCADE,
        Costo DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        FechaContratacion DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE)
    );
END
GO

-- 7. Tabla Tareas
IF OBJECT_ID(N'dbo.Tareas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tareas (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        EventoId INT NOT NULL CONSTRAINT FK_Tareas_Eventos FOREIGN KEY REFERENCES dbo.Eventos(Id) ON DELETE CASCADE,
        Titulo NVARCHAR(200) NOT NULL,
        FechaLimite DATE NOT NULL,
        Responsable NVARCHAR(100) NOT NULL DEFAULT '',
        Estado NVARCHAR(30) NOT NULL DEFAULT 'Pendiente',
        Prioridad NVARCHAR(20) NOT NULL DEFAULT 'Media'
    );
END
GO

-- 8. Tabla Invitados
IF OBJECT_ID(N'dbo.Invitados', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Invitados (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        EventoId INT NOT NULL CONSTRAINT FK_Invitados_Eventos FOREIGN KEY REFERENCES dbo.Eventos(Id) ON DELETE CASCADE,
        Nombre NVARCHAR(150) NOT NULL,
        Email NVARCHAR(150) NOT NULL DEFAULT '',
        Telefono NVARCHAR(30) NOT NULL DEFAULT '',
        Confirmacion NVARCHAR(30) NOT NULL DEFAULT 'Pendiente',
        Mesa NVARCHAR(50) NOT NULL DEFAULT ''
    );
END
GO

-- 9. Tabla Comunicados
IF OBJECT_ID(N'dbo.Comunicados', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Comunicados (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        EventoId INT NOT NULL CONSTRAINT FK_Comunicados_Eventos FOREIGN KEY REFERENCES dbo.Eventos(Id) ON DELETE CASCADE,
        Asunto NVARCHAR(200) NOT NULL,
        Contenido NVARCHAR(MAX) NOT NULL,
        Canal NVARCHAR(40) NOT NULL DEFAULT 'Correo electrónico',
        Destinatarios NVARCHAR(120) NOT NULL DEFAULT 'Todos los invitados',
        EstadoEnvio NVARCHAR(80) NOT NULL DEFAULT 'Enviado',
        FechaEnvio DATETIME2 NOT NULL DEFAULT GETDATE(),
        EnviadoPorId INT NULL CONSTRAINT FK_Comunicados_Usuarios FOREIGN KEY REFERENCES dbo.Usuarios(Id) ON DELETE SET NULL
    );
END
GO

-- 10. Tabla DocumentosConocimiento (Memoria RAG para Agentes IA)
IF OBJECT_ID(N'dbo.DocumentosConocimiento', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DocumentosConocimiento (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Titulo NVARCHAR(200) NOT NULL,
        Categoria NVARCHAR(80) NOT NULL DEFAULT 'Normativa ITESCA',
        Contenido NVARCHAR(MAX) NOT NULL,
        PalabrasClave NVARCHAR(300) NOT NULL DEFAULT '',
        FechaActualizacion DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE)
    );
END
GO

-- 11. Tabla Bitacoras (Auditoría Transaccional y Trazabilidad Multi-Agente)
IF OBJECT_ID(N'dbo.Bitacoras', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Bitacoras (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Fecha DATETIME2 NOT NULL DEFAULT GETDATE(),
        Tipo NVARCHAR(40) NOT NULL DEFAULT 'Sistema',
        Accion NVARCHAR(200) NOT NULL,
        Detalle NVARCHAR(MAX) NOT NULL DEFAULT '',
        Usuario NVARCHAR(120) NOT NULL DEFAULT 'Sistema',
        EventoId INT NULL
    );
END
GO

-- Nota: Al ejecutar `dotnet run` en EventPro, DbInitializer siembra automáticamente
-- usuarios con contraseñas PBKDF2, clientes, proveedores, eventos, rubros,
-- tareas, invitados, comunicados, documentos RAG y bitácora inicial.
