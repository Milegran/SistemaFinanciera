# Cobertura del esquema simplificado

Fuente obligatoria: Proyecto No. 2 DS8.pdf, secciones 3–19. El Glosario.pdf define los conceptos. Las extensiones del antiguo diseño de pantallas no se tratan como requisitos obligatorios del enunciado.

Esta tabla indica soporte de datos, no funcionalidad implementada. Clientes, productos, solicitudes, préstamos y pagos están conectados a operaciones reales. Las cinco clases de cálculo, simulación integrada y autorización de los módulos nuevos se describen en docs/Modulos_financieros.md; los pendientes de la tabla siguiente reflejan el diseño anterior y se actualizan con ese registro de implementación.

| Requisito | Secciones | Soporte de datos / implementación pendiente |
|---|---|---|
| Usuarios, roles y claves con hash | 11–12 | Usuarios, Roles, UsuarioRoles; autenticación/autorización pendientes |
| Clientes y datos mínimos | 6 | Clientes, EstadosCiviles, ReferenciasCliente; referencias estructuradas pendientes de UI |
| Ubicación dependiente | 6 | provincia, distrito, corregimiento; catálogos suministrados cargados |
| Productos y condiciones | 3, 5, 7 | ProductosFinancieros, ProductoMetodos, ProductoPeriodicidades, Tasas, TiposTasa, Periodicidades |
| Cinco métodos extensibles | 4, 13, 18–19 | MetodosCalculo; cinco algoritmos implementados en BL (IMetodoCalculoPrestamo) y métodos nuevos con fórmulas propias |
| Solicitud, evaluación, aprobación/rechazo | 7 | Solicitudes, Estados; transiciones en BL y eventos en Auditoria pendientes |
| Formalización, desembolso y parámetros | 5, 7, 10 | Prestamos; condiciones contractuales y relaciones con Solicitudes |
| Amortización y simulación | 4, 8 | Cuotas y MetodosCalculo; la simulación puede calcularse sin persistir |
| Pago y desglose por cuota | 9 | Pagos, AplicacionesPago, FormasPago; validaciones transaccionales pendientes |
| Estado de cuenta y saldo | 3, 14 | MovimientosPrestamo y vistas; contabilización desde BL pendiente |
| Vencimiento y mora | 3, 9 | Cuotas, ParametrosSistema y vistas; reglas de BL pendientes |
| Refinanciamiento/reestructuración | 3 | Refinanciamientos, Prestamos; nueva obligación vinculada, anterior conservada y motivo en Auditoria |
| Historial de préstamos por cliente | 3 | Prestamos por ClienteId y vínculo de Refinanciamientos; consulta de aplicación pendiente |
| Diez reportes | 14 | Diez vistas v_Reporte01…10; interfaz/filtros/permisos en servidor pendientes |
| Registro de operaciones e historial de estados | 3, 12, 15 | Auditoria; eventos con anterior/nuevo y usuario pendientes |
| Modelo ER/lógico, diccionario y SQL | 12, 17 | Archivos de esta carpeta, actualizados a 27 tablas |

Los pagos anticipados y la mora tienen escenarios condicionales en la sección 16; el recálculo por abonos anticipados se pospone. Se mantienen datos y vistas para identificar mora, que sí aparece en los objetivos y reportes. La recuperación de claves, notificaciones, preferencias y permisos configurables por acción no son exigencias expresas del PDF.

La base valida integridad de registros mediante FK, UNIQUE y CHECK. Edad mínima, formatos admitidos de identificación, capacidad de pago, vigencias, sobrepagos, transiciones y contabilización deben implementarse en BL. Véanse las responsabilidades y límites en LEEME.md y las decisiones en Simplificacion.md.
