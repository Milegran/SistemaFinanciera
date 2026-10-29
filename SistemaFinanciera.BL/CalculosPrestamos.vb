Option Strict On
Imports SistemaFinanciera.Entities

Public Interface IMetodoCalculoPrestamo
    Function Generar(c As CondicionesPrestamo, tasaPeriodica As Decimal) As List(Of CuotaPlan)
End Interface
Public Class MotorPrestamos
    Private ReadOnly metodos As New Dictionary(Of String, IMetodoCalculoPrestamo) From {
        {"FRANCES", New MetodoFrances()}, {"ALEMAN", New MetodoAleman()}, {"AMERICANO", New MetodoAmericano()},
        {"SIMPLE", New InteresSimple()}, {"COMPUESTO", New InteresCompuesto()}}
    Public Sub Registrar(codigo As String, metodo As IMetodoCalculoPrestamo)
        If String.IsNullOrWhiteSpace(codigo) OrElse metodo Is Nothing Then Throw New ArgumentException("Método inválido.")
        metodos(codigo) = metodo
    End Sub
    Public Function Generar(c As CondicionesPrestamo) As List(Of CuotaPlan)
        If c Is Nothing OrElse c.Monto < 0.01D OrElse c.Monto > 999999999999.99D OrElse Decimal.Round(c.Monto, 2) <> c.Monto Then Throw New ArgumentException("Monto inválido.")
        If c.CantidadCuotas < 1 OrElse c.CantidadCuotas > 1200 OrElse c.TasaAnual < 0 OrElse c.TasaAnual > 100 OrElse c.PeriodosAnuales <= 0 OrElse c.CapitalizacionesAnuales <= 0 OrElse c.Intervalo < 1 OrElse Not {"MESES", "DIAS"}.Contains(c.Unidad) Then Throw New ArgumentException("Revise tasa, cuotas y periodicidad.")
        If c.TipoTasa <> "NOMINAL_ANUAL" AndAlso c.TipoTasa <> "EFECTIVA_ANUAL" Then Throw New ArgumentException("Tipo de tasa inválido.")
        If c.Metodo Is Nothing OrElse Not metodos.ContainsKey(c.Metodo) Then Throw New ArgumentException("Método de cálculo no implementado.")
        Dim r = CDbl(c.TasaAnual / 100D)
        Dim i = If(c.TipoTasa = "EFECTIVA_ANUAL", Math.Pow(1 + r, 1 / CDbl(c.PeriodosAnuales)) - 1, Math.Pow(1 + r / CDbl(c.CapitalizacionesAnuales), CDbl(c.CapitalizacionesAnuales / c.PeriodosAnuales)) - 1)
        Dim plan = metodos(c.Metodo).Generar(c, CDec(i))
        If plan.Count <> c.CantidadCuotas OrElse plan.Last().Saldo <> 0 OrElse plan.Sum(Function(q) q.Capital) <> c.Monto OrElse plan.Any(Function(q) q.Capital < 0 OrElse q.Interes < 0 OrElse q.Cuota > 999999999999.99D) Then Throw New ArgumentException("Las condiciones exceden los límites del calendario.")
        For n As Integer = 0 To plan.Count - 1
            plan(n).Numero = n + 1
            plan(n).Fecha = If(c.Unidad = "MESES", c.FechaPrimerPago.AddMonths(n * c.Intervalo), c.FechaPrimerPago.AddDays(n * c.Intervalo))
        Next
        Return plan
    End Function
    Public Shared Function Dinero(valor As Decimal) As Decimal
        Return Decimal.Round(valor, 2, MidpointRounding.AwayFromZero)
    End Function
End Class
Public Class MetodoFrances
    Implements IMetodoCalculoPrestamo
    Public Function Generar(c As CondicionesPrestamo, i As Decimal) As List(Of CuotaPlan) Implements IMetodoCalculoPrestamo.Generar
        Dim cuota = If(i = 0, c.Monto / c.CantidadCuotas, c.Monto * i / CDec(1 - Math.Pow(1 + CDbl(i), -c.CantidadCuotas)))
        Dim saldo = c.Monto, plan As New List(Of CuotaPlan)
        For k = 1 To c.CantidadCuotas
            Dim interes = MotorPrestamos.Dinero(saldo * i)
            Dim capital = If(k = c.CantidadCuotas, saldo, Math.Min(saldo, MotorPrestamos.Dinero(cuota) - interes))
            If capital < 0 Then Throw New ArgumentException("La cuota redondeada no amortiza capital.")
            saldo -= capital
            plan.Add(New CuotaPlan With {.Capital = capital, .Interes = interes, .Saldo = saldo})
        Next
        Return plan
    End Function
End Class
Public Class MetodoAleman
    Implements IMetodoCalculoPrestamo
    Public Function Generar(c As CondicionesPrestamo, i As Decimal) As List(Of CuotaPlan) Implements IMetodoCalculoPrestamo.Generar
        Dim saldo = c.Monto, plan As New List(Of CuotaPlan)
        For k = 1 To c.CantidadCuotas
            Dim capital = If(k = c.CantidadCuotas, saldo, Math.Min(saldo, MotorPrestamos.Dinero(c.Monto / c.CantidadCuotas)))
            Dim interes = MotorPrestamos.Dinero(saldo * i)
            saldo -= capital
            plan.Add(New CuotaPlan With {.Capital = capital, .Interes = interes, .Saldo = saldo})
        Next
        Return plan
    End Function
End Class
Public Class MetodoAmericano
    Implements IMetodoCalculoPrestamo
    Public Function Generar(c As CondicionesPrestamo, i As Decimal) As List(Of CuotaPlan) Implements IMetodoCalculoPrestamo.Generar
        Return Enumerable.Range(1, c.CantidadCuotas).Select(Function(k) New CuotaPlan With {.Capital = If(k = c.CantidadCuotas, c.Monto, 0D), .Interes = MotorPrestamos.Dinero(c.Monto * i), .Saldo = If(k = c.CantidadCuotas, 0D, c.Monto)}).ToList()
    End Function
End Class
Public Class InteresSimple
    Implements IMetodoCalculoPrestamo
    Public Function Generar(c As CondicionesPrestamo, i As Decimal) As List(Of CuotaPlan) Implements IMetodoCalculoPrestamo.Generar
        ' Interés simple con tasa anual proporcional al tiempo contractual n/f.
        If c.TipoTasa <> "NOMINAL_ANUAL" Then Throw New ArgumentException("Interés simple utiliza tasa nominal anual proporcional al plazo.")
        Dim total = MotorPrestamos.Dinero(c.Monto * c.TasaAnual / 100D * c.CantidadCuotas / c.PeriodosAnuales)
        Dim saldo = c.Monto, pendienteInteres = total, plan As New List(Of CuotaPlan)
        For k = 1 To c.CantidadCuotas
            Dim capital = If(k = c.CantidadCuotas, saldo, Math.Min(saldo, MotorPrestamos.Dinero(c.Monto / c.CantidadCuotas)))
            Dim interes = If(k = c.CantidadCuotas, pendienteInteres, Math.Min(pendienteInteres, MotorPrestamos.Dinero(total / c.CantidadCuotas)))
            saldo -= capital : pendienteInteres -= interes
            plan.Add(New CuotaPlan With {.Capital = capital, .Interes = interes, .Saldo = saldo})
        Next
        Return plan
    End Function
End Class
Public Class InteresCompuesto
    Implements IMetodoCalculoPrestamo
    Public Function Generar(c As CondicionesPrestamo, i As Decimal) As List(Of CuotaPlan) Implements IMetodoCalculoPrestamo.Generar
        ' Pago único al vencimiento. La capitalización es proyectada, sin cobro intermedio.
        Dim plan As New List(Of CuotaPlan)
        For k = 1 To c.CantidadCuotas
            Dim acumulado = MotorPrestamos.Dinero(c.Monto * CDec(Math.Pow(1 + CDbl(i), k)))
            plan.Add(New CuotaPlan With {.Capital = If(k = c.CantidadCuotas, c.Monto, 0D), .Interes = If(k = c.CantidadCuotas, acumulado - c.Monto, 0D), .Saldo = If(k = c.CantidadCuotas, 0D, acumulado)})
        Next
        Return plan
    End Function
End Class
