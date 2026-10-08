-- CAPITALWEST · Proyecto No. 2 DS8 + Diseño detallado de pantallas, octubre 2026.
-- Importar en phpMyAdmin, formato SQL, UTF-8. Probado en MariaDB 10.4.32 (XAMPP).
-- Esquema NUEVO: importar una sola vez en una base vacía. No es una migración.
-- No elimina ni modifica la base sistema_financiera anterior.
-- Fechas Utc en UTC; fechas operativas según America/Panama.
-- No incluye clientes, préstamos, usuarios ni contraseñas ficticias.
CREATE DATABASE IF NOT EXISTS capitalwest
  CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE capitalwest;
SET NAMES utf8mb4;
SET time_zone = '+00:00';


CREATE TABLE Provincias (
 Id INT AUTO_INCREMENT PRIMARY KEY, Codigo VARCHAR(10) NOT NULL UNIQUE,
 Nombre VARCHAR(80) NOT NULL UNIQUE, Activo BOOLEAN NOT NULL DEFAULT 1,
 CHECK (Activo IN (0,1))
) ENGINE=InnoDB;
CREATE TABLE Distritos (
 Id INT AUTO_INCREMENT PRIMARY KEY, ProvinciaId INT NOT NULL, Codigo VARCHAR(20) NOT NULL UNIQUE,
 Nombre VARCHAR(80) NOT NULL, Activo BOOLEAN NOT NULL DEFAULT 1,
 UNIQUE (Id,ProvinciaId), UNIQUE (ProvinciaId,Nombre),
 FOREIGN KEY (ProvinciaId) REFERENCES Provincias(Id), CHECK (Activo IN (0,1))
) ENGINE=InnoDB;
CREATE TABLE Corregimientos (
 Id INT AUTO_INCREMENT PRIMARY KEY, DistritoId INT NOT NULL, Codigo VARCHAR(30) NOT NULL UNIQUE,
 Nombre VARCHAR(80) NOT NULL, Activo BOOLEAN NOT NULL DEFAULT 1,
 UNIQUE (Id,DistritoId), UNIQUE (DistritoId,Nombre),
 FOREIGN KEY (DistritoId) REFERENCES Distritos(Id), CHECK (Activo IN (0,1))
) ENGINE=InnoDB;
CREATE TABLE EstadosCiviles (
 Codigo VARCHAR(15) PRIMARY KEY, Nombre VARCHAR(60) NOT NULL, Activo BOOLEAN NOT NULL DEFAULT 1,
 CHECK (Activo IN (0,1))
) ENGINE=InnoDB;
CREATE TABLE TiposTasa (
 Codigo VARCHAR(15) PRIMARY KEY, Nombre VARCHAR(80) NOT NULL,
 Activo BOOLEAN NOT NULL DEFAULT 1, CHECK (Activo IN (0,1))
) ENGINE=InnoDB;
CREATE TABLE FormasPago (
 Codigo VARCHAR(20) PRIMARY KEY, Nombre VARCHAR(80) NOT NULL,
 Activo BOOLEAN NOT NULL DEFAULT 1, CHECK (Activo IN (0,1))
) ENGINE=InnoDB;

CREATE TABLE Roles (
  EsSistema BOOLEAN NOT NULL DEFAULT 0,
  Activo BOOLEAN NOT NULL DEFAULT 1,
  CHECK (EsSistema IN (0,1) AND Activo IN (0,1)),
  Id INT AUTO_INCREMENT PRIMARY KEY,
  Codigo VARCHAR(30) NOT NULL UNIQUE,
  Nombre VARCHAR(80) NOT NULL
) ENGINE=InnoDB;

CREATE TABLE Usuarios (
  IntentosFallidos INT NOT NULL DEFAULT 0,
  BloqueadoHastaUtc DATETIME NULL,
  UltimoAccesoUtc DATETIME NULL,
  DebeCambiarClave BOOLEAN NOT NULL DEFAULT 0,
  VersionSesion BIGINT NOT NULL DEFAULT 1,
  ModificadoUtc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  CHECK (IntentosFallidos >= 0 AND VersionSesion > 0 AND DebeCambiarClave IN (0,1)),
  Id INT AUTO_INCREMENT PRIMARY KEY,
  NombreUsuario VARCHAR(60) NOT NULL UNIQUE,
  NombreCompleto VARCHAR(160) NOT NULL,
  Correo VARCHAR(254) NOT NULL UNIQUE,
  ContrasenaHash VARCHAR(255) NOT NULL COMMENT 'Hash con algoritmo y parámetros; nunca texto plano',
  Activo TINYINT(1) NOT NULL DEFAULT 1,
  CreadoUtc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT ck_usuario_activo CHECK (Activo IN (0,1))
) ENGINE=InnoDB;

CREATE TABLE UsuarioRoles (
  UsuarioId INT NOT NULL,
  RolId INT NOT NULL,
  PRIMARY KEY (UsuarioId, RolId),
  CONSTRAINT fk_ur_usuario FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id),
  CONSTRAINT fk_ur_rol FOREIGN KEY (RolId) REFERENCES Roles(Id)
) ENGINE=InnoDB;

CREATE TABLE Clientes (
  RucDv VARCHAR(2) NULL,
  Empleador VARCHAR(160) NULL,
  Cargo VARCHAR(120) NULL,
  AntiguedadMeses INT NULL,
  ProvinciaId INT NULL,
  DistritoId INT NULL,
  CorregimientoId INT NULL,
  CreadoPor INT NULL,
  ModificadoPor INT NULL,
  FOREIGN KEY (CreadoPor) REFERENCES Usuarios(Id),
  FOREIGN KEY (ModificadoPor) REFERENCES Usuarios(Id),
  FOREIGN KEY (EstadoCivil) REFERENCES EstadosCiviles(Codigo),
  FOREIGN KEY (ProvinciaId) REFERENCES Provincias(Id),
  FOREIGN KEY (DistritoId,ProvinciaId) REFERENCES Distritos(Id,ProvinciaId),
  FOREIGN KEY (CorregimientoId,DistritoId) REFERENCES Corregimientos(Id,DistritoId),
  CHECK (DistritoId IS NULL OR ProvinciaId IS NOT NULL),
  CHECK (CorregimientoId IS NULL OR DistritoId IS NOT NULL),
  CHECK (AntiguedadMeses IS NULL OR AntiguedadMeses >= 0),
  CHECK (Telefono REGEXP '^[0-9]{7,8}$'),
  CHECK (Sexo IN ('MASCULINO','FEMENINO','OTRO','NO_APLICA','M','F','N/A')),
  CHECK (TipoPersona <> 'NATURAL' OR FechaNacimiento IS NOT NULL),
  CHECK (TipoPersona <> 'NATURAL' OR TipoIdentificacion IN ('CEDULA','PASAPORTE')),
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

CREATE TABLE MetodosCalculo (
  ClaseImplementacion VARCHAR(200) NULL,
  Formula TEXT NULL,
  Variables TEXT NULL,
  Algoritmo TEXT NULL,
  EjemploManual TEXT NULL,
  CHECK (Activo IN (0,1)),
  Id INT AUTO_INCREMENT PRIMARY KEY,
  Codigo VARCHAR(30) NOT NULL UNIQUE,
  Nombre VARCHAR(100) NOT NULL,
  Descripcion VARCHAR(500) NOT NULL,
  Activo TINYINT(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB;

CREATE TABLE Periodicidades (
  Activo BOOLEAN NOT NULL DEFAULT 1,
  CHECK (Activo IN (0,1)),
  Id INT AUTO_INCREMENT PRIMARY KEY,
  Codigo VARCHAR(30) NOT NULL UNIQUE,
  Nombre VARCHAR(80) NOT NULL,
  Unidad VARCHAR(10) NOT NULL COMMENT 'DIAS o MESES; el calendario del contrato define fechas',
  Intervalo INT NOT NULL,
  PeriodosPorAnio DECIMAL(10,4) NOT NULL,
  CONSTRAINT ck_periodicidad_unidad CHECK (Unidad IN ('DIAS','MESES')),
  CONSTRAINT ck_periodicidad_intervalo CHECK (Intervalo > 0 AND PeriodosPorAnio > 0)
) ENGINE=InnoDB;

CREATE TABLE Estados (
  EsSistema BOOLEAN NOT NULL DEFAULT 1,
  Activo BOOLEAN NOT NULL DEFAULT 1,
  CHECK (EsSistema IN (0,1) AND Activo IN (0,1)),
  Codigo VARCHAR(30) PRIMARY KEY,
  Nombre VARCHAR(80) NOT NULL
) ENGINE=InnoDB;

CREATE TABLE ProductosFinancieros (
  PlazoMinimoMeses DECIMAL(8,2) NOT NULL DEFAULT 1,
  PlazoMaximoMeses DECIMAL(8,2) NOT NULL DEFAULT 120,
  TipoComision VARCHAR(10) NOT NULL DEFAULT 'FIJO',
  ValorComision DECIMAL(14,6) NOT NULL DEFAULT 0,
  TipoGastos VARCHAR(10) NOT NULL DEFAULT 'FIJO',
  ValorGastos DECIMAL(14,6) NOT NULL DEFAULT 0,
  CreadoUtc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  ModificadoUtc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  UNIQUE (Nombre),
  CHECK (PlazoMinimoMeses > 0 AND PlazoMaximoMeses >= PlazoMinimoMeses),
  CHECK (TipoComision IN ('FIJO','PORCENTAJE') AND TipoGastos IN ('FIJO','PORCENTAJE')),
  CHECK (ValorComision >= 0 AND ValorGastos >= 0 AND Activo IN (0,1)),
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

CREATE TABLE ProductoMetodos (
  ProductoId INT NOT NULL,
  MetodoId INT NOT NULL,
  PRIMARY KEY (ProductoId, MetodoId),
  CONSTRAINT fk_pm_producto FOREIGN KEY (ProductoId) REFERENCES ProductosFinancieros(Id),
  CONSTRAINT fk_pm_metodo FOREIGN KEY (MetodoId) REFERENCES MetodosCalculo(Id)
) ENGINE=InnoDB;

CREATE TABLE Tasas (
  Activo BOOLEAN NOT NULL DEFAULT 1,
  EsPredeterminada BOOLEAN NOT NULL DEFAULT 0,
  CreadoPor INT NULL,
  FOREIGN KEY (TipoTasa) REFERENCES TiposTasa(Codigo),
  FOREIGN KEY (CreadoPor) REFERENCES Usuarios(Id),
  CHECK (Activo IN (0,1) AND EsPredeterminada IN (0,1)),
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

CREATE TABLE Solicitudes (
  PlazoMeses DECIMAL(8,2) NOT NULL,
  TasaPactada DECIMAL(9,6) NOT NULL,
  TipoTasa VARCHAR(15) NOT NULL,
  CapitalizacionId INT NOT NULL,
  FechaPrimerPago DATE NOT NULL,
  Comision DECIMAL(14,2) NOT NULL DEFAULT 0,
  GastosAsociados DECIMAL(14,2) NOT NULL DEFAULT 0,
  MontoAprobado DECIMAL(14,2) NULL,
  FechaAprobacion DATETIME NULL,
  MotivoRechazo VARCHAR(1000) NULL,
  CuotaEstimada DECIMAL(14,2) NULL,
  IngresosEvaluados DECIMAL(14,2) NULL,
  Version BIGINT UNSIGNED NOT NULL DEFAULT 1,
  FOREIGN KEY (TipoTasa) REFERENCES TiposTasa(Codigo),
  FOREIGN KEY (CapitalizacionId) REFERENCES Periodicidades(Id),
  CHECK (PlazoMeses > 0 AND TasaPactada >= 0 AND Comision >= 0 AND GastosAsociados >= 0),
  CHECK (MontoAprobado IS NULL OR (MontoAprobado > 0 AND MontoAprobado <= MontoSolicitado)),
  CHECK (EstadoCodigo <> 'APROBADO' OR (MontoAprobado IS NOT NULL AND FechaAprobacion IS NOT NULL AND EvaluadoPor IS NOT NULL)),
  CHECK (EstadoCodigo <> 'RECHAZADO' OR (MotivoRechazo IS NOT NULL AND CHAR_LENGTH(TRIM(MotivoRechazo)) > 0 AND EvaluadoPor IS NOT NULL)),
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

CREATE TABLE Prestamos (
  PlazoMeses DECIMAL(8,2) NOT NULL,
  FechaCancelacion DATETIME NULL,
  TipoCancelacion VARCHAR(20) NULL,
  DesembolsadoPor INT NULL,
  CreadoUtc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  ModificadoUtc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  FOREIGN KEY (TipoTasa) REFERENCES TiposTasa(Codigo),
  FOREIGN KEY (DesembolsadoPor) REFERENCES Usuarios(Id),
  CHECK (PlazoMeses > 0 AND Comision + GastosAsociados <= MontoAprobado),
  CHECK (TipoCancelacion IS NULL OR TipoCancelacion IN ('NORMAL','ANTICIPADA')),
  CHECK (EstadoCodigo <> 'CANCELADO' OR (SaldoCapital = 0 AND FechaCancelacion IS NOT NULL)),
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

CREATE TABLE Cuotas (
  VersionPlan INT NOT NULL DEFAULT 1,
  CHECK (VersionPlan > 0),
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

CREATE TABLE Pagos (
  TipoPago VARCHAR(20) NOT NULL DEFAULT 'CUOTA_NORMAL',
  FormaPagoCodigo VARCHAR(20) NOT NULL,
  ReferenciaExterna VARCHAR(100) NULL,
  FechaAnulacionUtc DATETIME NULL,
  ClaveIdempotencia VARCHAR(64) NOT NULL UNIQUE,
  CreadoUtc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (FormaPagoCodigo) REFERENCES FormasPago(Codigo),
  CHECK (TipoPago IN ('CUOTA_NORMAL','ABONO_CAPITAL','CANCELACION_TOTAL')),
  CHECK (Estado <> 'ANULADO' OR FechaAnulacionUtc IS NOT NULL),
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

CREATE TABLE AplicacionesPago (
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

CREATE TABLE Refinanciamientos (
  CapitalTransferido DECIMAL(14,2) NOT NULL DEFAULT 0,
  InteresTransferido DECIMAL(14,2) NOT NULL DEFAULT 0,
  MoraTransferida DECIMAL(14,2) NOT NULL DEFAULT 0,
  CargosTransferidos DECIMAL(14,2) NOT NULL DEFAULT 0,
  UNIQUE (PrestamoOrigenId),
  CHECK (CapitalTransferido >= 0 AND InteresTransferido >= 0 AND MoraTransferida >= 0 AND CargosTransferidos >= 0),
  CHECK (SaldoTransferido = CapitalTransferido + InteresTransferido + MoraTransferida + CargosTransferidos),
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

CREATE TABLE Auditoria (
  RolCodigo VARCHAR(30) NULL,
  Modulo VARCHAR(60) NOT NULL,
  Descripcion VARCHAR(1000) NULL,
  ValorAnterior LONGTEXT NULL,
  ValorNuevo LONGTEXT NULL,
  DireccionIp VARCHAR(45) NULL,
  INDEX ix_auditoria_usuario_fecha (UsuarioId,FechaUtc),
  Id BIGINT AUTO_INCREMENT PRIMARY KEY,
  UsuarioId INT NULL,
  FechaUtc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  Accion VARCHAR(40) NOT NULL,
  Entidad VARCHAR(60) NOT NULL,
  RegistroId BIGINT NOT NULL,
  Detalle LONGTEXT NULL COMMENT 'Descripción o JSON sin contraseñas ni secretos',
  INDEX ix_auditoria_registro (Entidad, RegistroId),
  INDEX ix_auditoria_fecha (FechaUtc),
  CONSTRAINT fk_auditoria_usuario FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id)
) ENGINE=InnoDB;



CREATE TABLE Permisos (
 Id INT AUTO_INCREMENT PRIMARY KEY, Codigo VARCHAR(80) NOT NULL UNIQUE,
 Modulo VARCHAR(40) NOT NULL, Accion VARCHAR(30) NOT NULL, Descripcion VARCHAR(200) NOT NULL,
 UNIQUE (Modulo,Accion)
) ENGINE=InnoDB;
CREATE TABLE RolPermisos (
 RolId INT NOT NULL, PermisoId INT NOT NULL, PRIMARY KEY (RolId,PermisoId),
 FOREIGN KEY (RolId) REFERENCES Roles(Id), FOREIGN KEY (PermisoId) REFERENCES Permisos(Id)
) ENGINE=InnoDB;
CREATE TABLE RecuperacionesClave (
 Id BIGINT AUTO_INCREMENT PRIMARY KEY, UsuarioId INT NOT NULL,
 TokenHash CHAR(64) CHARACTER SET ascii COLLATE ascii_bin NOT NULL UNIQUE,
 CreadoUtc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP, VenceUtc DATETIME NOT NULL,
 UsadoUtc DATETIME NULL, FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id),
 CHECK (VenceUtc > CreadoUtc), INDEX (UsuarioId,VenceUtc)
) ENGINE=InnoDB;
CREATE TABLE SesionesUsuario (
 Id BIGINT AUTO_INCREMENT PRIMARY KEY, UsuarioId INT NOT NULL,
 TokenHash CHAR(64) CHARACTER SET ascii COLLATE ascii_bin NOT NULL UNIQUE,
 VersionSesion BIGINT NOT NULL, CreadoUtc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
 UltimaActividadUtc DATETIME NOT NULL, VenceUtc DATETIME NOT NULL, RevocadaUtc DATETIME NULL,
 FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id), CHECK (VersionSesion > 0),
 INDEX (UsuarioId,VenceUtc)
) ENGINE=InnoDB;
CREATE TABLE ReferenciasCliente (
 Id INT AUTO_INCREMENT PRIMARY KEY, ClienteId INT NOT NULL,
 Nombre VARCHAR(160) NOT NULL, Parentesco VARCHAR(80) NOT NULL, Telefono VARCHAR(8) NOT NULL,
 FOREIGN KEY (ClienteId) REFERENCES Clientes(Id), CHECK (Telefono REGEXP '^[0-9]{7,8}$')
) ENGINE=InnoDB;
CREATE TABLE ProductoPeriodicidades (
 ProductoId INT NOT NULL, PeriodicidadId INT NOT NULL, PRIMARY KEY (ProductoId,PeriodicidadId),
 FOREIGN KEY (ProductoId) REFERENCES ProductosFinancieros(Id),
 FOREIGN KEY (PeriodicidadId) REFERENCES Periodicidades(Id)
) ENGINE=InnoDB;
ALTER TABLE Solicitudes ADD CONSTRAINT fk_sol_periodicidad_producto
 FOREIGN KEY (ProductoId,PeriodicidadId) REFERENCES ProductoPeriodicidades(ProductoId,PeriodicidadId);
ALTER TABLE Prestamos ADD CONSTRAINT fk_pre_periodicidad_producto
 FOREIGN KEY (ProductoId,PeriodicidadId) REFERENCES ProductoPeriodicidades(ProductoId,PeriodicidadId);
CREATE TABLE HistorialSolicitudes (
 Id BIGINT AUTO_INCREMENT PRIMARY KEY, SolicitudId INT NOT NULL,
 EstadoAnterior VARCHAR(30) NULL, EstadoNuevo VARCHAR(30) NOT NULL,
 FechaUtc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP, UsuarioId INT NOT NULL,
 Motivo VARCHAR(1000) NULL,
 FOREIGN KEY (SolicitudId) REFERENCES Solicitudes(Id),
 FOREIGN KEY (EstadoAnterior) REFERENCES Estados(Codigo),
 FOREIGN KEY (EstadoNuevo) REFERENCES Estados(Codigo), FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id)
) ENGINE=InnoDB;
CREATE TABLE HistorialPrestamos (
 Id BIGINT AUTO_INCREMENT PRIMARY KEY, PrestamoId INT NOT NULL,
 EstadoAnterior VARCHAR(30) NULL, EstadoNuevo VARCHAR(30) NOT NULL,
 FechaUtc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP, UsuarioId INT NULL,
 Motivo VARCHAR(1000) NULL,
 FOREIGN KEY (PrestamoId) REFERENCES Prestamos(Id),
 FOREIGN KEY (EstadoAnterior) REFERENCES Estados(Codigo),
 FOREIGN KEY (EstadoNuevo) REFERENCES Estados(Codigo), FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id)
) ENGINE=InnoDB;
CREATE TABLE VersionesAmortizacion (
 Id BIGINT AUTO_INCREMENT PRIMARY KEY, PrestamoId INT NOT NULL, VersionPlan INT NOT NULL,
 FechaUtc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP, UsuarioId INT NOT NULL,
 Motivo VARCHAR(500) NOT NULL, UNIQUE (PrestamoId,VersionPlan),
 FOREIGN KEY (PrestamoId) REFERENCES Prestamos(Id), FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id)
) ENGINE=InnoDB;
CREATE TABLE DetallesAmortizacionHistorica (
 VersionId BIGINT NOT NULL, NumeroCuota INT NOT NULL, FechaVencimiento DATE NOT NULL,
 Capital DECIMAL(14,2) NOT NULL, Interes DECIMAL(14,2) NOT NULL,
 InteresCapitalizado DECIMAL(14,2) NOT NULL DEFAULT 0, SaldoProyectado DECIMAL(14,2) NOT NULL,
 PRIMARY KEY (VersionId,NumeroCuota), FOREIGN KEY (VersionId) REFERENCES VersionesAmortizacion(Id),
 CHECK (NumeroCuota > 0 AND Capital >= 0 AND Interes >= 0 AND InteresCapitalizado >= 0 AND SaldoProyectado >= 0)
) ENGINE=InnoDB;
CREATE TABLE MovimientosPrestamo (
 Id BIGINT AUTO_INCREMENT PRIMARY KEY, PrestamoId INT NOT NULL, PagoId INT NULL,
 Fecha DATETIME NOT NULL, Tipo VARCHAR(30) NOT NULL, Concepto VARCHAR(200) NOT NULL,
 Debito DECIMAL(14,2) NOT NULL DEFAULT 0, Credito DECIMAL(14,2) NOT NULL DEFAULT 0,
 UsuarioId INT NULL, ReversaDeId BIGINT NULL UNIQUE,
 FOREIGN KEY (PrestamoId) REFERENCES Prestamos(Id),
 FOREIGN KEY (PagoId,PrestamoId) REFERENCES Pagos(Id,PrestamoId),
 FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id), FOREIGN KEY (ReversaDeId) REFERENCES MovimientosPrestamo(Id),
 CHECK (Tipo IN ('DESEMBOLSO','INTERES','CAPITALIZACION','MORA','CARGO','PAGO','REVERSA','REFINANCIAMIENTO','AJUSTE')),
 CHECK ((Debito > 0 AND Credito = 0) OR (Credito > 0 AND Debito = 0)),
 INDEX (PrestamoId,Fecha,Id)
) ENGINE=InnoDB;
CREATE TABLE ParametrosSistema (
 Codigo VARCHAR(80) PRIMARY KEY, Valor VARCHAR(500) NOT NULL, TipoDato VARCHAR(20) NOT NULL,
 Descripcion VARCHAR(500) NOT NULL, EsPropuesta BOOLEAN NOT NULL DEFAULT 0,
 ModificadoPor INT NULL, ModificadoUtc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
 FOREIGN KEY (ModificadoPor) REFERENCES Usuarios(Id),
 CHECK (TipoDato IN ('ENTERO','DECIMAL','BOOLEANO','TEXTO'))
) ENGINE=InnoDB;
CREATE TABLE PreferenciasUsuario (
 UsuarioId INT NOT NULL, Clave VARCHAR(80) NOT NULL, Valor VARCHAR(500) NOT NULL,
 PRIMARY KEY (UsuarioId,Clave), FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id)
) ENGINE=InnoDB;
CREATE TABLE Notificaciones (
 Id BIGINT AUTO_INCREMENT PRIMARY KEY, UsuarioId INT NOT NULL, Tipo VARCHAR(40) NOT NULL,
 Titulo VARCHAR(160) NOT NULL, Mensaje VARCHAR(1000) NOT NULL,
 Entidad VARCHAR(60) NULL, RegistroId BIGINT NULL,
 CreadoUtc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP, LeidoUtc DATETIME NULL,
 FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id), INDEX (UsuarioId,LeidoUtc,CreadoUtc)
) ENGINE=InnoDB;
CREATE TABLE Reportes (
 Id INT AUTO_INCREMENT PRIMARY KEY, Codigo VARCHAR(40) NOT NULL UNIQUE,
 Nombre VARCHAR(160) NOT NULL, Descripcion VARCHAR(500) NOT NULL,
 VistaSql VARCHAR(100) NOT NULL, Activo BOOLEAN NOT NULL DEFAULT 1, CHECK (Activo IN (0,1))
) ENGINE=InnoDB;
CREATE TABLE RolReportes (
 RolId INT NOT NULL, ReporteId INT NOT NULL, PRIMARY KEY (RolId,ReporteId),
 FOREIGN KEY (RolId) REFERENCES Roles(Id), FOREIGN KEY (ReporteId) REFERENCES Reportes(Id)
) ENGINE=InnoDB;
CREATE TABLE EjecucionesReporte (
 Id BIGINT AUTO_INCREMENT PRIMARY KEY, ReporteId INT NOT NULL, UsuarioId INT NOT NULL,
 FechaUtc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP, FiltrosJson LONGTEXT NULL,
 Formato VARCHAR(10) NOT NULL, FOREIGN KEY (ReporteId) REFERENCES Reportes(Id),
 FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id), CHECK (Formato IN ('PANTALLA','PDF','CSV','XLSX','IMPRESION'))
) ENGINE=InnoDB;

INSERT INTO Roles (Codigo,Nombre,EsSistema) VALUES
('ADMINISTRADOR','Administrador',1),
('OFICIAL_CREDITO','Oficial de crédito',1),
('CAJERO','Cajero',1),
('CONSULTA','Consulta',1);
INSERT INTO EstadosCiviles (Codigo,Nombre) VALUES
('SOLTERO','Soltero'),
('CASADO','Casado'),
('DIVORCIADO','Divorciado'),
('VIUDO','Viudo'),
('UNIDO','Unido'),
('NO_APLICA','No aplica');
INSERT INTO TiposTasa (Codigo,Nombre) VALUES
('NOMINAL_ANUAL','Nominal anual'),
('EFECTIVA_ANUAL','Efectiva anual');
INSERT INTO FormasPago (Codigo,Nombre) VALUES
('EFECTIVO','Efectivo'),
('TRANSFERENCIA','Transferencia'),
('CHEQUE','Cheque'),
('TARJETA','Tarjeta');
INSERT INTO MetodosCalculo (Codigo,Nombre,Descripcion,ClaseImplementacion,Formula) VALUES
('FRANCES','Sistema francés','Cuota fija con interés sobre saldo.','MetodoFrances','C = P*i/(1-(1+i)^(-n)); para i=0, C=P/n'),
('ALEMAN','Sistema alemán','Amortización constante; cuota decreciente.','MetodoAleman','A=P/n; I_k=saldo_anterior*i; C_k=A+I_k'),
('AMERICANO','Sistema americano','Interés periódico; capital al vencimiento.','MetodoAmericano','I_k=P*i; capital de la última cuota=P'),
('SIMPLE','Interés simple','Interés sobre capital original y tiempo.','InteresSimple','I=P*r*t'),
('COMPUESTO','Interés compuesto','Intereses capitalizables según el contrato.','InteresCompuesto','M=P*(1+i)^n');
INSERT INTO Periodicidades (Codigo,Nombre,Unidad,Intervalo,PeriodosPorAnio) VALUES
('MENSUAL','Mensual','MESES',1,12),
('QUINCENAL','Cada 15 días','DIAS',15,24),
('TRIMESTRAL','Trimestral','MESES',3,4),
('BIMESTRAL','Bimestral','MESES',2,6),
('SEMESTRAL','Semestral','MESES',6,2),
('ANUAL','Anual','MESES',12,1);
INSERT INTO Estados (Codigo,Nombre) VALUES
('SOLICITUD','Solicitud'),
('EN_EVALUACION','En evaluación'),
('APROBADO','Aprobado'),
('RECHAZADO','Rechazado'),
('DESEMBOLSADO','Desembolsado'),
('VIGENTE','Vigente'),
('EN_MORA','En mora'),
('CANCELADO','Cancelado'),
('REFINANCIADO','Refinanciado');
INSERT INTO ParametrosSistema (Codigo,Valor,TipoDato,Descripcion,EsPropuesta) VALUES
('MONEDA','PAB','TEXTO','Importes en balboas.',0),
('ZONA_HORARIA','America/Panama','TEXTO','Zona de presentación; registros Utc usan UTC.',0),
('INTENTOS_MAXIMOS','5','ENTERO','Propuesta del diseño de pantallas.',1),
('BLOQUEO_MINUTOS','15','ENTERO','Propuesta del diseño de pantallas.',1),
('INACTIVIDAD_MINUTOS','20','ENTERO','Propuesta del diseño de pantallas.',1),
('RECUPERACION_MINUTOS','30','ENTERO','Propuesta del diseño de pantallas.',1),
('CLAVE_LONGITUD_MINIMA','8','ENTERO','Propuesta del diseño de pantallas.',1),
('CLAVE_MAYUSCULA','1','BOOLEANO','Regla propuesta.',1),
('CLAVE_NUMERO','1','BOOLEANO','Regla propuesta.',1),
('PORCENTAJE_MAXIMO_CUOTA_INGRESOS','35','DECIMAL','Valor inicial de implementación; confirmar criterio de evaluación.',1),
('TASA_MORA_ANUAL','0','DECIMAL','Configurar antes de activar el cálculo de mora; el enunciado no fija tasa.',1),
('DIAS_GRACIA_MORA','0','ENTERO','Configurar la política de mora.',1),
('PERMITIR_SOBREPAGO','0','BOOLEANO','Sin regla explícita, bloquear pagos por encima de la deuda exigible.',0),
('BASE_DIAS_ANIO','360','ENTERO','Convención propuesta; confirmar contrato. No usar automáticamente para toda periodicidad.',1);
INSERT INTO Permisos (Codigo,Modulo,Accion,Descripcion) VALUES
('CLIENTES.VER','CLIENTES','VER','Clientes / ver'),
('CLIENTES.CREAR','CLIENTES','CREAR','Clientes / crear'),
('CLIENTES.EDITAR','CLIENTES','EDITAR','Clientes / editar'),
('CLIENTES.DESACTIVAR','CLIENTES','DESACTIVAR','Clientes / desactivar'),
('PRODUCTOS.VER','PRODUCTOS','VER','Productos / ver'),
('PRODUCTOS.CREAR','PRODUCTOS','CREAR','Productos / crear'),
('PRODUCTOS.EDITAR','PRODUCTOS','EDITAR','Productos / editar'),
('PRODUCTOS.DESACTIVAR','PRODUCTOS','DESACTIVAR','Productos / desactivar'),
('SOLICITUDES.VER','SOLICITUDES','VER','Solicitudes / ver'),
('SOLICITUDES.CREAR','SOLICITUDES','CREAR','Solicitudes / crear'),
('SOLICITUDES.EVALUAR','SOLICITUDES','EVALUAR','Solicitudes / evaluar'),
('SOLICITUDES.APROBAR','SOLICITUDES','APROBAR','Solicitudes / aprobar'),
('SOLICITUDES.RECHAZAR','SOLICITUDES','RECHAZAR','Solicitudes / rechazar'),
('PRESTAMOS.VER','PRESTAMOS','VER','Prestamos / ver'),
('PRESTAMOS.FORMALIZAR','PRESTAMOS','FORMALIZAR','Prestamos / formalizar'),
('PRESTAMOS.CANCELAR','PRESTAMOS','CANCELAR','Prestamos / cancelar'),
('PRESTAMOS.REFINANCIAR','PRESTAMOS','REFINANCIAR','Prestamos / refinanciar'),
('PAGOS.VER','PAGOS','VER','Pagos / ver'),
('PAGOS.REGISTRAR','PAGOS','REGISTRAR','Pagos / registrar'),
('PAGOS.ANULAR','PAGOS','ANULAR','Pagos / anular'),
('ESTADOS_CUENTA.VER','ESTADOS_CUENTA','VER','Estados_Cuenta / ver'),
('SIMULADOR.USAR','SIMULADOR','USAR','Simulador / usar'),
('REPORTES.VER','REPORTES','VER','Reportes / ver'),
('REPORTES.GENERAR','REPORTES','GENERAR','Reportes / generar'),
('ADMINISTRACION.VER','ADMINISTRACION','VER','Administracion / ver'),
('ADMINISTRACION.CONFIGURAR','ADMINISTRACION','CONFIGURAR','Administracion / configurar'),
('USUARIOS.VER','USUARIOS','VER','Usuarios / ver'),
('USUARIOS.CREAR','USUARIOS','CREAR','Usuarios / crear'),
('USUARIOS.EDITAR','USUARIOS','EDITAR','Usuarios / editar'),
('USUARIOS.DESACTIVAR','USUARIOS','DESACTIVAR','Usuarios / desactivar'),
('AUDITORIA.VER','AUDITORIA','VER','Auditoria / ver');
INSERT INTO RolPermisos SELECT r.Id,p.Id FROM Roles r CROSS JOIN Permisos p WHERE r.Codigo='ADMINISTRADOR';
INSERT INTO RolPermisos SELECT r.Id,p.Id FROM Roles r CROSS JOIN Permisos p WHERE r.Codigo='OFICIAL_CREDITO' AND p.Codigo IN ('CLIENTES.VER','CLIENTES.CREAR','CLIENTES.EDITAR','PRODUCTOS.VER','SOLICITUDES.VER','SOLICITUDES.CREAR','PRESTAMOS.VER','PAGOS.VER','ESTADOS_CUENTA.VER','SIMULADOR.USAR','REPORTES.VER','REPORTES.GENERAR');
INSERT INTO RolPermisos SELECT r.Id,p.Id FROM Roles r CROSS JOIN Permisos p WHERE r.Codigo='CAJERO' AND p.Codigo IN ('CLIENTES.VER','PRESTAMOS.VER','PAGOS.VER','PAGOS.REGISTRAR','PRESTAMOS.CANCELAR','ESTADOS_CUENTA.VER','REPORTES.VER','REPORTES.GENERAR');
INSERT INTO RolPermisos SELECT r.Id,p.Id FROM Roles r CROSS JOIN Permisos p WHERE r.Codigo='CONSULTA' AND p.Codigo IN ('CLIENTES.VER','PRODUCTOS.VER','SOLICITUDES.VER','PRESTAMOS.VER','PAGOS.VER','ESTADOS_CUENTA.VER','REPORTES.VER','REPORTES.GENERAR');
INSERT INTO Reportes (Codigo,Nombre,Descripcion,VistaSql) VALUES
('R01','Listado de clientes','Reporte mínimo 1 del Proyecto No. 2.','v_Reporte01Clientes'),
('R02','Préstamos vigentes','Reporte mínimo 2 del Proyecto No. 2.','v_Reporte02PrestamosVigentes'),
('R03','Préstamos cancelados','Reporte mínimo 3 del Proyecto No. 2.','v_Reporte03PrestamosCancelados'),
('R04','Préstamos en mora','Reporte mínimo 4 del Proyecto No. 2.','v_Reporte04PrestamosMora'),
('R05','Pagos por período','Reporte mínimo 5 del Proyecto No. 2.','v_Reporte05PagosPeriodo'),
('R06','Saldos pendientes','Reporte mínimo 6 del Proyecto No. 2.','v_Reporte06SaldosPendientes'),
('R07','Estado de cuenta','Reporte mínimo 7 del Proyecto No. 2.','v_Reporte07EstadoCuenta'),
('R08','Tabla de amortización','Reporte mínimo 8 del Proyecto No. 2.','v_Reporte08Amortizacion'),
('R09','Resumen de cartera','Reporte mínimo 9 del Proyecto No. 2.','v_Reporte09ResumenCartera'),
('R10','Comparativo de métodos','Reporte mínimo 10 del Proyecto No. 2.','v_Reporte10ComparativoMetodos');
INSERT INTO RolReportes SELECT r.Id,p.Id FROM Roles r CROSS JOIN Reportes p WHERE r.Codigo='ADMINISTRADOR';
INSERT INTO RolReportes SELECT r.Id,p.Id FROM Roles r CROSS JOIN Reportes p WHERE r.Codigo='CAJERO' AND p.Codigo IN ('R05','R07','R08');
INSERT INTO RolReportes SELECT r.Id,p.Id FROM Roles r CROSS JOIN Reportes p WHERE r.Codigo='OFICIAL_CREDITO' AND p.Codigo IN ('R01','R02','R04','R06','R07','R08');


CREATE VIEW v_AplicacionesActivas AS
 SELECT a.* FROM AplicacionesPago a JOIN Pagos p ON p.Id=a.PagoId WHERE p.Estado='APLICADO';
CREATE VIEW v_SaldosCuota AS
 SELECT c.*, COALESCE(a.Capital,0) AS CapitalPagado, COALESCE(a.Interes,0) AS InteresPagado,
 COALESCE(a.Mora,0) AS MoraPagada, COALESCE(a.OtrosCargos,0) AS CargosPagados,
 c.Capital-COALESCE(a.Capital,0) AS CapitalPendiente,
 c.Interes-COALESCE(a.Interes,0) AS InteresPendiente,
 c.Mora-COALESCE(a.Mora,0) AS MoraPendiente,
 c.OtrosCargos-COALESCE(a.OtrosCargos,0) AS CargosPendientes,
 c.Capital+c.Interes+c.Mora+c.OtrosCargos-COALESCE(a.Total,0) AS TotalPendiente,
 CASE WHEN c.Capital+c.Interes+c.Mora+c.OtrosCargos-COALESCE(a.Total,0)<=0 THEN 'PAGADA'
 WHEN c.FechaVencimiento < DATE(UTC_TIMESTAMP() - INTERVAL 5 HOUR) THEN 'VENCIDA'
 WHEN COALESCE(a.Total,0)>0 THEN 'PARCIAL' ELSE 'PENDIENTE' END AS EstadoCuota
 FROM Cuotas c LEFT JOIN (
 SELECT CuotaId,SUM(Capital) Capital,SUM(Interes) Interes,SUM(Mora) Mora,
 SUM(OtrosCargos) OtrosCargos,SUM(Capital+Interes+Mora+OtrosCargos) Total
 FROM v_AplicacionesActivas GROUP BY CuotaId) a ON a.CuotaId=c.Id;
CREATE VIEW v_PagosDesglosados AS
 SELECT p.*,COALESCE(a.Capital,0) Capital,COALESCE(a.Interes,0) Interes,
 COALESCE(a.Mora,0) Mora,COALESCE(a.OtrosCargos,0) OtrosCargos,
 COALESCE(a.Total,0) TotalAplicado,p.MontoPagado-COALESCE(a.Total,0) DiferenciaAplicacion
 FROM Pagos p LEFT JOIN (SELECT PagoId,SUM(Capital) Capital,SUM(Interes) Interes,
 SUM(Mora) Mora,SUM(OtrosCargos) OtrosCargos,SUM(Capital+Interes+Mora+OtrosCargos) Total
 FROM AplicacionesPago GROUP BY PagoId) a ON a.PagoId=p.Id;
CREATE VIEW v_Clientes AS
 SELECT c.*,TRIM(CONCAT(c.Nombres,' ',c.Apellidos)) NombreCompleto FROM Clientes c;
CREATE VIEW v_PrestamosResumen AS
 SELECT p.*,c.NombreCompleto,c.Identificacion,c.Telefono,pr.Nombre Producto,m.Nombre Metodo,
 s.FechaSolicitud,s.MontoSolicitado,
 COALESCE(q.InteresPendiente,0) InteresesProgramadosPendientes,
 COALESCE(q.MoraPendiente,0) MoraPendiente,COALESCE(q.CargosPendientes,0) CargosPendientes,
 COALESCE(q.CuotasVencidas,0) CuotasVencidas,q.ProximoVencimiento
 FROM Prestamos p JOIN v_Clientes c ON c.Id=p.ClienteId
 JOIN ProductosFinancieros pr ON pr.Id=p.ProductoId JOIN MetodosCalculo m ON m.Id=p.MetodoId
 JOIN Solicitudes s ON s.Id=p.SolicitudId
 LEFT JOIN (SELECT PrestamoId,SUM(InteresPendiente) InteresPendiente,SUM(MoraPendiente) MoraPendiente,
 SUM(CargosPendientes) CargosPendientes,SUM(EstadoCuota='VENCIDA') CuotasVencidas,
 MIN(CASE WHEN TotalPendiente>0 THEN FechaVencimiento END) ProximoVencimiento
 FROM v_SaldosCuota GROUP BY PrestamoId) q ON q.PrestamoId=p.Id;
CREATE VIEW v_Reporte01Clientes AS
 SELECT Id,Identificacion,NombreCompleto,Telefono,Provincia,TipoPersona,Activo FROM v_Clientes;
CREATE VIEW v_Reporte02PrestamosVigentes AS
 SELECT * FROM v_PrestamosResumen WHERE EstadoCodigo IN ('VIGENTE','DESEMBOLSADO');
CREATE VIEW v_Reporte03PrestamosCancelados AS
 SELECT * FROM v_PrestamosResumen WHERE EstadoCodigo='CANCELADO';
CREATE VIEW v_Reporte04PrestamosMora AS
 SELECT p.Id PrestamoId,p.NumeroPrestamo,p.ClienteId,c.NombreCompleto,c.Telefono,
 q.Id CuotaId,q.NumeroCuota,q.FechaVencimiento,
 DATEDIFF(DATE(UTC_TIMESTAMP() - INTERVAL 5 HOUR),q.FechaVencimiento) DiasAtraso,
 q.TotalPendiente MontoVencido,q.MoraPendiente
 FROM v_SaldosCuota q JOIN Prestamos p ON p.Id=q.PrestamoId JOIN v_Clientes c ON c.Id=p.ClienteId
 WHERE q.EstadoCuota='VENCIDA' AND p.EstadoCodigo IN ('VIGENTE','EN_MORA','DESEMBOLSADO');
CREATE VIEW v_Reporte05PagosPeriodo AS
 SELECT p.*,pr.NumeroPrestamo,c.NombreCompleto,u.NombreCompleto NombreUsuario
 FROM v_PagosDesglosados p JOIN Prestamos pr ON pr.Id=p.PrestamoId
 JOIN v_Clientes c ON c.Id=p.ClienteId JOIN Usuarios u ON u.Id=p.RegistradoPor WHERE p.Estado='APLICADO';
CREATE VIEW v_Reporte06SaldosPendientes AS
 SELECT * FROM v_PrestamosResumen WHERE EstadoCodigo IN ('VIGENTE','EN_MORA','DESEMBOLSADO');
CREATE VIEW v_Reporte07EstadoCuenta AS
 SELECT m.*,p.ClienteId,p.NumeroPrestamo,c.NombreCompleto,
 SUM(m.Debito-m.Credito) OVER (PARTITION BY m.PrestamoId ORDER BY m.Fecha,m.Id ROWS UNBOUNDED PRECEDING) SaldoAcumulado
 FROM MovimientosPrestamo m JOIN Prestamos p ON p.Id=m.PrestamoId JOIN v_Clientes c ON c.Id=p.ClienteId;
CREATE VIEW v_Reporte08Amortizacion AS
 SELECT p.NumeroPrestamo,p.ClienteId,q.*,q.Capital+q.Interes CuotaProgramada
 FROM v_SaldosCuota q JOIN Prestamos p ON p.Id=q.PrestamoId;
CREATE VIEW v_Reporte09ResumenCartera AS
 SELECT EstadoCodigo,ProductoId,MetodoId,DATE(FechaDesembolso) FechaDesembolso,
 COUNT(*) Cantidad,SUM(MontoAprobado) MontoAprobado,SUM(SaldoCapital) SaldoCapital
 FROM Prestamos GROUP BY EstadoCodigo,ProductoId,MetodoId,DATE(FechaDesembolso);
CREATE VIEW v_Reporte10ComparativoMetodos AS
 SELECT p.MetodoId,m.Nombre Metodo,DATE(p.FechaDesembolso) FechaDesembolso,
 COUNT(*) Cantidad,SUM(p.MontoAprobado) Monto,SUM(p.SaldoCapital) SaldoCapital,
 SUM(COALESCE(q.Interes,0)) InteresProgramado,SUM(COALESCE(q.Mora,0)) MoraGenerada
 FROM Prestamos p JOIN MetodosCalculo m ON m.Id=p.MetodoId
 LEFT JOIN (SELECT PrestamoId,SUM(Interes+InteresCapitalizado) Interes,SUM(Mora) Mora FROM Cuotas GROUP BY PrestamoId) q ON q.PrestamoId=p.Id
 GROUP BY p.MetodoId,m.Nombre,DATE(p.FechaDesembolso);


-- La auditoría es inmutable. El usuario SQL de la aplicación tampoco debe tener permisos DDL.
DELIMITER $$
CREATE TRIGGER tr_auditoria_no_update BEFORE UPDATE ON Auditoria FOR EACH ROW
BEGIN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='La auditoria no se puede modificar'; END$$
CREATE TRIGGER tr_auditoria_no_delete BEFORE DELETE ON Auditoria FOR EACH ROW
BEGIN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='La auditoria no se puede eliminar'; END$$
DELIMITER ;
