# Fórmulas y convenciones del motor

P: principal en balboas; r: porcentaje anual dividido entre 100; n: número de cuotas; f: períodos de pago por año; m: capitalizaciones por año; i: tasa equivalente por período de pago. B: capital pendiente; C: cuota; A: abono a capital; I: interés.

Para tasa nominal anual: `i = (1 + r/m)^(m/f) − 1`.
Para tasa efectiva anual: `i = (1 + r)^(1/f) − 1`.

La conversión usa Math.Pow; importes y saldos usan Decimal. Cada interés y abono se redondea a centavos con MidpointRounding.AwayFromZero. El último abono ajusta los centavos para que la suma de capital sea exactamente P y el saldo final cero. Una cuota francesa final puede variar algunos centavos por ese ajuste. No se redondea previamente la tasa equivalente a una tasa mensual de dos decimales.

## Francés: MetodoFrances

`C = P*i / (1 − (1+i)^(-n))`; con tasa cero, `C=P/n`.
Por período: `I_k=redondear(B_(k−1)*i)`, `A_k=redondear(C)−I_k`, `B_k=B_(k−1)−A_k`. La última cuota toma el capital restante. Si el redondeo produjera amortización negativa, se rechazan las condiciones.

Ejemplo independiente: P=1000, nominal anual 12 %, capitalización/pago mensual, n=2. i=0.01; C sin redondeo≈507.5124378. Cuota 1: I=10, A=497.51, saldo=502.49. Cuota 2: I=5.02, A=502.49, saldo=0. Cuotas=507.51; interés total=15.02.

## Alemán: MetodoAleman

`A=P/n`, ajustando el último período. `I_k=B_(k−1)*i`; `C_k=A_k+I_k`.

P=1000, i=0.01, n=3: capitales 333.33, 333.33, 333.34; intereses 10, 6.67, 3.33. Cuotas 343.33, 340.00, 336.67; total=1020.00.

## Americano: MetodoAmericano

Cada período paga `I=P*i`. El capital se paga completo en la última cuota.

P=1000, i=0.01, n=3: cuotas 10, 10, 1010; interés total=30; saldo de capital 1000, 1000, 0.

## Simple: InteresSimple

Tasa nominal anual proporcional al tiempo contractual: `t=n/f`, `I_total=P*r*t`. Se distribuyen principal e interés por igual entre n períodos con ajuste de centavos final. No incorpora interés al capital ni utiliza una tasa efectiva como si fuera nominal; se rechaza EFECTIVA_ANUAL para este método.

P=1000, r=0.12, n=3, f=12: t=0.25, interés total=30. Capitales 333.33, 333.33, 333.34; intereses 10, 10, 10; cuotas 343.33, 343.33, 343.34.

## Compuesto: InteresCompuesto

Modalidad elegida: capitalización proyectada y pago global al vencimiento. `M_k=P*(1+i)^k`; se conservan proyecciones de saldo durante el plazo y se cobra al final `Capital=P`, `Interés=M_n−P`. Los períodos anteriores no tienen pago exigible. El primer vencimiento indica el primer período de proyección; el pago se exige en el último.

P=1000, i=0.01, n=3: proyección al cierre de períodos 1010, 1020.10 y 1030.30. Cuotas exigibles 0, 0, 1030.30; interés total=30.30. Los saldos mostrados en el plan son proyecciones y no significan que ya se haya desembolsado o cobrado interés futuro. SaldoCapital contable permanece P hasta el pago del principal. InteresCapitalizado de Cuotas no se rellena con proyecciones porque las vistas sumarían ese importe otra vez al interés final.

Esta convención permite comparar interés simple/compuesto sobre un mismo capital y plazo sin cobrar intereses aún no exigibles. Otras variantes de interés compuesto con pagos intermedios requieren otro método o política explícita; no deben incorporarse cambiando contratos existentes.

## Extensibilidad y pruebas

`IMetodoCalculoPrestamo.Generar` recibe condiciones y tasa periódica. `MotorPrestamos.Registrar` permite incorporar otra implementación. El motor asigna fechas y verifica cardinalidad, capital total, saldos e importes; no contiene un bloque de selección por cada cuota de cada método.

Tests/FinanzasTests.cs compara los ejemplos manuales anteriores con importes literales esperados. Además cubre capitales de 0.01, 1 y 10000000, plazos de 3 y 120 períodos, tasa cero, tasa efectiva equivalente, periodicidad de 15 días y fechas de fin de mes. No son pruebas únicamente de la fórmula consigo misma: la integración verifica que el dinero aplicado y los movimientos coincidan con los recibos y saldos reales.
