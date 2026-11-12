# Manual técnico de CapitalWest

## Estados de solicitudes y préstamos

Esta sección documenta el ciclo que está implementado en el repositorio y separa los estados de una solicitud de los estados de un préstamo formalizado.

### Dónde se guardan

- `Solicitudes.EstadoCodigo` admite `SOLICITUD`, `EN_EVALUACION`, `APROBADO` y `RECHAZADO`.
- `Prestamos.EstadoCodigo` admite `APROBADO`, `DESEMBOLSADO`, `VIGENTE`, `EN_MORA`, `CANCELADO` y `REFINANCIADO`.
- La tabla `Estados` contiene los nueve códigos y sus etiquetas legibles.
- Por ello, Solicitud, En evaluación, Aprobado y Rechazado son estados del trámite de crédito. Los estados posteriores describen el contrato de préstamo.

### Significado y comportamiento encontrado

| Estado | Entidad | Comportamiento actual |
|---|---|---|
| Solicitud | Solicitudes | Estado inicial al guardar una solicitud nueva. |
| En evaluación | Solicitudes | Se cambia desde Solicitud cuando un administrador inicia la evaluación. |
| Aprobado | Solicitudes | Se decide desde En evaluación; exige monto aprobado válido. Permanece así hasta formalizarse. |
| Rechazado | Solicitudes | Se decide desde En evaluación; el motivo es obligatorio y la solicitud se conserva. |
| Desembolsado | Préstamos | Código permitido en esquema y tratado como préstamo activo por consultas/pagos. El flujo `Formalizar` guarda `FechaDesembolso` y movimiento, pero crea el préstamo directamente en `VIGENTE`; no asigna este código. |
| Vigente | Préstamos | Estado inicial que realmente se persiste al formalizar y desembolsar. Representa un contrato activo con calendario de cuotas. |
| En mora | Préstamos | Se deriva cuando existen cuotas vencidas con saldo. Después de pagos, el DAL recalcula si persiste en mora o vuelve a vigente. |
| Cancelado | Préstamos | Se establece al quedar sin saldo exigible. Se registra fecha de cancelación y tipo `NORMAL`. |
| Refinanciado | Préstamos | Permitido por esquema, mostrado como cerrado, y asociado a la tabla `Refinanciamientos`. No existe una operación de refinanciamiento localizada en BL/DAL/pantallas. |

### Transiciones observadas

```text
Solicitud → En evaluación → Aprobado → Formalizar/desembolsar → Vigente
                         ↘ Rechazado
Vigente ↔ En mora → Cancelado cuando el saldo exigible llega a cero
```

La transición de En mora a Vigente ocurre si, tras aplicar el pago, ya no quedan cuotas vencidas pendientes. La lógica puede mostrar En mora como `EstadoActual` a partir de cuotas vencidas incluso si el valor persistido no se ha actualizado todavía.

### Diferencias entre catálogo y flujo ejecutable

No faltan códigos en el catálogo actual, por lo que no se agregó otro. Sí se detectaron dos brechas funcionales:

1. **Desembolsado** está declarado y aceptado, pero la formalización asigna directamente `VIGENTE`. No hay una etapa persistida intermedia de desembolso.
2. **Refinanciado** está declarado y la estructura SQL conserva campos para vincular préstamo original y nuevo, importes transferidos, fecha, motivo y usuario. Sin embargo, no hay un flujo transaccional que cree la nueva obligación, transfiera saldos, cierre el préstamo anterior y escriba auditoría.

No se debe marcar un préstamo como Refinanciado aislando el cambio de estado: el nuevo contrato, la relación con el anterior, la distribución del saldo y la trazabilidad deben confirmarse en una sola operación.

### Reglas asociadas del ciclo

- Solo una solicitud aprobada sin préstamo puede formalizarse; `SolicitudId` es único en `Prestamos`.
- La aprobación exige un monto dentro de límites y condiciones del producto; el rechazo exige motivo.
- El contrato copia las condiciones pactadas y crea su calendario de cuotas.
- Para pagos, el préstamo debe estar en `VIGENTE`, `EN_MORA` o `DESEMBOLSADO`; no se permiten cuotas futuras antes de que sean exigibles.
- Los pagos actualizan saldo, cuotas, movimientos, auditoría y estado dentro de una transacción.

### Referencias técnicas

- `Database/MySQL/CapitalWest/CapitalWest.sql`: tablas `Estados`, `Solicitudes`, `Prestamos`, `Refinanciamientos` y semillas de catálogo.
- `SistemaFinanciera.DAL/FinanzasRepositorio.vb`: `Evaluar`, `Formalizar` y `RegistrarPago`.
- `SistemaFinanciera/Prestamos/Prestamos.aspx.vb`: filtros y presentación del estado actual.
