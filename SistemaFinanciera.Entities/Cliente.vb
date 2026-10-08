Option Strict On
Public Class Cliente
    Public Property Id As Integer
    Public Property TipoPersona As String
    Public Property TipoIdentificacion As String
    Public Property Identificacion As String
    Public Property Nombres As String
    Public Property Apellidos As String
    Public Property FechaNacimiento As Nullable(Of Date)
    Public Property Sexo As String
    Public Property EstadoCivil As String
    Public Property Telefono As String
    Public Property Correo As String
    Public Property Direccion As String
    Public Property CodigoProvincia As String
    Public Property CodigoDistrito As String
    Public Property CodigoCorregimiento As String
    Public Property Provincia As String
    Public Property Distrito As String
    Public Property Corregimiento As String
    Public Property Ocupacion As String
    Public Property Ingresos As Decimal
    Public Property Referencias As String
    Public Property CreadoUtc As DateTime
    Public Property Activo As Boolean = True
    Public Property Version As Byte()
    Public ReadOnly Property NombreCompleto As String
        Get
            Return (Nombres & " " & Apellidos).Trim()
        End Get
    End Property
    Public ReadOnly Property Estado As String
        Get
            Return If(Activo, "Activo", "Inactivo")
        End Get
    End Property
End Class
Public Interface IClienteRepositorio
    Function Listar(busqueda As String, activo As Nullable(Of Boolean)) As List(Of Cliente)
    Function Obtener(id As Integer) As Cliente
    Function Guardar(cliente As Cliente) As Integer
    Sub CambiarEstado(id As Integer, activo As Boolean, version As Byte())
End Interface
