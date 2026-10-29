Option Strict On
Public Class UsuarioFinanzas
    Public Property Id As Integer
    Public Property Nombre As String
    Public Property Roles As String()
    Public Function Puede(ParamArray permitidos As String()) As Boolean
        Return Roles IsNot Nothing AndAlso Roles.Any(Function(r) permitidos.Contains(r))
    End Function
End Class
Public Class ProductoFinanciero
    Public Property Id As Integer
    Public Property Codigo As String
    Public Property Nombre As String
    Public Property Descripcion As String
    Public Property MontoMinimo As Decimal
    Public Property MontoMaximo As Decimal
    Public Property CuotasMinimas As Integer
    Public Property CuotasMaximas As Integer
    Public Property MesesMinimos As Decimal
    Public Property MesesMaximos As Decimal
    Public Property Activo As Boolean = True
    Public Property Metodos As New List(Of Integer)
    Public Property Periodicidades As New List(Of Integer)
    Public Property TasaAnual As Decimal
    Public Property TipoTasa As String
    Public Property CapitalizacionId As Integer
    Public Property Comision As Decimal
    Public Property Gastos As Decimal
End Class
Public Class SolicitudFinanciera
    Public Property Clave As String
    Public Property ClienteId As Integer
    Public Property ProductoId As Integer
    Public Property MetodoId As Integer
    Public Property PeriodicidadId As Integer
    Public Property Monto As Decimal
    Public Property CantidadCuotas As Integer
    Public Property FechaPrimerPago As Date
    Public Property Observaciones As String
End Class
Public Class CondicionesPrestamo
    Public Property Monto As Decimal
    Public Property TasaAnual As Decimal
    Public Property TipoTasa As String
    Public Property CapitalizacionesAnuales As Decimal
    Public Property PeriodosAnuales As Decimal
    Public Property Unidad As String
    Public Property Intervalo As Integer
    Public Property CantidadCuotas As Integer
    Public Property FechaPrimerPago As Date
    Public Property Metodo As String
End Class
Public Class CuotaPlan
    Public Property Numero As Integer
    Public Property Fecha As Date
    Public Property Capital As Decimal
    Public Property Interes As Decimal
    Public Property Saldo As Decimal
    Public ReadOnly Property Cuota As Decimal
        Get
            Return Capital + Interes
        End Get
    End Property
End Class
Public Class PagoFinanciero
    Public Property PrestamoId As Integer
    Public Property Monto As Decimal
    Public Property Fecha As Date
    Public Property Forma As String
    Public Property Referencia As String
    Public Property Clave As String
End Class
