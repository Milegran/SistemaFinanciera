# Base de datos CapitalWest

Actualización de implementación: los módulos de Productos, Solicitudes, Préstamos y Pagos, los cinco métodos y el acceso inicial ya están implementados. Véase `docs/Modulos_financieros.md` desde la raíz para responsabilidades cumplidas y límites actuales. Las menciones a funciones pendientes en el diseño original deben contrastarse con ese documento. El esquema no cambia.

Esquema simplificado para el Proyecto No. 2 DS8: **27 tablas, 15 vistas y dos triggers de protección de auditoría**. MariaDB 10.4.32 de XAMPP; aplicación Web Forms VB/.NET Framework 4.8 con MySqlConnector.

## Instalación nueva

1. Inicie MySQL en XAMPP y abra phpMyAdmin.
2. Importe `CapitalWest.sql` con UTF-8 en una instalación donde capitalwest aún no exista o no tenga objetos.
3. El script crea capitalwest y carga roles, estados, métodos, periodicidades, tipos de tasa, formas de pago, parámetros y ubicación.
4. Configure la conexión SistemaFinanciera de Web.config; la configuración local predeterminada apunta a 127.0.0.1:3306/capitalwest.
5. Compile la solución y ejecute `Tests/ProbarClientes.ps1` desde PowerShell.

No importe el script completo sobre una instalación existente. No es una migración ni una operación idempotente.

## Instalación anterior de 40 tablas

Realice un respaldo completo con mysqldump, incluidos triggers y rutinas. Ejecute una sola vez `Migrar_a_esquema_simplificado.sql`. Comprueba que las nueve tablas operativas que se retiran estén vacías antes de eliminar cualquiera de las 13 tablas auxiliares. Si encuentra datos, detiene la migración y debe prepararse su traslado antes de continuar. Los catálogos de permisos/reportes y sus asignaciones se retiran con esta versión; el respaldo conserva esa configuración.

MariaDB confirma DDL implícitamente; los DROP TABLE no se revierten con ROLLBACK. Si ocurre un fallo parcial, restaure el respaldo o revise los objetos restantes antes de reintentar. La rutina de migración puede quedar creada tras un error: diagnostique la causa antes de retirarla. No deshabilite claves foráneas para forzar la ejecución.

La instalación local del usuario ya se migró y validó el 23 de octubre de 2026. No necesita volver a ejecutar el script de migración.

## Tablas conservadas

| Área | Tablas |
|---|---|
| Ubicación (3) | provincia, distrito, corregimiento |
| Acceso (3) | Usuarios, Roles, UsuarioRoles |
| Clientes (2) | Clientes, ReferenciasCliente |
| Catálogos (5) | EstadosCiviles, TiposTasa, FormasPago, MetodosCalculo, Estados |
| Productos y condiciones (5) | ProductosFinancieros, ProductoMetodos, Periodicidades, ProductoPeriodicidades, Tasas |
| Ciclo financiero (7) | Solicitudes, Prestamos, Cuotas, Pagos, AplicacionesPago, Refinanciamientos, MovimientosPrestamo |
| Control (2) | Auditoria, ParametrosSistema |

Las 15 vistas son consultas guardadas; diez corresponden a los reportes obligatorios. Conservarlas evita repetir consultas complejas y no añade tablas de almacenamiento.

## Responsabilidades de la aplicación

- Autenticar usuarios y almacenar contraseñas con un algoritmo especializado con sal y parámetros. No se incluyen usuarios ni claves de ejemplo.
- Autorizar en servidor según Roles/UsuarioRoles. Ya no hay permisos configurables por acción ni por reporte; la matriz inicial está en Simplificacion.md. Las sesiones usarán la infraestructura de ASP.NET cuando se implemente autenticación. Recuperación de claves queda fuera del alcance actual.
- Registrar operaciones importantes y cambios de estado en Auditoria, con usuario, entidad, registro y detalle anterior/nuevo. Los triggers impiden editar/borrar eventos; no los generan. La auditoría será la fuente del historial de cambios de estado, mientras el historial de préstamos de un cliente se consulta desde Prestamos y Refinanciamientos.
- Las cinco implementaciones de cálculo siguen pendientes de BL. Las fórmulas descriptivas de MetodosCalculo no son algoritmos ejecutables.
- Formalizar, pagar, cancelar y refinanciar mediante transacciones y bloqueo del préstamo/solicitud antes de validar saldos o transiciones.
- Comprobar producto/cliente activos, límites, tasa vigente, método y periodicidad permitidos; guardar copia contractual en Prestamos.
- Aplicar pagos a cuotas mediante AplicacionesPago; impedir sobrepagos sin regla explícita. Registrar recibo, aplicaciones, saldo, movimientos y auditoría en la misma transacción. ClaveIdempotencia evita duplicar recibos si la aplicación la usa correctamente.
- Definir prioridad de mora/interés/capital y otros cargos, redondeo, conversión de tasa, capitalización y calendario. No hay tasa de mora obligatoria en el enunciado.
- SaldoCapital es capital real pendiente; los intereses futuros del plan no son importe de liquidación anticipada. MovimientosPrestamo debe registrar todos los débitos/créditos y sus reversas para que el estado de cuenta sea correcto.
- Los pagos anticipados con recálculo de la misma obligación quedan pospuestos. Para refinanciamiento o reestructuración, crear un nuevo préstamo vinculado en Refinanciamientos y conservar el anterior y sus cuotas; registrar el motivo en auditoría. No sobrescribir planes con pagos. VersionPlan permanece como campo compatible, sin repositorio de versiones históricas.
- Los reportes se consultarán con filtros parametrizados, autorización por rol y evento en auditoría cuando corresponda. No hay catálogo ni historial separado de ejecuciones de reportes.

## Clientes y ubicación

Se conserva la compatibilidad con ClienteServicio y ClienteRepositorio: todas las columnas utilizadas por la aplicación mantienen nombres/tipos y se conservan los CHECK, UNIQUE y FK existentes. Version es BIGINT UNSIGNED de concurrencia; la UI conserva su token de ocho bytes.

Hay 13 provincias, 82 distritos y 668 corregimientos suministrados. Los códigos son texto y preservan ceros iniciales. BRILLANTE (130601) permanece fuera del selector sin distrito válido. Los datos suministrados no se certifican como catálogo oficial vigente. Para su actualización consulte `../Ubicacion/LEEME.md`.

ReferenciasCliente admite referencias estructuradas; el formulario actual conserva Referencias como texto. Empleador, Cargo, AntiguedadMeses y RucDv están preparados en SQL, pero no conectados al formulario. Los problemas de validación identificados en docs/Revision_requisitos_2026-10-23.md no se corrigen mediante esta simplificación.

Cada conexión configura UTC. Las fechas civiles son de Panamá. La periodicidad QUINCENAL del catálogo significa 15 días; confirme la convención contractual y no la confunda automáticamente con dos fechas fijas al mes.

## Documentación y verificación

- `Simplificacion.md`: decisiones, tablas retiradas y matriz de acceso propuesta.
- `Cobertura_requisitos.md`: correspondencia con el enunciado.
- `Diccionario_datos.md`, `Modelo_logico.md`, `Modelo_entidad_relacion.mmd`: esquema reducido, tipos y relaciones.
- `Validacion.json`: resultados actuales y resultados históricos identificados por separado.

La versión reducida se importó sin errores en una base aislada y pasó 42 pruebas de clientes. Después se migró capitalwest y pasaron de nuevo las 42 pruebas; los datos de prueba se revirtieron. Las 15 vistas se consultaron sin errores. No son pruebas de cálculos financieros ni de interfaz visual.
