Option Strict On
Imports System.Globalization
Imports System.Text

''' <summary>
''' Opciones con las que un administrador define un método de cálculo nuevo sin escribir fórmulas.
''' </summary>
Public NotInheritable Class OpcionesMetodo
    ''' <summary>CUOTA_FIJA, CAPITAL_FIJO, AL_VENCIMIENTO o CUOTA_CRECIENTE.</summary>
    Public Property FormaCapital As String = "CUOTA_FIJA"
    ''' <summary>SALDO (sobre el saldo pendiente) o MONTO_ORIGINAL.</summary>
    Public Property BaseInteres As String = "SALDO"
    ''' <summary>CADA_CUOTA o AL_VENCIMIENTO (interés capitalizado y cobrado al final).</summary>
    Public Property PagoInteres As String = "CADA_CUOTA"
    ''' <summary>Primeras cuotas que solo pagan interés.</summary>
    Public Property CuotasGracia As Integer
    ''' <summary>Porcentaje del capital que se paga en la última cuota.</summary>
    Public Property PorcentajeGlobo As Decimal
    ''' <summary>Porcentaje en que sube la cuota cada período (solo cuota creciente).</summary>
    Public Property PorcentajeCrecimiento As Decimal
End Class

''' <summary>Resultado: las mismas piezas que guarda un método creado "Con fórmulas".</summary>
Public NotInheritable Class FormulasGeneradas
    Public Property Variables As String
    Public Property ExprInteres As String
    Public Property ExprCapital As String
    Public Property ExprSaldo As String
    Public Property Descripcion As String
    Public Property Formula As String
    Public Property Algoritmo As String
End Class

''' <summary>
''' Convierte las opciones en variables y fórmulas del motor de expresiones.
''' No cambia el motor ni los métodos existentes: el método resultante se guarda y se
''' ejecuta exactamente igual que uno creado "Con fórmulas" (MetodoExpresion).
''' </summary>
Public NotInheritable Class MetodoPorOpciones
    Public Const CuotaFija As String = "CUOTA_FIJA"
    Public Const CapitalFijo As String = "CAPITAL_FIJO"
    Public Const AlVencimiento As String = "AL_VENCIMIENTO"
    Public Const CuotaCreciente As String = "CUOTA_CRECIENTE"
    Public Const BaseSaldo As String = "SALDO"
    Public Const BaseMontoOriginal As String = "MONTO_ORIGINAL"
    Public Const PagoCadaCuota As String = "CADA_CUOTA"
    Public Const MaximoGracia As Integer = 60

    Private Sub New()
    End Sub

    Public Shared Sub Validar(o As OpcionesMetodo)
        If o Is Nothing Then Throw New ArgumentException("Seleccione las opciones del método.")
        If Not {CuotaFija, CapitalFijo, AlVencimiento, CuotaCreciente}.Contains(o.FormaCapital) Then Throw New ArgumentException("Seleccione cómo se paga el capital.")
        If Not {BaseSaldo, BaseMontoOriginal}.Contains(o.BaseInteres) Then Throw New ArgumentException("Seleccione sobre qué se calcula el interés.")
        If Not {PagoCadaCuota, AlVencimiento}.Contains(o.PagoInteres) Then Throw New ArgumentException("Seleccione cuándo se paga el interés.")
        If o.CuotasGracia < 0 OrElse o.CuotasGracia > MaximoGracia Then Throw New ArgumentException("Las cuotas de gracia deben estar entre 0 y " & MaximoGracia.ToString(CultureInfo.InvariantCulture) & ".")
        If o.PorcentajeGlobo < 0D OrElse o.PorcentajeGlobo > 90D OrElse Decimal.Round(o.PorcentajeGlobo, 2) <> o.PorcentajeGlobo Then Throw New ArgumentException("El % de cuota globo debe estar entre 0 y 90, con hasta 2 decimales.")
        If o.PorcentajeCrecimiento < 0D OrElse o.PorcentajeCrecimiento > 20D OrElse Decimal.Round(o.PorcentajeCrecimiento, 2) <> o.PorcentajeCrecimiento Then Throw New ArgumentException("El % de crecimiento debe estar entre 0 y 20, con hasta 2 decimales.")
        If o.PagoInteres = AlVencimiento AndAlso o.FormaCapital <> AlVencimiento Then Throw New ArgumentException("El interés pagado al final solo se combina con capital pagado al final.")
        If o.PagoInteres = AlVencimiento AndAlso o.BaseInteres <> BaseSaldo Then Throw New ArgumentException("El interés pagado al final se calcula sobre el saldo.")
        If o.FormaCapital = AlVencimiento AndAlso o.CuotasGracia > 0 Then Throw New ArgumentException("Con capital pagado al final no hace falta gracia: ninguna cuota abona capital antes del vencimiento.")
        If o.PorcentajeGlobo > 0D AndAlso o.FormaCapital <> CuotaFija AndAlso o.FormaCapital <> CapitalFijo Then Throw New ArgumentException("La cuota globo solo se combina con cuota fija o capital fijo.")
        If o.BaseInteres = BaseMontoOriginal AndAlso o.FormaCapital <> CapitalFijo Then Throw New ArgumentException("El interés sobre el monto original se combina con capital fijo.")
        If o.FormaCapital = CuotaCreciente AndAlso o.PorcentajeCrecimiento <= 0D Then Throw New ArgumentException("Indique un % de crecimiento mayor que 0 para la cuota creciente.")
        If o.FormaCapital <> CuotaCreciente AndAlso o.PorcentajeCrecimiento <> 0D Then Throw New ArgumentException("El % de crecimiento solo aplica a la cuota creciente.")
    End Sub

    Public Shared Function Generar(o As OpcionesMetodo) As FormulasGeneradas
        Validar(o)
        Dim globo = o.PorcentajeGlobo > 0D
        Dim v As New StringBuilder()
        v.Append("# Método por opciones: capital=").Append(o.FormaCapital).Append("; interes=").Append(o.BaseInteres).
          Append("; pago_interes=").Append(o.PagoInteres).Append("; gracia=").Append(Num(o.CuotasGracia)).
          Append("; globo=").Append(Num(o.PorcentajeGlobo)).Append("; crecimiento=").Append(Num(o.PorcentajeCrecimiento)).Append(vbLf)
        ' La gracia nunca consume todas las cuotas: en plazos cortos se reduce hasta n-1.
        v.Append("g = min(").Append(Num(o.CuotasGracia)).Append(", n - 1)").Append(vbLf)
        v.Append("m = n - g").Append(vbLf)
        If globo Then v.Append("globo = round(P * ").Append(Num(o.PorcentajeGlobo)).Append(" / 100)").Append(vbLf)
        Dim g = If(globo, "globo", "0")

        Dim r As New FormulasGeneradas With {.ExprSaldo = ""}
        If o.PagoInteres = AlVencimiento Then
            v.Append("acumulado = round(P * pow(1 + i, k))").Append(vbLf)
            r.ExprInteres = "if(k = n, acumulado - P, 0)"
            r.ExprCapital = "if(k = n, P, 0)"
            r.ExprSaldo = "if(k = n, 0, acumulado)"
        Else
            r.ExprInteres = If(o.BaseInteres = BaseMontoOriginal, "round(P * i)", "round(saldo * i)")
            Select Case o.FormaCapital
                Case CuotaFija
                    v.Append("cuota = if(i = 0, (P - ").Append(g).Append(") / m, (P - ").Append(g).Append(" / (1 + i)^m) * i / (1 - (1 + i)^(-m)))").Append(vbLf)
                    r.ExprCapital = "if(k <= g, 0, if(k = n, saldo, min(saldo, round(cuota - interes))))"
                Case CapitalFijo
                    r.ExprCapital = "if(k <= g, 0, if(k = n, saldo, min(saldo, round((P - " & g & ") / m))))"
                Case AlVencimiento
                    r.ExprCapital = "if(k = n, saldo, 0)"
                Case Else
                    v.Append("crece = ").Append(Num(o.PorcentajeCrecimiento)).Append(" / 100").Append(vbLf)
                    v.Append("primera = if(i = crece, P * (1 + i) / m, P * (i - crece) / (1 - ((1 + crece) / (1 + i))^m))").Append(vbLf)
                    v.Append("cuota = primera * (1 + crece)^(k - g - 1)").Append(vbLf)
                    ' Si la cuota no alcanza para el interés, ese período no abona capital (nunca negativo).
                    r.ExprCapital = "if(k <= g, 0, if(k = n, saldo, max(0, min(saldo, round(cuota - interes)))))"
            End Select
        End If
        r.Variables = v.ToString().TrimEnd(ChrW(10))
        r.Descripcion = Describir(o)
        ' Formula y Algoritmo son campos de documentación: no admiten los signos menor/mayor.
        r.Formula = FormulaLegible(o)
        r.Algoritmo = "Método creado con opciones. " & r.Descripcion & " Las fórmulas que ejecuta el motor están en Variables y en las fórmulas de interés, capital y saldo."
        Return r
    End Function

    Public Shared Function Describir(o As OpcionesMetodo) As String
        Dim sb As New StringBuilder()
        Select Case o.FormaCapital
            Case CuotaFija : sb.Append("Cuota fija")
            Case CapitalFijo : sb.Append("Capital fijo en cada cuota")
            Case AlVencimiento : sb.Append("Capital completo en la última cuota")
            Case Else : sb.Append("Cuota que sube ").Append(Num(o.PorcentajeCrecimiento)).Append("% cada período")
        End Select
        If o.PagoInteres = AlVencimiento Then
            sb.Append("; interés capitalizado y cobrado al final")
        ElseIf o.BaseInteres = BaseMontoOriginal Then
            sb.Append("; interés sobre el monto original")
        Else
            sb.Append("; interés sobre el saldo pendiente")
        End If
        If o.CuotasGracia > 0 Then sb.Append("; ").Append(Num(o.CuotasGracia)).Append(If(o.CuotasGracia = 1, " cuota", " cuotas")).Append(" de gracia (solo interés)")
        If o.PorcentajeGlobo > 0D Then sb.Append("; ").Append(Num(o.PorcentajeGlobo)).Append("% del capital en la última cuota")
        sb.Append(".")
        Return sb.ToString()
    End Function

    Private Shared Function FormulaLegible(o As OpcionesMetodo) As String
        If o.PagoInteres = AlVencimiento Then Return "M = P·(1+i)^n; última cuota: capital = P, interés = M − P; cuotas anteriores sin pago."
        Dim interes = If(o.BaseInteres = BaseMontoOriginal, "I = P·i", "I = saldo·i")
        Dim gracia = If(o.CuotasGracia > 0, "gracia = " & Num(o.CuotasGracia) & " (solo interés); m = n − gracia; ", "m = n; ")
        Dim globo = If(o.PorcentajeGlobo > 0D, "G = P·" & Num(o.PorcentajeGlobo) & "%; ", "G = 0; ")
        Select Case o.FormaCapital
            Case CuotaFija : Return gracia & globo & "C = (P − G/(1+i)^m)·i / (1 − (1+i)^(−m)); " & interes & "; A = C − I; la última cuota paga el saldo."
            Case CapitalFijo : Return gracia & globo & "A = (P − G)/m; " & interes & "; la última cuota paga el saldo."
            Case AlVencimiento : Return interes & "; A = 0 hasta la última cuota, que paga P."
            Case Else : Return gracia & "c = " & Num(o.PorcentajeCrecimiento) & "%; C1 = P·(i − c)/(1 − ((1+c)/(1+i))^m); C_k = C1·(1+c)^(k−gracia−1); " & interes & "; A = C − I (nunca negativo); la última cuota paga el saldo."
        End Select
    End Function

    Private Shared Function Num(valor As Decimal) As String
        Return valor.ToString("0.##", CultureInfo.InvariantCulture)
    End Function
End Class
