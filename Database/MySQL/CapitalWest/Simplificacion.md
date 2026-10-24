# Decisiones de simplificación

23 de octubre de 2026. De 40 a 27 tablas, conservando las 15 vistas, las restricciones de las tablas retenidas y la compatibilidad del módulo de clientes. No se modifica código funcional en este cambio.

## Tablas retiradas y reemplazo de su responsabilidad

| Tablas retiradas | Decisión |
|---|---|
| Permisos, RolPermisos | Reglas de autorización por roles en servidor; no se implementa un editor de permisos |
| SesionesUsuario | Usar infraestructura de sesión/autenticación ASP.NET al implementar seguridad |
| RecuperacionesClave | Recuperación de contraseña pospuesta |
| PreferenciasUsuario, Notificaciones | Funciones adicionales fuera del alcance actual |
| Reportes, RolReportes, EjecucionesReporte | Mantener consultas de los diez reportes y acceso por rol en código; eventos relevantes en Auditoria |
| HistorialSolicitudes, HistorialPrestamos | Registrar transiciones con estado anterior/nuevo, motivo y usuario en Auditoria |
| VersionesAmortizacion, DetallesAmortizacionHistorica | Posponer recálculo por abono anticipado; conservar préstamo/plan anterior al refinanciar o reestructurar |

Los reemplazos en código son decisiones para la implementación pendiente, no funciones ya implementadas. El historial de préstamos por cliente permanece en Prestamos; no dependía de las tablas retiradas de historial de estados. Refinanciamiento/reestructuración sigue dentro del alcance obligatorio.

## Matriz de acceso inicial propuesta

| Rol | Acceso previsto en servidor |
|---|---|
| Administrador | Usuarios, productos, condiciones, evaluación/aprobación/rechazo, préstamos, auditoría y todos los reportes |
| Oficial de crédito | Registrar/consultar/editar clientes, crear solicitudes, simular y consultar préstamos; reportes de clientes/cartera/amortización dentro de su alcance |
| Cajero | Registrar pagos y consultar préstamos/estados de cuenta necesarios para cobrarlos; reportes de pagos/estados de cuenta/amortización |
| Consulta | Lectura de información y reportes que se autoricen; ninguna modificación |

La autorización se aplicará antes de cada operación/consulta, incluso por URL directa. Consultar un reporte no concede acceso general a todos los clientes; el alcance debe filtrarse en servidor. La matriz precisa de reportes de Consulta debe definirse al implementar ese rol; por defecto no se concede acceso.

## Límites conservados

- AplicacionesPago no se elimina: permite pagos parciales y un recibo distribuido entre cuotas sin duplicar importes.
- MovimientosPrestamo no se elimina: permite estados de cuenta y reversas verificables.
- ProductoMetodos y ProductoPeriodicidades conservan las combinaciones permitidas mediante FK.
- ReferenciasCliente permanece para cerrar posteriormente la validación de referencias.
- Tasas y la copia contractual de condiciones en Prestamos se conservan para no alterar contratos al modificar catálogos.
- Catálogos y vistas se conservan porque quitar objetos únicamente para reducir el contador dificultaría mantener integridad y consultas.

## Respaldo e instalación

La base capitalwest se respaldó antes de migrar. Copia local de esta ejecución: `C:/Users/rafae/source/repos/SistemaFinanciera/Database/obj/Respaldos/capitalwest_antes_simplificar_20261023_193057.sql`. Se conserva en la carpeta local de respaldos, excluida de Git.

Instalaciones nuevas usan CapitalWest.sql. Instalaciones anteriores usan Migrar_a_esquema_simplificado.sql una vez, con respaldo previo y sin datos operativos en las tablas retiradas. La base local ya está migrada. Los demás integrantes deben actualizar su base por separado; Git no aplica la migración.

Verificación: importación limpia y migración correctas en MariaDB 10.4.32; 42 pruebas de clientes aprobadas en ambos escenarios; 15 vistas consultables. No se verificaron algoritmos financieros pendientes ni interfaz en navegador.
