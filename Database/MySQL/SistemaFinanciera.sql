-- Sistema financiero: importar este archivo desde phpMyAdmin > Importar.
-- MySQL 8.0.16+ / MariaDB 10.6+. Codificación UTF-8 e InnoDB.
-- Crea una base nueva o completa una base con este mismo esquema; no migra esquemas existentes.
-- No elimina tablas ni registros. Las operaciones financieras deben ejecutarse en transacciones.
CREATE DATABASE IF NOT EXISTS sistema_financiera
  CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE sistema_financiera;
SET NAMES utf8mb4;
SET time_zone = '+00:00';

CREATE TABLE IF NOT EXISTS Roles (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  Codigo VARCHAR(30) NOT NULL UNIQUE,
  Nombre VARCHAR(80) NOT NULL
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS Usuarios (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  NombreUsuario VARCHAR(60) NOT NULL UNIQUE,
  NombreCompleto VARCHAR(160) NOT NULL,
  Correo VARCHAR(254) NOT NULL UNIQUE,
  ContrasenaHash VARCHAR(255) NOT NULL COMMENT 'Hash con algoritmo y parámetros; nunca texto plano',
  Activo TINYINT(1) NOT NULL DEFAULT 1,
  CreadoUtc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT ck_usuario_activo CHECK (Activo IN (0,1))
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS UsuarioRoles (
  UsuarioId INT NOT NULL,
  RolId INT NOT NULL,
  PRIMARY KEY (UsuarioId, RolId),
  CONSTRAINT fk_ur_usuario FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id),
  CONSTRAINT fk_ur_rol FOREIGN KEY (RolId) REFERENCES Roles(Id)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS Clientes (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  TipoPersona VARCHAR(10) NOT NULL,
  TipoIdentificacion VARCHAR(10) NOT NULL,
  Identificacion VARCHAR(30) NOT NULL UNIQUE,
  Nombres VARCHAR(120) NOT NULL,
  Apellidos VARCHAR(120) NOT NULL DEFAULT '',
  FechaNacimiento DATE NULL,
  Sexo VARCHAR(10) NOT NULL,
  EstadoCivil VARCHAR(15) NOT NULL,
  Telefono VARCHAR(8) NOT NULL,
  Correo VARCHAR(254) NOT NULL,
  Direccion VARCHAR(300) NOT NULL,
  Provincia VARCHAR(80) NOT NULL,
  Distrito VARCHAR(80) NOT NULL,
  Corregimiento VARCHAR(80) NOT NULL,
  Ocupacion VARCHAR(200) NOT NULL,
  Ingresos DECIMAL(14,2) NOT NULL,
  Referencias VARCHAR(1000) NOT NULL,
  Activo TINYINT(1) NOT NULL DEFAULT 1,
  Version BIGINT UNSIGNED NOT NULL DEFAULT 1 COMMENT 'Incrementar con cada UPDATE; no equivale a rowversion de SQL Server',
  CreadoUtc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  ModificadoUtc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  INDEX ix_clientes_nombre (Nombres, Apellidos),
  CONSTRAINT ck_cliente_persona CHECK (TipoPersona IN ('NATURAL','JURIDICA')),
  CONSTRAINT ck_cliente_identificacion CHECK (TipoIdentificacion IN ('CEDULA','PASAPORTE','RUC')),
  CONSTRAINT ck_cliente_ingresos CHECK (Ingresos >= 0),
  CONSTRAINT ck_cliente_activo CHECK (Activo IN (0,1)),
  CONSTRAINT ck_cliente_juridico CHECK (TipoPersona <> 'JURIDICA' OR TipoIdentificacion = 'RUC')
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS MetodosCalculo (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  Codigo VARCHAR(30) NOT NULL UNIQUE,
  Nombre VARCHAR(100) NOT NULL,
  Descripcion VARCHAR(500) NOT NULL,
  Activo TINYINT(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS Periodicidades (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  Codigo VARCHAR(30) NOT NULL UNIQUE,
  Nombre VARCHAR(80) NOT NULL,
  Unidad VARCHAR(10) NOT NULL COMMENT 'DIAS o MESES; el calendario del contrato define fechas',
  Intervalo INT NOT NULL,
  PeriodosPorAnio DECIMAL(10,4) NOT NULL,
  CONSTRAINT ck_periodicidad_unidad CHECK (Unidad IN ('DIAS','MESES')),
  CONSTRAINT ck_periodicidad_intervalo CHECK (Intervalo > 0 AND PeriodosPorAnio > 0)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS Estados (
  Codigo VARCHAR(30) PRIMARY KEY,
  Nombre VARCHAR(80) NOT NULL
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS ProductosFinancieros (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  Codigo VARCHAR(30) NOT NULL UNIQUE,
  Nombre VARCHAR(120) NOT NULL,
  Descripcion VARCHAR(500) NOT NULL,
  MontoMinimo DECIMAL(14,2) NOT NULL,
  MontoMaximo DECIMAL(14,2) NOT NULL,
  PlazoMinimo INT NOT NULL COMMENT 'Número de cuotas',
  PlazoMaximo INT NOT NULL COMMENT 'Número de cuotas',
  Activo TINYINT(1) NOT NULL DEFAULT 1,
  CONSTRAINT ck_producto_montos CHECK (MontoMinimo > 0 AND MontoMaximo >= MontoMinimo),
  CONSTRAINT ck_producto_plazos CHECK (PlazoMinimo > 0 AND PlazoMaximo >= PlazoMinimo)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS ProductoMetodos (
  ProductoId INT NOT NULL,
  MetodoId INT NOT NULL,
  PRIMARY KEY (ProductoId, MetodoId),
  CONSTRAINT fk_pm_producto FOREIGN KEY (ProductoId) REFERENCES ProductosFinancieros(Id),
  CONSTRAINT fk_pm_metodo FOREIGN KEY (MetodoId) REFERENCES MetodosCalculo(Id)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS Tasas (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  ProductoId INT NOT NULL,
  Porcentaje DECIMAL(9,6) NOT NULL COMMENT 'Porcentaje anual; 12 representa 12 por ciento',
  TipoTasa VARCHAR(15) NOT NULL COMMENT 'NOMINAL_ANUAL o EFECTIVA_ANUAL',
  CapitalizacionId INT NOT NULL,
  VigenteDesde DATE NOT NULL,
  VigenteHasta DATE NULL,
  UNIQUE KEY uq_tasa_producto (Id, ProductoId),
  CONSTRAINT fk_tasa_producto FOREIGN KEY (ProductoId) REFERENCES ProductosFinancieros(Id),
  CONSTRAINT fk_tasa_capitalizacion FOREIGN KEY (CapitalizacionId) REFERENCES Periodicidades(Id),
  CONSTRAINT ck_tasa_valor CHECK (Porcentaje >= 0),
  CONSTRAINT ck_tasa_tipo CHECK (TipoTasa IN ('NOMINAL_ANUAL','EFECTIVA_ANUAL')),
  CONSTRAINT ck_tasa_fechas CHECK (VigenteHasta IS NULL OR VigenteHasta >= VigenteDesde)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS Solicitudes (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  NumeroSolicitud VARCHAR(40) NOT NULL UNIQUE,
  ClienteId INT NOT NULL,
  ProductoId INT NOT NULL,
  MetodoId INT NOT NULL,
  TasaId INT NOT NULL,
  PeriodicidadId INT NOT NULL,
  FechaSolicitud DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  MontoSolicitado DECIMAL(14,2) NOT NULL,
  CantidadCuotas INT NOT NULL,
  EstadoCodigo VARCHAR(30) NOT NULL DEFAULT 'SOLICITUD',
  CreadoPor INT NOT NULL,
  EvaluadoPor INT NULL,
  FechaEvaluacion DATETIME NULL,
  Observaciones VARCHAR(1000) NULL,
  UNIQUE KEY uq_solicitud_contexto (Id, ClienteId, ProductoId),
  CONSTRAINT fk_sol_cliente FOREIGN KEY (ClienteId) REFERENCES Clientes(Id),
  CONSTRAINT fk_sol_metodo FOREIGN KEY (ProductoId, MetodoId) REFERENCES ProductoMetodos(ProductoId, MetodoId),
  CONSTRAINT fk_sol_tasa FOREIGN KEY (TasaId, ProductoId) REFERENCES Tasas(Id, ProductoId),
  CONSTRAINT fk_sol_periodicidad FOREIGN KEY (PeriodicidadId) REFERENCES Periodicidades(Id),
  CONSTRAINT fk_sol_estado FOREIGN KEY (EstadoCodigo) REFERENCES Estados(Codigo),
  CONSTRAINT fk_sol_creador FOREIGN KEY (CreadoPor) REFERENCES Usuarios(Id),
  CONSTRAINT fk_sol_evaluador FOREIGN KEY (EvaluadoPor) REFERENCES Usuarios(Id),
  CONSTRAINT ck_sol_monto CHECK (MontoSolicitado > 0 AND CantidadCuotas > 0),
  CONSTRAINT ck_sol_estado CHECK (EstadoCodigo IN ('SOLICITUD','EN_EVALUACION','APROBADO','RECHAZADO'))
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS Prestamos (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  NumeroPrestamo VARCHAR(40) NOT NULL UNIQUE,
  SolicitudId INT NOT NULL UNIQUE,
  ClienteId INT NOT NULL,
  ProductoId INT NOT NULL,
  MetodoId INT NOT NULL,
  PeriodicidadId INT NOT NULL,
  CapitalizacionId INT NOT NULL,
  FechaAprobacion DATETIME NOT NULL,
  FechaDesembolso DATETIME NULL,
  FechaPrimerPago DATE NOT NULL,
  MontoAprobado DECIMAL(14,2) NOT NULL,
  TasaInteres DECIMAL(9,6) NOT NULL COMMENT 'Copia contractual del porcentaje anual; no depende de cambios del catálogo',
  TipoTasa VARCHAR(15) NOT NULL,
  CantidadCuotas INT NOT NULL,
  Comision DECIMAL(14,2) NOT NULL DEFAULT 0,
  GastosAsociados DECIMAL(14,2) NOT NULL DEFAULT 0,
  SaldoCapital DECIMAL(14,2) NOT NULL COMMENT 'Saldo contable; actualizar atómicamente al aplicar pagos/capitalizar',
  EstadoCodigo VARCHAR(30) NOT NULL DEFAULT 'APROBADO',
  AprobadoPor INT NOT NULL,
  Version BIGINT UNSIGNED NOT NULL DEFAULT 1,
  UNIQUE KEY uq_prestamo_cliente (Id, ClienteId),
  INDEX ix_prestamo_estado (EstadoCodigo),
  CONSTRAINT fk_pre_solicitud FOREIGN KEY (SolicitudId, ClienteId, ProductoId) REFERENCES Solicitudes(Id, ClienteId, ProductoId),
  CONSTRAINT fk_pre_metodo FOREIGN KEY (ProductoId, MetodoId) REFERENCES ProductoMetodos(ProductoId, MetodoId),
  CONSTRAINT fk_pre_periodicidad FOREIGN KEY (PeriodicidadId) REFERENCES Periodicidades(Id),
  CONSTRAINT fk_pre_capitalizacion FOREIGN KEY (CapitalizacionId) REFERENCES Periodicidades(Id),
  CONSTRAINT fk_pre_estado FOREIGN KEY (EstadoCodigo) REFERENCES Estados(Codigo),
  CONSTRAINT fk_pre_usuario FOREIGN KEY (AprobadoPor) REFERENCES Usuarios(Id),
  CONSTRAINT ck_pre_importes CHECK (MontoAprobado > 0 AND TasaInteres >= 0 AND CantidadCuotas > 0 AND Comision >= 0 AND GastosAsociados >= 0 AND SaldoCapital >= 0),
  CONSTRAINT ck_pre_tasa CHECK (TipoTasa IN ('NOMINAL_ANUAL','EFECTIVA_ANUAL')),
  CONSTRAINT ck_pre_fechas CHECK (FechaDesembolso IS NULL OR FechaDesembolso >= FechaAprobacion),
  CONSTRAINT ck_pre_estado CHECK (EstadoCodigo IN ('APROBADO','DESEMBOLSADO','VIGENTE','EN_MORA','CANCELADO','REFINANCIADO'))
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS Cuotas (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  PrestamoId INT NOT NULL,
  NumeroCuota INT NOT NULL,
  FechaVencimiento DATE NOT NULL,
  Capital DECIMAL(14,2) NOT NULL,
  Interes DECIMAL(14,2) NOT NULL,
  InteresCapitalizado DECIMAL(14,2) NOT NULL DEFAULT 0 COMMENT 'Interés incorporado al principal, no cobrado en esta cuota',
  Mora DECIMAL(14,2) NOT NULL DEFAULT 0,
  OtrosCargos DECIMAL(14,2) NOT NULL DEFAULT 0,
  SaldoProyectado DECIMAL(14,2) NOT NULL,
  UNIQUE KEY uq_cuota_numero (PrestamoId, NumeroCuota),
  UNIQUE KEY uq_cuota_prestamo (Id, PrestamoId),
  INDEX ix_cuota_vencimiento (FechaVencimiento),
  CONSTRAINT fk_cuota_prestamo FOREIGN KEY (PrestamoId) REFERENCES Prestamos(Id),
  CONSTRAINT ck_cuota_valores CHECK (NumeroCuota > 0 AND Capital >= 0 AND Interes >= 0 AND InteresCapitalizado >= 0 AND Mora >= 0 AND OtrosCargos >= 0 AND SaldoProyectado >= 0)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS Pagos (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  NumeroRecibo VARCHAR(40) NOT NULL UNIQUE,
  PrestamoId INT NOT NULL,
  ClienteId INT NOT NULL,
  FechaPago DATETIME NOT NULL,
  MontoPagado DECIMAL(14,2) NOT NULL,
  SaldoCapitalPosterior DECIMAL(14,2) NOT NULL,
  RegistradoPor INT NOT NULL,
  Estado VARCHAR(10) NOT NULL DEFAULT 'APLICADO',
  AnuladoPor INT NULL,
  MotivoAnulacion VARCHAR(500) NULL,
  UNIQUE KEY uq_pago_prestamo (Id, PrestamoId),
  INDEX ix_pago_fecha (FechaPago),
  CONSTRAINT fk_pago_prestamo FOREIGN KEY (PrestamoId, ClienteId) REFERENCES Prestamos(Id, ClienteId),
  CONSTRAINT fk_pago_usuario FOREIGN KEY (RegistradoPor) REFERENCES Usuarios(Id),
  CONSTRAINT fk_pago_anulador FOREIGN KEY (AnuladoPor) REFERENCES Usuarios(Id),
  CONSTRAINT ck_pago_valores CHECK (MontoPagado > 0 AND SaldoCapitalPosterior >= 0),
  CONSTRAINT ck_pago_estado CHECK (Estado IN ('APLICADO','ANULADO')),
  CONSTRAINT ck_pago_anulacion CHECK (Estado <> 'ANULADO' OR (AnuladoPor IS NOT NULL AND MotivoAnulacion IS NOT NULL))
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS AplicacionesPago (
  PagoId INT NOT NULL,
  CuotaId INT NOT NULL,
  PrestamoId INT NOT NULL,
  Capital DECIMAL(14,2) NOT NULL DEFAULT 0,
  Interes DECIMAL(14,2) NOT NULL DEFAULT 0,
  Mora DECIMAL(14,2) NOT NULL DEFAULT 0,
  OtrosCargos DECIMAL(14,2) NOT NULL DEFAULT 0,
  PRIMARY KEY (PagoId, CuotaId),
  CONSTRAINT fk_aplicacion_pago FOREIGN KEY (PagoId, PrestamoId) REFERENCES Pagos(Id, PrestamoId),
  CONSTRAINT fk_aplicacion_cuota FOREIGN KEY (CuotaId, PrestamoId) REFERENCES Cuotas(Id, PrestamoId),
  CONSTRAINT ck_aplicacion_importes CHECK (Capital >= 0 AND Interes >= 0 AND Mora >= 0 AND OtrosCargos >= 0 AND Capital + Interes + Mora + OtrosCargos > 0)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS Refinanciamientos (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  PrestamoOrigenId INT NOT NULL,
  PrestamoNuevoId INT NOT NULL UNIQUE,
  ClienteId INT NOT NULL,
  Fecha DATETIME NOT NULL,
  SaldoTransferido DECIMAL(14,2) NOT NULL,
  Motivo VARCHAR(1000) NOT NULL,
  RegistradoPor INT NOT NULL,
  CONSTRAINT fk_ref_origen FOREIGN KEY (PrestamoOrigenId, ClienteId) REFERENCES Prestamos(Id, ClienteId),
  CONSTRAINT fk_ref_nuevo FOREIGN KEY (PrestamoNuevoId, ClienteId) REFERENCES Prestamos(Id, ClienteId),
  CONSTRAINT fk_ref_usuario FOREIGN KEY (RegistradoPor) REFERENCES Usuarios(Id),
  CONSTRAINT ck_ref_valores CHECK (PrestamoOrigenId <> PrestamoNuevoId AND SaldoTransferido >= 0)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS Auditoria (
  Id BIGINT AUTO_INCREMENT PRIMARY KEY,
  UsuarioId INT NOT NULL,
  FechaUtc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  Accion VARCHAR(40) NOT NULL,
  Entidad VARCHAR(60) NOT NULL,
  RegistroId BIGINT NOT NULL,
  Detalle LONGTEXT NULL COMMENT 'Descripción o JSON sin contraseñas ni secretos',
  INDEX ix_auditoria_registro (Entidad, RegistroId),
  INDEX ix_auditoria_fecha (FechaUtc),
  CONSTRAINT fk_auditoria_usuario FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id)
) ENGINE=InnoDB;

-- Catálogos iniciales sin usuarios ni contraseñas predeterminadas.
-- Se insertan por código, sin depender de valores AUTO_INCREMENT específicos.
INSERT INTO Roles (Codigo, Nombre) SELECT 'ADMINISTRADOR','Administrador' WHERE NOT EXISTS (SELECT 1 FROM Roles WHERE Codigo='ADMINISTRADOR');
INSERT INTO Roles (Codigo, Nombre) SELECT 'OFICIAL_CREDITO','Oficial de crédito' WHERE NOT EXISTS (SELECT 1 FROM Roles WHERE Codigo='OFICIAL_CREDITO');
INSERT INTO Roles (Codigo, Nombre) SELECT 'CAJERO','Cajero' WHERE NOT EXISTS (SELECT 1 FROM Roles WHERE Codigo='CAJERO');
INSERT INTO Roles (Codigo, Nombre) SELECT 'CONSULTA','Consulta' WHERE NOT EXISTS (SELECT 1 FROM Roles WHERE Codigo='CONSULTA');

INSERT INTO MetodosCalculo (Codigo,Nombre,Descripcion) SELECT 'FRANCES','Sistema francés','Cuota fija con interés sobre saldo.' WHERE NOT EXISTS (SELECT 1 FROM MetodosCalculo WHERE Codigo='FRANCES');
INSERT INTO MetodosCalculo (Codigo,Nombre,Descripcion) SELECT 'ALEMAN','Sistema alemán','Amortización de capital constante; cuotas decrecientes.' WHERE NOT EXISTS (SELECT 1 FROM MetodosCalculo WHERE Codigo='ALEMAN');
INSERT INTO MetodosCalculo (Codigo,Nombre,Descripcion) SELECT 'AMERICANO','Sistema americano','Intereses periódicos y capital al vencimiento.' WHERE NOT EXISTS (SELECT 1 FROM MetodosCalculo WHERE Codigo='AMERICANO');
INSERT INTO MetodosCalculo (Codigo,Nombre,Descripcion) SELECT 'SIMPLE','Interés simple','Interés calculado sobre capital, tasa y tiempo.' WHERE NOT EXISTS (SELECT 1 FROM MetodosCalculo WHERE Codigo='SIMPLE');
INSERT INTO MetodosCalculo (Codigo,Nombre,Descripcion) SELECT 'COMPUESTO','Interés compuesto','Capitalización según periodicidad contractual.' WHERE NOT EXISTS (SELECT 1 FROM MetodosCalculo WHERE Codigo='COMPUESTO');

INSERT INTO Periodicidades (Codigo,Nombre,Unidad,Intervalo,PeriodosPorAnio) SELECT 'MENSUAL','Mensual','MESES',1,12 WHERE NOT EXISTS (SELECT 1 FROM Periodicidades WHERE Codigo='MENSUAL');
INSERT INTO Periodicidades (Codigo,Nombre,Unidad,Intervalo,PeriodosPorAnio) SELECT 'BIMESTRAL','Bimestral','MESES',2,6 WHERE NOT EXISTS (SELECT 1 FROM Periodicidades WHERE Codigo='BIMESTRAL');
INSERT INTO Periodicidades (Codigo,Nombre,Unidad,Intervalo,PeriodosPorAnio) SELECT 'TRIMESTRAL','Trimestral','MESES',3,4 WHERE NOT EXISTS (SELECT 1 FROM Periodicidades WHERE Codigo='TRIMESTRAL');
INSERT INTO Periodicidades (Codigo,Nombre,Unidad,Intervalo,PeriodosPorAnio) SELECT 'SEMESTRAL','Semestral','MESES',6,2 WHERE NOT EXISTS (SELECT 1 FROM Periodicidades WHERE Codigo='SEMESTRAL');
INSERT INTO Periodicidades (Codigo,Nombre,Unidad,Intervalo,PeriodosPorAnio) SELECT 'ANUAL','Anual','MESES',12,1 WHERE NOT EXISTS (SELECT 1 FROM Periodicidades WHERE Codigo='ANUAL');

INSERT INTO Estados (Codigo,Nombre) SELECT 'SOLICITUD','Solicitud' WHERE NOT EXISTS (SELECT 1 FROM Estados WHERE Codigo='SOLICITUD');
INSERT INTO Estados (Codigo,Nombre) SELECT 'EN_EVALUACION','En evaluación' WHERE NOT EXISTS (SELECT 1 FROM Estados WHERE Codigo='EN_EVALUACION');
INSERT INTO Estados (Codigo,Nombre) SELECT 'APROBADO','Aprobado' WHERE NOT EXISTS (SELECT 1 FROM Estados WHERE Codigo='APROBADO');
INSERT INTO Estados (Codigo,Nombre) SELECT 'RECHAZADO','Rechazado' WHERE NOT EXISTS (SELECT 1 FROM Estados WHERE Codigo='RECHAZADO');
INSERT INTO Estados (Codigo,Nombre) SELECT 'DESEMBOLSADO','Desembolsado' WHERE NOT EXISTS (SELECT 1 FROM Estados WHERE Codigo='DESEMBOLSADO');
INSERT INTO Estados (Codigo,Nombre) SELECT 'VIGENTE','Vigente' WHERE NOT EXISTS (SELECT 1 FROM Estados WHERE Codigo='VIGENTE');
INSERT INTO Estados (Codigo,Nombre) SELECT 'EN_MORA','En mora' WHERE NOT EXISTS (SELECT 1 FROM Estados WHERE Codigo='EN_MORA');
INSERT INTO Estados (Codigo,Nombre) SELECT 'CANCELADO','Cancelado' WHERE NOT EXISTS (SELECT 1 FROM Estados WHERE Codigo='CANCELADO');
INSERT INTO Estados (Codigo,Nombre) SELECT 'REFINANCIADO','Refinanciado' WHERE NOT EXISTS (SELECT 1 FROM Estados WHERE Codigo='REFINANCIADO');

-- Comprobación posterior a la importación.
SELECT TABLE_NAME AS Tabla FROM information_schema.TABLES
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME;
