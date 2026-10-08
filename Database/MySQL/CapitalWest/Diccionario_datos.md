# Diccionario de datos de CapitalWest

Generado desde el esquema importado y validado en MariaDB. PK: clave primaria; UNI: única; MUL: índice o relación. Importes DECIMAL en balboas. Fechas Utc en UTC.

## aplicacionespago

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| PagoId | int(11) | NO | — | PRI |
| CuotaId | int(11) | NO | — | PRI |
| PrestamoId | int(11) | NO | — | — |
| Capital | decimal(14,2) | NO | 0.00 | — |
| Interes | decimal(14,2) | NO | 0.00 | — |
| Mora | decimal(14,2) | NO | 0.00 | — |
| OtrosCargos | decimal(14,2) | NO | 0.00 | — |
## auditoria

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| RolCodigo | varchar(30) | YES | NULL | — |
| Modulo | varchar(60) | NO | — | — |
| Descripcion | varchar(1000) | YES | NULL | — |
| ValorAnterior | longtext | YES | NULL | — |
| ValorNuevo | longtext | YES | NULL | — |
| DireccionIp | varchar(45) | YES | NULL | — |
| Id | bigint(20) | NO | — | PRI |
| UsuarioId | int(11) | YES | NULL | MUL |
| FechaUtc | datetime | NO | current_timestamp() | MUL |
| Accion | varchar(40) | NO | — | — |
| Entidad | varchar(60) | NO | — | MUL |
| RegistroId | bigint(20) | NO | — | — |
| Detalle | longtext | YES | NULL | — |
## clientes

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| RucDv | varchar(2) | YES | NULL | — |
| Empleador | varchar(160) | YES | NULL | — |
| Cargo | varchar(120) | YES | NULL | — |
| AntiguedadMeses | int(11) | YES | NULL | — |
| ProvinciaId | int(11) | YES | NULL | MUL |
| DistritoId | int(11) | YES | NULL | MUL |
| CorregimientoId | int(11) | YES | NULL | MUL |
| CreadoPor | int(11) | YES | NULL | MUL |
| ModificadoPor | int(11) | YES | NULL | MUL |
| Id | int(11) | NO | — | PRI |
| TipoPersona | varchar(10) | NO | — | — |
| TipoIdentificacion | varchar(10) | NO | — | — |
| Identificacion | varchar(30) | NO | — | UNI |
| Nombres | varchar(120) | NO | — | MUL |
| Apellidos | varchar(120) | NO | '' | — |
| FechaNacimiento | date | YES | NULL | — |
| Sexo | varchar(10) | NO | — | — |
| EstadoCivil | varchar(15) | NO | — | MUL |
| Telefono | varchar(8) | NO | — | — |
| Correo | varchar(254) | NO | — | — |
| Direccion | varchar(300) | NO | — | — |
| Provincia | varchar(80) | NO | — | — |
| Distrito | varchar(80) | NO | — | — |
| Corregimiento | varchar(80) | NO | — | — |
| Ocupacion | varchar(200) | NO | — | — |
| Ingresos | decimal(14,2) | NO | — | — |
| Referencias | varchar(1000) | NO | — | — |
| Activo | tinyint(1) | NO | 1 | — |
| Version | bigint(20) unsigned | NO | 1 | — |
| CreadoUtc | datetime | NO | current_timestamp() | — |
| ModificadoUtc | datetime | NO | current_timestamp() | — |
## corregimientos

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| Id | int(11) | NO | — | PRI |
| DistritoId | int(11) | NO | — | MUL |
| Codigo | varchar(30) | NO | — | UNI |
| Nombre | varchar(80) | NO | — | — |
| Activo | tinyint(1) | NO | 1 | — |
## cuotas

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| VersionPlan | int(11) | NO | 1 | — |
| Id | int(11) | NO | — | PRI |
| PrestamoId | int(11) | NO | — | MUL |
| NumeroCuota | int(11) | NO | — | — |
| FechaVencimiento | date | NO | — | MUL |
| Capital | decimal(14,2) | NO | — | — |
| Interes | decimal(14,2) | NO | — | — |
| InteresCapitalizado | decimal(14,2) | NO | 0.00 | — |
| Mora | decimal(14,2) | NO | 0.00 | — |
| OtrosCargos | decimal(14,2) | NO | 0.00 | — |
| SaldoProyectado | decimal(14,2) | NO | — | — |
## detallesamortizacionhistorica

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| VersionId | bigint(20) | NO | — | PRI |
| NumeroCuota | int(11) | NO | — | PRI |
| FechaVencimiento | date | NO | — | — |
| Capital | decimal(14,2) | NO | — | — |
| Interes | decimal(14,2) | NO | — | — |
| InteresCapitalizado | decimal(14,2) | NO | 0.00 | — |
| SaldoProyectado | decimal(14,2) | NO | — | — |
## distritos

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| Id | int(11) | NO | — | PRI |
| ProvinciaId | int(11) | NO | — | MUL |
| Codigo | varchar(20) | NO | — | UNI |
| Nombre | varchar(80) | NO | — | — |
| Activo | tinyint(1) | NO | 1 | — |
## ejecucionesreporte

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| Id | bigint(20) | NO | — | PRI |
| ReporteId | int(11) | NO | — | MUL |
| UsuarioId | int(11) | NO | — | MUL |
| FechaUtc | datetime | NO | current_timestamp() | — |
| FiltrosJson | longtext | YES | NULL | — |
| Formato | varchar(10) | NO | — | — |
## estados

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| EsSistema | tinyint(1) | NO | 1 | — |
| Activo | tinyint(1) | NO | 1 | — |
| Codigo | varchar(30) | NO | — | PRI |
| Nombre | varchar(80) | NO | — | — |
## estadosciviles

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| Codigo | varchar(15) | NO | — | PRI |
| Nombre | varchar(60) | NO | — | — |
| Activo | tinyint(1) | NO | 1 | — |
## formaspago

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| Codigo | varchar(20) | NO | — | PRI |
| Nombre | varchar(80) | NO | — | — |
| Activo | tinyint(1) | NO | 1 | — |
## historialprestamos

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| Id | bigint(20) | NO | — | PRI |
| PrestamoId | int(11) | NO | — | MUL |
| EstadoAnterior | varchar(30) | YES | NULL | MUL |
| EstadoNuevo | varchar(30) | NO | — | MUL |
| FechaUtc | datetime | NO | current_timestamp() | — |
| UsuarioId | int(11) | YES | NULL | MUL |
| Motivo | varchar(1000) | YES | NULL | — |
## historialsolicitudes

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| Id | bigint(20) | NO | — | PRI |
| SolicitudId | int(11) | NO | — | MUL |
| EstadoAnterior | varchar(30) | YES | NULL | MUL |
| EstadoNuevo | varchar(30) | NO | — | MUL |
| FechaUtc | datetime | NO | current_timestamp() | — |
| UsuarioId | int(11) | NO | — | MUL |
| Motivo | varchar(1000) | YES | NULL | — |
## metodoscalculo

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| ClaseImplementacion | varchar(200) | YES | NULL | — |
| Formula | text | YES | NULL | — |
| Variables | text | YES | NULL | — |
| Algoritmo | text | YES | NULL | — |
| EjemploManual | text | YES | NULL | — |
| Id | int(11) | NO | — | PRI |
| Codigo | varchar(30) | NO | — | UNI |
| Nombre | varchar(100) | NO | — | — |
| Descripcion | varchar(500) | NO | — | — |
| Activo | tinyint(1) | NO | 1 | — |
## movimientosprestamo

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| Id | bigint(20) | NO | — | PRI |
| PrestamoId | int(11) | NO | — | MUL |
| PagoId | int(11) | YES | NULL | MUL |
| Fecha | datetime | NO | — | — |
| Tipo | varchar(30) | NO | — | — |
| Concepto | varchar(200) | NO | — | — |
| Debito | decimal(14,2) | NO | 0.00 | — |
| Credito | decimal(14,2) | NO | 0.00 | — |
| UsuarioId | int(11) | YES | NULL | MUL |
| ReversaDeId | bigint(20) | YES | NULL | UNI |
## notificaciones

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| Id | bigint(20) | NO | — | PRI |
| UsuarioId | int(11) | NO | — | MUL |
| Tipo | varchar(40) | NO | — | — |
| Titulo | varchar(160) | NO | — | — |
| Mensaje | varchar(1000) | NO | — | — |
| Entidad | varchar(60) | YES | NULL | — |
| RegistroId | bigint(20) | YES | NULL | — |
| CreadoUtc | datetime | NO | current_timestamp() | — |
| LeidoUtc | datetime | YES | NULL | — |
## pagos

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| TipoPago | varchar(20) | NO | 'CUOTA_NORMAL' | — |
| FormaPagoCodigo | varchar(20) | NO | — | MUL |
| ReferenciaExterna | varchar(100) | YES | NULL | — |
| FechaAnulacionUtc | datetime | YES | NULL | — |
| ClaveIdempotencia | varchar(64) | NO | — | UNI |
| CreadoUtc | datetime | NO | current_timestamp() | — |
| Id | int(11) | NO | — | PRI |
| NumeroRecibo | varchar(40) | NO | — | UNI |
| PrestamoId | int(11) | NO | — | MUL |
| ClienteId | int(11) | NO | — | — |
| FechaPago | datetime | NO | — | MUL |
| MontoPagado | decimal(14,2) | NO | — | — |
| SaldoCapitalPosterior | decimal(14,2) | NO | — | — |
| RegistradoPor | int(11) | NO | — | MUL |
| Estado | varchar(10) | NO | 'APLICADO' | — |
| AnuladoPor | int(11) | YES | NULL | MUL |
| MotivoAnulacion | varchar(500) | YES | NULL | — |
## parametrossistema

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| Codigo | varchar(80) | NO | — | PRI |
| Valor | varchar(500) | NO | — | — |
| TipoDato | varchar(20) | NO | — | — |
| Descripcion | varchar(500) | NO | — | — |
| EsPropuesta | tinyint(1) | NO | 0 | — |
| ModificadoPor | int(11) | YES | NULL | MUL |
| ModificadoUtc | datetime | NO | current_timestamp() | — |
## periodicidades

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| Activo | tinyint(1) | NO | 1 | — |
| Id | int(11) | NO | — | PRI |
| Codigo | varchar(30) | NO | — | UNI |
| Nombre | varchar(80) | NO | — | — |
| Unidad | varchar(10) | NO | — | — |
| Intervalo | int(11) | NO | — | — |
| PeriodosPorAnio | decimal(10,4) | NO | — | — |
## permisos

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| Id | int(11) | NO | — | PRI |
| Codigo | varchar(80) | NO | — | UNI |
| Modulo | varchar(40) | NO | — | MUL |
| Accion | varchar(30) | NO | — | — |
| Descripcion | varchar(200) | NO | — | — |
## preferenciasusuario

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| UsuarioId | int(11) | NO | — | PRI |
| Clave | varchar(80) | NO | — | PRI |
| Valor | varchar(500) | NO | — | — |
## prestamos

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| PlazoMeses | decimal(8,2) | NO | — | — |
| FechaCancelacion | datetime | YES | NULL | — |
| TipoCancelacion | varchar(20) | YES | NULL | — |
| DesembolsadoPor | int(11) | YES | NULL | MUL |
| CreadoUtc | datetime | NO | current_timestamp() | — |
| ModificadoUtc | datetime | NO | current_timestamp() | — |
| Id | int(11) | NO | — | PRI |
| NumeroPrestamo | varchar(40) | NO | — | UNI |
| SolicitudId | int(11) | NO | — | UNI |
| ClienteId | int(11) | NO | — | — |
| ProductoId | int(11) | NO | — | MUL |
| MetodoId | int(11) | NO | — | — |
| PeriodicidadId | int(11) | NO | — | MUL |
| CapitalizacionId | int(11) | NO | — | MUL |
| FechaAprobacion | datetime | NO | — | — |
| FechaDesembolso | datetime | YES | NULL | — |
| FechaPrimerPago | date | NO | — | — |
| MontoAprobado | decimal(14,2) | NO | — | — |
| TasaInteres | decimal(9,6) | NO | — | — |
| TipoTasa | varchar(15) | NO | — | MUL |
| CantidadCuotas | int(11) | NO | — | — |
| Comision | decimal(14,2) | NO | 0.00 | — |
| GastosAsociados | decimal(14,2) | NO | 0.00 | — |
| SaldoCapital | decimal(14,2) | NO | — | — |
| EstadoCodigo | varchar(30) | NO | 'APROBADO' | MUL |
| AprobadoPor | int(11) | NO | — | MUL |
| Version | bigint(20) unsigned | NO | 1 | — |
## productometodos

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| ProductoId | int(11) | NO | — | PRI |
| MetodoId | int(11) | NO | — | PRI |
## productoperiodicidades

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| ProductoId | int(11) | NO | — | PRI |
| PeriodicidadId | int(11) | NO | — | PRI |
## productosfinancieros

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| PlazoMinimoMeses | decimal(8,2) | NO | 1.00 | — |
| PlazoMaximoMeses | decimal(8,2) | NO | 120.00 | — |
| TipoComision | varchar(10) | NO | 'FIJO' | — |
| ValorComision | decimal(14,6) | NO | 0.000000 | — |
| TipoGastos | varchar(10) | NO | 'FIJO' | — |
| ValorGastos | decimal(14,6) | NO | 0.000000 | — |
| CreadoUtc | datetime | NO | current_timestamp() | — |
| ModificadoUtc | datetime | NO | current_timestamp() | — |
| Id | int(11) | NO | — | PRI |
| Codigo | varchar(30) | NO | — | UNI |
| Nombre | varchar(120) | NO | — | UNI |
| Descripcion | varchar(500) | NO | — | — |
| MontoMinimo | decimal(14,2) | NO | — | — |
| MontoMaximo | decimal(14,2) | NO | — | — |
| PlazoMinimo | int(11) | NO | — | — |
| PlazoMaximo | int(11) | NO | — | — |
| Activo | tinyint(1) | NO | 1 | — |
## provincias

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| Id | int(11) | NO | — | PRI |
| Codigo | varchar(10) | NO | — | UNI |
| Nombre | varchar(80) | NO | — | UNI |
| Activo | tinyint(1) | NO | 1 | — |
## recuperacionesclave

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| Id | bigint(20) | NO | — | PRI |
| UsuarioId | int(11) | NO | — | MUL |
| TokenHash | char(64) | NO | — | UNI |
| CreadoUtc | datetime | NO | current_timestamp() | — |
| VenceUtc | datetime | NO | — | — |
| UsadoUtc | datetime | YES | NULL | — |
## referenciascliente

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| Id | int(11) | NO | — | PRI |
| ClienteId | int(11) | NO | — | MUL |
| Nombre | varchar(160) | NO | — | — |
| Parentesco | varchar(80) | NO | — | — |
| Telefono | varchar(8) | NO | — | — |
## refinanciamientos

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| CapitalTransferido | decimal(14,2) | NO | 0.00 | — |
| InteresTransferido | decimal(14,2) | NO | 0.00 | — |
| MoraTransferida | decimal(14,2) | NO | 0.00 | — |
| CargosTransferidos | decimal(14,2) | NO | 0.00 | — |
| Id | int(11) | NO | — | PRI |
| PrestamoOrigenId | int(11) | NO | — | UNI |
| PrestamoNuevoId | int(11) | NO | — | UNI |
| ClienteId | int(11) | NO | — | — |
| Fecha | datetime | NO | — | — |
| SaldoTransferido | decimal(14,2) | NO | — | — |
| Motivo | varchar(1000) | NO | — | — |
| RegistradoPor | int(11) | NO | — | MUL |
## reportes

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| Id | int(11) | NO | — | PRI |
| Codigo | varchar(40) | NO | — | UNI |
| Nombre | varchar(160) | NO | — | — |
| Descripcion | varchar(500) | NO | — | — |
| VistaSql | varchar(100) | NO | — | — |
| Activo | tinyint(1) | NO | 1 | — |
## roles

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| EsSistema | tinyint(1) | NO | 0 | — |
| Activo | tinyint(1) | NO | 1 | — |
| Id | int(11) | NO | — | PRI |
| Codigo | varchar(30) | NO | — | UNI |
| Nombre | varchar(80) | NO | — | — |
## rolpermisos

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| RolId | int(11) | NO | — | PRI |
| PermisoId | int(11) | NO | — | PRI |
## rolreportes

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| RolId | int(11) | NO | — | PRI |
| ReporteId | int(11) | NO | — | PRI |
## sesionesusuario

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| Id | bigint(20) | NO | — | PRI |
| UsuarioId | int(11) | NO | — | MUL |
| TokenHash | char(64) | NO | — | UNI |
| VersionSesion | bigint(20) | NO | — | — |
| CreadoUtc | datetime | NO | current_timestamp() | — |
| UltimaActividadUtc | datetime | NO | — | — |
| VenceUtc | datetime | NO | — | — |
| RevocadaUtc | datetime | YES | NULL | — |
## solicitudes

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| PlazoMeses | decimal(8,2) | NO | — | — |
| TasaPactada | decimal(9,6) | NO | — | — |
| TipoTasa | varchar(15) | NO | — | MUL |
| CapitalizacionId | int(11) | NO | — | MUL |
| FechaPrimerPago | date | NO | — | — |
| Comision | decimal(14,2) | NO | 0.00 | — |
| GastosAsociados | decimal(14,2) | NO | 0.00 | — |
| MontoAprobado | decimal(14,2) | YES | NULL | — |
| FechaAprobacion | datetime | YES | NULL | — |
| MotivoRechazo | varchar(1000) | YES | NULL | — |
| CuotaEstimada | decimal(14,2) | YES | NULL | — |
| IngresosEvaluados | decimal(14,2) | YES | NULL | — |
| Version | bigint(20) unsigned | NO | 1 | — |
| Id | int(11) | NO | — | PRI |
| NumeroSolicitud | varchar(40) | NO | — | UNI |
| ClienteId | int(11) | NO | — | MUL |
| ProductoId | int(11) | NO | — | MUL |
| MetodoId | int(11) | NO | — | — |
| TasaId | int(11) | NO | — | MUL |
| PeriodicidadId | int(11) | NO | — | MUL |
| FechaSolicitud | datetime | NO | current_timestamp() | — |
| MontoSolicitado | decimal(14,2) | NO | — | — |
| CantidadCuotas | int(11) | NO | — | — |
| EstadoCodigo | varchar(30) | NO | 'SOLICITUD' | MUL |
| CreadoPor | int(11) | NO | — | MUL |
| EvaluadoPor | int(11) | YES | NULL | MUL |
| FechaEvaluacion | datetime | YES | NULL | — |
| Observaciones | varchar(1000) | YES | NULL | — |
## tasas

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| Activo | tinyint(1) | NO | 1 | — |
| EsPredeterminada | tinyint(1) | NO | 0 | — |
| CreadoPor | int(11) | YES | NULL | MUL |
| Id | int(11) | NO | — | PRI |
| ProductoId | int(11) | NO | — | MUL |
| Porcentaje | decimal(9,6) | NO | — | — |
| TipoTasa | varchar(15) | NO | — | MUL |
| CapitalizacionId | int(11) | NO | — | MUL |
| VigenteDesde | date | NO | — | — |
| VigenteHasta | date | YES | NULL | — |
## tipostasa

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| Codigo | varchar(15) | NO | — | PRI |
| Nombre | varchar(80) | NO | — | — |
| Activo | tinyint(1) | NO | 1 | — |
## usuarioroles

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| UsuarioId | int(11) | NO | — | PRI |
| RolId | int(11) | NO | — | PRI |
## usuarios

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| IntentosFallidos | int(11) | NO | 0 | — |
| BloqueadoHastaUtc | datetime | YES | NULL | — |
| UltimoAccesoUtc | datetime | YES | NULL | — |
| DebeCambiarClave | tinyint(1) | NO | 0 | — |
| VersionSesion | bigint(20) | NO | 1 | — |
| ModificadoUtc | datetime | NO | current_timestamp() | — |
| Id | int(11) | NO | — | PRI |
| NombreUsuario | varchar(60) | NO | — | UNI |
| NombreCompleto | varchar(160) | NO | — | — |
| Correo | varchar(254) | NO | — | UNI |
| ContrasenaHash | varchar(255) | NO | — | — |
| Activo | tinyint(1) | NO | 1 | — |
| CreadoUtc | datetime | NO | current_timestamp() | — |
## versionesamortizacion

| Campo | Tipo | Acepta NULL | Valor inicial | Clave |
|---|---|---|---|---|
| Id | bigint(20) | NO | — | PRI |
| PrestamoId | int(11) | NO | — | MUL |
| VersionPlan | int(11) | NO | — | — |
| FechaUtc | datetime | NO | current_timestamp() | — |
| UsuarioId | int(11) | NO | — | MUL |
| Motivo | varchar(500) | NO | — | — |